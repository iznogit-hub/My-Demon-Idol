using System;
using System.Collections;
using UnityEngine;
using DemonIdol.UI;

namespace DemonIdol.Combat
{
    public enum MatchState
    {
        Inactive,
        IntroCountdown,
        Fighting,
        RoundEnd,
        MatchOver
    }

    public enum RoundWinner
    {
        None,
        Player1,
        Player2,
        Draw
    }

    public enum EndReason
    {
        Knockout,
        TimeOut,
        DoubleKO
    }

    /// <summary>
    /// Master Match & Round Referee for Demon Idol 1v1 Fighting.
    /// Manages round introductions ("ROUND 1... FIGHT!"), 99-second countdown timer,
    /// dynamic K.O. slow-motion finish, win pips, and match victory ceremonies.
    /// </summary>
    public class MatchRoundReferee : MonoBehaviour
    {
        public static MatchRoundReferee Instance { get; private set; }

        [Header("Fighter References")]
        [SerializeField] private FighterController _fighter1;
        [SerializeField] private FighterController _fighter2;

        public FighterController Fighter1 => _fighter1;
        public FighterController Fighter2 => _fighter2;

        [Header("Match Settings")]
        [SerializeField] private int _roundDurationSeconds = 99;
        [SerializeField] private int _winsRequired = 2; // Best 2 out of 3

        public MatchState State { get; private set; } = MatchState.Inactive;
        public int CurrentRound { get; private set; } = 1;
        public int P1Wins { get; private set; } = 0;
        public int P2Wins { get; private set; } = 0;
        public float SecondsRemaining { get; private set; }
        public int RoundTicksRemaining { get; private set; } = 99 * 60;
        public bool isTrainingMode = false;
        private int _lastLoggedSec = -1;

        public event Action<int> OnRoundIntro; // int roundNumber
        public event Action OnFightBegun;
        public event Action<int> OnTimerUpdated; // int seconds
        public event Action<RoundWinner, EndReason> OnRoundEnded;
        public event Action<RoundWinner> OnMatchFinished;

        private Coroutine _roundRoutine;
        private Vector3 _p1SpawnPos = new Vector3(-2.5f, 0, 0);
        private Vector3 _p2SpawnPos = new Vector3(2.5f, 0, 0);

