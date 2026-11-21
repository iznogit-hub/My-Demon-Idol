using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DemonIdol.Combat;
using DemonIdol.Launch;
using DemonIdol.UI;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Builds the VS screen, round banners, and results screen from BBMatchFlow.
    /// Reuses PreMatchVSOverlay and MatchResultsOverlay when they are already in the scene.
    /// </summary>
    public class BBMatchScreenDirector : MonoBehaviour
    {
        public static BBMatchScreenDirector Instance { get; private set; }
        public BBMatchFlow Flow = new BBMatchFlow();
        public BBMatchStats Stats = new BBMatchStats();
        Text _banner;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Start()
        {
            var referee = MatchRoundReferee.Instance;
            if (referee == null) return;
            referee.OnRoundIntro += round =>
            {
                if (round == 1) Stats.Reset();
                ShowBanner(round >= 3 ? "FINAL ROUND" : "ROUND " + round);
            };
            referee.OnFightBegun += () => ShowBanner("FIGHT");
            referee.OnRoundEnded += (winner, reason) =>
            {
                bool isPerfect = false;
                var sim = BBMatchSim.Instance;
                if (sim != null)
                {
                    if (winner == RoundWinner.Player1 && sim.p1Brain != null)
                        isPerfect = sim.p1Brain.health >= sim.p1Brain.maxHealth;
                    else if (winner == RoundWinner.Player2 && sim.p2Brain != null)
                        isPerfect = sim.p2Brain.health >= sim.p2Brain.maxHealth;
                }
                else
                {
                    if (winner == RoundWinner.Player1 && referee.Fighter1 != null)
                        isPerfect = referee.Fighter1.currentHealth >= referee.Fighter1.maxHealth;
                    else if (winner == RoundWinner.Player2 && referee.Fighter2 != null)
                        isPerfect = referee.Fighter2.currentHealth >= referee.Fighter2.maxHealth;
                }
                ShowBanner(isPerfect ? "PERFECT" : (reason == EndReason.TimeOut ? "TIME" : "KO"));
            };
            referee.OnMatchFinished += winner => ShowResults(winner == RoundWinner.Player2 ? 2 : 1);
        }

        public void PlayVersus(string p1, string p2, string stageId, string mode)
        {
            Stats.Reset();
            Flow.ConfirmCharacters(p1, p2);
            Flow.StageId = string.IsNullOrEmpty(stageId) ? "ST07" : stageId;
            Flow.Mode = mode;
            var overlay = PreMatchVSOverlay.Instance;
            if (overlay == null)
                overlay = PreMatchVSOverlay.CreateRuntime();
            overlay.PlayMatchIntro(Flow.P1Id, Flow.P2Id, Flow.StageId, Flow.Mode, () => Flow.BeginFight());
            StartCoroutine(Countdown(overlay));
        }

        IEnumerator Countdown(PreMatchVSOverlay overlay)
        {
            for (int n = 3; n >= 1; n--)
            {
                if (overlay != null) overlay.SetCountdown(n.ToString());
                yield return new WaitForSecondsRealtime(0.6f);
            }
            if (overlay != null) overlay.SetCountdown("FIGHT");
        }

        public void ShowBanner(string text)
        {
            EnsureBanner();
            if (_banner != null) _banner.text = text;
            Flow.Screen = text == "FIGHT" ? BBFlowScreen.Fight : BBFlowScreen.RoundIntro;
        }

        public void ShowResults(int winnerPlayer)
        {
            string winnerId = winnerPlayer == 2 ? Flow.P2Id : Flow.P1Id;
            bool perfect = Stats.P1Damage == 0 || Stats.P2Damage == 0;
            var sim = BBMatchSim.Instance;
            if (sim != null)
            {
                if (winnerPlayer == 1 && sim.p1Brain != null) perfect |= sim.p1Brain.health >= sim.p1Brain.maxHealth;
                else if (winnerPlayer == 2 && sim.p2Brain != null) perfect |= sim.p2Brain.health >= sim.p2Brain.maxHealth;
            }
            Flow.ReportRound(winnerPlayer, perfect, false);
            var card = BBPresentationLibrary.Find(winnerId);
            string quote = card != null && !string.IsNullOrEmpty(card.quote) ? card.quote : "Quote slot empty.";
            string name = card != null ? card.displayName : winnerId;

            // Move camera to frame winner and play victory or idle clip behind results panel
            Transform winnerTransform = null;
            Animator winnerAnimator = null;
            BBAnimationDriver winnerDriver = null;

            if (sim != null)
            {
                var brain = winnerPlayer == 2 ? sim.p2Brain : sim.p1Brain;
                if (brain != null)
                {
                    winnerTransform = brain.transform;
                    winnerAnimator = brain.fighter != null ? brain.fighter.animator : brain.GetComponentInChildren<Animator>();
                    winnerDriver = brain.animDriver;
                }
            }
            if (winnerTransform == null && MatchRoundReferee.Instance != null)
            {
                var fc = winnerPlayer == 2 ? MatchRoundReferee.Instance.Fighter2 : MatchRoundReferee.Instance.Fighter1;
                if (fc != null)
                {
                    winnerTransform = fc.transform;
                    winnerAnimator = fc.animator;
                }
            }

            if (winnerTransform != null && FighterCamera2D.Instance != null)
            {
                FighterCamera2D.Instance.RequestState(CameraState.Results, 999f, winnerTransform);
            }

            if (winnerDriver != null)
            {
                winnerDriver.BlendToNeutral();
            }
            if (winnerAnimator != null)
            {
                winnerAnimator.SetTrigger("Victory");
            }

            MatchResultsOverlay.CreateOrReuse().ShowFightResult(winnerPlayer, name, quote, Stats, Flow);
            BBContactPresentation.RaiseWinner(winnerId);
        }

        void EnsureBanner()
        {
            if (_banner != null) return;
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            var go = new GameObject("RoundBanner", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(canvas.transform, false);
            _banner = go.GetComponent<Text>();
            _banner.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _banner.fontSize = 64;
            _banner.alignment = TextAnchor.MiddleCenter;
            _banner.color = Color.white;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(800f, 120f);
        }
    }
}