        private void Awake()
        {
            if (GameplaySimulationAuthority.IsPackageNative)
            {
                enabled = false;
                return;
            }

            if (Instance != null && Instance != this)
            {
                DemonIdol.Audio.CombatAudioDirector.DestroyDuplicateComponent(this);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (CombatTickEngine.Instance != null)
            {
                CombatTickEngine.Instance.OnTick += HandleCombatTick;
            }
        }

        private void OnDestroy()
        {
            if (CombatTickEngine.Instance != null)
            {
                CombatTickEngine.Instance.OnTick -= HandleCombatTick;
            }
        }

        private void HandleCombatTick(int currentTick)
        {
            if (State != MatchState.Fighting) return;
            if (CombatTickEngine.Instance != null && CombatTickEngine.Instance.IsSuperfrozen) return;

            if (isTrainingMode)
            {
                SecondsRemaining = _roundDurationSeconds;
                if (_lastLoggedSec != _roundDurationSeconds)
                {
                    _lastLoggedSec = _roundDurationSeconds;
                    OnTimerUpdated?.Invoke(_lastLoggedSec);
                }
                return;
            }

            if (RoundTicksRemaining > 0)
            {
                RoundTicksRemaining--;
                SecondsRemaining = Mathf.Max(0f, RoundTicksRemaining / 60f);
                int curSec = Mathf.CeilToInt(SecondsRemaining);
                if (curSec != _lastLoggedSec)
                {
                    _lastLoggedSec = curSec;
                    OnTimerUpdated?.Invoke(_lastLoggedSec);
                }
            }
        }

        public void BindFighters(FighterController p1, FighterController p2)
        {
            _fighter1 = p1;
            _fighter2 = p2;
            if (_fighter1 != null) _p1SpawnPos = _fighter1.transform.position;
            if (_fighter2 != null) _p2SpawnPos = _fighter2.transform.position;
        }

        /// <summary>
        /// Begins a full 1v1 Match from Round 1.
        /// </summary>
        public void StartMatch()
        {
            FindFightersIfMissing();
            CurrentRound = 1;
            P1Wins = 0;
            P2Wins = 0;

            if (_roundRoutine != null) StopCoroutine(_roundRoutine);
            _roundRoutine = StartCoroutine(RunMatchFlow());
        }

        private void FindFightersIfMissing()
        {
            if (_fighter1 == null || _fighter2 == null)
            {
                var fighters = FindObjectsByType<FighterController>();
                foreach (var f in fighters)
                {
                    if (f.isPlayerControlled && _fighter1 == null) _fighter1 = f;
                    else if (_fighter2 == null) _fighter2 = f;
                }
            }
        }

        private IEnumerator RunMatchFlow()
        {
            while (P1Wins < _winsRequired && P2Wins < _winsRequired)
            {
                // 1. Reset fighters to starting positions
                ResetFighter(_fighter1, _p1SpawnPos, Quaternion.Euler(0, 90f, 0));
                ResetFighter(_fighter2, _p2SpawnPos, Quaternion.Euler(0, -90f, 0));

                RoundTicksRemaining = _roundDurationSeconds * 60;
                SecondsRemaining = _roundDurationSeconds;
                _lastLoggedSec = _roundDurationSeconds;
                State = MatchState.IntroCountdown;

                // Start Replay recording if session active
                if (MatchReplaySession.Instance != null && !isTrainingMode && CurrentRound == 1)
                {
                    string p1 = _fighter1 != null ? _fighter1.fighterName : "C01";
                    string p2 = _fighter2 != null ? _fighter2.fighterName : "C02";
                    MatchReplaySession.Instance.StartRecording(p1, p2, "ST01", UnityEngine.Random.Range(1000, 99999));
                }

                // Lock inputs during intro
                SetFighterInputsEnabled(false);

                // Notify UI: "ROUND X"
                OnRoundIntro?.Invoke(CurrentRound);
                yield return new WaitForSeconds(1.4f);

                // Notify UI: "FIGHT!"
                OnFightBegun?.Invoke();
                SetFighterInputsEnabled(true);
                State = MatchState.Fighting;

                OnTimerUpdated?.Invoke(_lastLoggedSec);

                // 2. Active Combat Loop
                RoundWinner winner = RoundWinner.None;
                EndReason reason = EndReason.Knockout;

                while (State == MatchState.Fighting)
                {
                    if (isTrainingMode)
                    {
                        // In free practice mode, fighters don't die - auto-replenish to keep practicing
                        if (_fighter1 != null && _fighter1.currentHealth <= 0)
                        {
                            _fighter1.ResetToIdle();
                        }
                        if (_fighter2 != null && _fighter2.currentHealth <= 0)
                        {
                            _fighter2.ResetToIdle();
                        }
                        yield return null;
                        continue;
                    }

                    // Check for HP depletion
                    bool p1Dead = _fighter1 != null && _fighter1.currentHealth <= 0;
                    bool p2Dead = _fighter2 != null && _fighter2.currentHealth <= 0;

                    if (p1Dead && p2Dead)
                    {
                        winner = RoundWinner.Draw;
                        reason = EndReason.DoubleKO;
                        break;
                    }
                    else if (p1Dead)
                    {
                        winner = RoundWinner.Player2;
                        reason = EndReason.Knockout;
                        break;
                    }
                    else if (p2Dead)
                    {
                        winner = RoundWinner.Player1;
                        reason = EndReason.Knockout;
                        break;
                    }

                    // Check for Time Out (strictly driven by 60Hz combat simulation ticks)
                    if (RoundTicksRemaining <= 0)
                    {
                        reason = EndReason.TimeOut;
                        float hp1 = _fighter1 != null ? _fighter1.currentHealth : 0f;
                        float hp2 = _fighter2 != null ? _fighter2.currentHealth : 0f;
                        if (hp1 > hp2) winner = RoundWinner.Player1;
                        else if (hp2 > hp1) winner = RoundWinner.Player2;
                        else winner = RoundWinner.Draw;
                        break;
                    }

                    yield return null;
                }

                // 3. Round Ended
                State = MatchState.RoundEnd;
                SetFighterInputsEnabled(false);

                if (winner == RoundWinner.Player1) P1Wins++;
                else if (winner == RoundWinner.Player2) P2Wins++;

                // Dramatic K.O. Slow Motion Finish & Cinematic Camera
                if (reason == EndReason.Knockout || reason == EndReason.DoubleKO)
                {
                    if (FighterCamera2D.Instance != null) FighterCamera2D.Instance.TriggerKO();

                    // Ace Death Replay integration (Fail-safe, preserves deterministic flow)
                    if (AceDeathReplayDirector.Instance != null && AceDeathReplayDirector.Instance.ReplayEnabled)
                    {
                        yield return AceDeathReplayDirector.Instance.PlayDeathReplayCoroutine(null);
                    }
                    else
                    {
                        Time.timeScale = 0.25f;
                        yield return new WaitForSecondsRealtime(1.0f);
                        Time.timeScale = 1.0f;
                    }
                }
                else
                {
                    Time.timeScale = 0.25f;
                    yield return new WaitForSecondsRealtime(1.0f);
                    Time.timeScale = 1.0f;
                }

                OnRoundEnded?.Invoke(winner, reason);
                yield return new WaitForSeconds(2.5f);

                CurrentRound++;
            }

            // 4. Match Finished
            State = MatchState.MatchOver;
            if (FighterCamera2D.Instance != null) FighterCamera2D.Instance.TriggerResults();
            RoundWinner matchWinner = P1Wins > P2Wins ? RoundWinner.Player1 : (P2Wins > P1Wins ? RoundWinner.Player2 : RoundWinner.Draw);

            if (MatchReplaySession.Instance != null && MatchReplaySession.Instance.IsRecording)
            {
                int winnerSlot = (matchWinner == RoundWinner.Player1) ? 1 : ((matchWinner == RoundWinner.Player2) ? 2 : 0);
                MatchReplaySession.Instance.StopRecording(winnerSlot);
            }

            OnMatchFinished?.Invoke(matchWinner);
        }

        /// <summary>
        /// Immediately concludes the match when an opponent forfeits or disconnects.
        /// Awards victory to the remaining player and triggers post-match settlement.
        /// </summary>
        public void TriggerForfeitVictory(RoundWinner winner)
        {
            if (State == MatchState.MatchOver) return;
            if (_roundRoutine != null) StopCoroutine(_roundRoutine);

            State = MatchState.MatchOver;
            if (winner == RoundWinner.Player1) P1Wins = _winsRequired;
            else if (winner == RoundWinner.Player2) P2Wins = _winsRequired;

            if (FighterCamera2D.Instance != null) FighterCamera2D.Instance.TriggerResults();

            if (MatchReplaySession.Instance != null && MatchReplaySession.Instance.IsRecording)
            {
                int winnerSlot = (winner == RoundWinner.Player1) ? 1 : ((winner == RoundWinner.Player2) ? 2 : 0);
                MatchReplaySession.Instance.StopRecording(winnerSlot);
            }

            OnRoundEnded?.Invoke(winner, EndReason.Knockout);
            OnMatchFinished?.Invoke(winner);
        }

        private void ResetFighter(FighterController fc, Vector3 pos, Quaternion rot)
        {
            if (fc == null) return;
            var cc = fc.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            fc.transform.position = pos;
            fc.transform.rotation = rot;
            if (cc != null) cc.enabled = true;

            fc.ResetToIdle();

            var brain = fc.GetComponent<DemonIdol.BlazBlue.BBFighterBrain>();
            if (brain != null)
            {
                brain.ResetForRound();
            }
        }

        private void SetFighterInputsEnabled(bool enabled)
        {
            SetControllerInput(_fighter1, enabled);
            SetControllerInput(_fighter2, enabled);
        }

        private void SetControllerInput(FighterController fc, bool enabled)
        {
            if (fc == null) return;
            fc.SetMoveInput(Vector2.zero);
            if (fc.InputBuf != null) fc.InputBuf.Clear();

            var input = fc.GetComponent<MobileFighterInput>();
            if (input != null) input.enabled = enabled;

            var dummy = fc.GetComponent<TrainingDummyAI>();
            if (dummy != null) dummy.enabled = enabled;

            var ai = fc.GetComponent<CombatAIController>();
            if (ai != null) ai.enabled = enabled;

            var bbSampler = fc.GetComponent<DemonIdol.BlazBlue.BBInputSampler>();
            if (bbSampler != null)
            {
                bbSampler.enabled = enabled;
                if (!enabled) bbSampler.ResetLatches();
            }

            var bbAi = fc.GetComponent<DemonIdol.BlazBlue.BBAIController>();
            if (bbAi != null)
            {
                bbAi.enabledAI = enabled;
                bbAi.enabled = enabled;
                if (!enabled) bbAi.ResetAI();
            }
        }

        public void Rematch()
        {
            StartMatch();
        }

        public void StopMatch()
        {
            if (_roundRoutine != null)
            {
                StopCoroutine(_roundRoutine);
                _roundRoutine = null;
            }
            State = MatchState.MatchOver;
            SetFighterInputsEnabled(false);
        }
    }
}
