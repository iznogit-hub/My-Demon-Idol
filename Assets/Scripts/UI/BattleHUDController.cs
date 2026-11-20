using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DemonIdol.Combat;
using DemonIdol.Launch;

namespace DemonIdol.UI
{
    /// <summary>
    /// Master In-Combat Battle HUD Controller.
    /// Drives dynamic Health bars with delayed chip damage lag, Super/Domain meter,
    /// round countdown timer, combo counters, announcement banners ("FIGHT!", "K.O.!"),
    /// and match victory modal.
    /// </summary>
    public class BattleHUDController : MonoBehaviour
    {
        private static BattleHUDController _instance;
        public static BattleHUDController Instance
        {
            get
            {
                if (_instance == null) return null;
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Player 1 Display")]
        [SerializeField] private Text _p1NameText;
        [SerializeField] private Image _p1HealthFill;
        [SerializeField] private Image _p1HealthLagFill;
        [SerializeField] private Image _p1ProjFill;
        [SerializeField] private Image _p1SuperFill;
        [SerializeField] private Text _p1DomainReadyText;
        [SerializeField] private Image[] _p1WinPips;

        [Header("Player 2 Display")]
        [SerializeField] private Text _p2NameText;
        [SerializeField] private Image _p2HealthFill;
        [SerializeField] private Image _p2HealthLagFill;
        [SerializeField] private Image _p2ProjFill;
        [SerializeField] private Image _p2SuperFill;
        [SerializeField] private Text _p2DomainReadyText;
        [SerializeField] private Image[] _p2WinPips;

        [Header("Round Timer & Status")]
        [SerializeField] private Text _timerText;
        [SerializeField] private Text _roundBannerText;
        [SerializeField] private CanvasGroup _roundBannerGroup;

        [Header("Burst & Callouts")]
        [SerializeField] private Image _p1BurstFill;
        [SerializeField] private Image _p2BurstFill;
        [SerializeField] private Text _calloutBannerText;
        [SerializeField] private CanvasGroup _calloutBannerGroup;

        [Header("Combo Floater")]
        [SerializeField] private Text _comboCounterText;
        [SerializeField] private CanvasGroup _comboGroup;

        [Header("Victory Results Modal")]
        [SerializeField] private GameObject _victoryModal;
        [SerializeField] private Text _victoryWinnerText;
        [SerializeField] private Button _btnRematch;
        [SerializeField] private Button _btnRetry;
        [SerializeField] private Button _btnCharacterSelect;
        [SerializeField] private Button _btnStageSelect;
        [SerializeField] private Button _btnReturnToMenu;

        [Header("In-Game Pause Modal")]
        [SerializeField] private GameObject _pauseModal;
        [SerializeField] private Button _btnPauseResume;
        [SerializeField] private Button _btnPauseRestart;
        [SerializeField] private Button _btnPauseCharSelect;
        [SerializeField] private Button _btnPauseStageSelect;
        [SerializeField] private Button _btnPauseMenu;

        [Header("Touch HUD Controls")]
        [SerializeField] private Button _btnBattlePause;

        [Header("Online Netcode & Rollback HUD")]
        [SerializeField] private GameObject _netcodeRibbon;
        [SerializeField] private Image _pingStatusDot;
        [SerializeField] private Text _pingValueText;
        [SerializeField] private Text _rollbackFramesText;
        [SerializeField] private GameObject _disconnectRecoveryBanner;
        [SerializeField] private Text _disconnectRecoveryText;

        private bool _resultsSubmissionLocked = false;

        private FighterController _p1;
        private FighterController _p2;

        private float _p1TargetFill = 1f;
        private float _p2TargetFill = 1f;
        private Coroutine _p1LagRoutine;
        private Coroutine _p2LagRoutine;
        private Coroutine _comboFadeRoutine;
        private Coroutine _bannerRoutine;

        private int _currentComboHits = 0;
        private float _currentComboDamage = 0f;
        public int CurrentComboHits => _currentComboHits;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureHUDComponents();

            if (_victoryModal != null) _victoryModal.SetActive(false);
            if (_roundBannerGroup != null) _roundBannerGroup.alpha = 0f;
            if (_comboGroup != null) _comboGroup.alpha = 0f;
        }

        private void Start()
        {
            EnsureHUDComponents();

            if (MatchRoundReferee.Instance != null)
            {
                SubscribeReferee(MatchRoundReferee.Instance);
            }

            if (DemonIdol.Network.QuantumPvPManager.Instance != null)
            {
                DemonIdol.Network.QuantumPvPManager.Instance.OnOpponentDisconnectChanged -= HandleOpponentDisconnectStatus;
                DemonIdol.Network.QuantumPvPManager.Instance.OnOpponentDisconnectChanged += HandleOpponentDisconnectStatus;
            }

            AutoBindFightersIfMissing();
        }

        private float _lastFighterSearchTime = -10f;

        public void AutoBindFightersIfMissing()
        {
            if (_p1 != null && _p2 != null) return;
            FighterController[] fighters = FindObjectsByType<FighterController>(FindObjectsSortMode.None);
            if (fighters.Length >= 2)
            {
                FighterController p1Candidate = fighters[0];
                FighterController p2Candidate = fighters[1];
                if (!p1Candidate.isPlayerControlled && p2Candidate.isPlayerControlled)
                {
                    p1Candidate = fighters[1];
                    p2Candidate = fighters[0];
                }
                BindFighters(p1Candidate, p2Candidate);
            }
            else if (fighters.Length == 1 && _p1 == null)
            {
                BindFighters(fighters[0], null);
            }
        }

        private void OnDestroy()
        {
            if (DemonIdol.Network.QuantumPvPManager.Instance != null)
            {
                DemonIdol.Network.QuantumPvPManager.Instance.OnOpponentDisconnectChanged -= HandleOpponentDisconnectStatus;
            }

            if (_instance == this)
            {
                _instance = null;
            }
        }

        public void BindFighters(FighterController p1, FighterController p2)
        {
            if (_p1 != null)
            {
                _p1.OnHealthChanged -= OnP1HealthChanged;
                _p1.OnSuperMeterChanged -= OnP1SuperChanged;
                _p1.OnHitProcessed -= OnP1HitProcessed;
                _p1.OnRapidCanceled -= OnP1RapidCanceled;
                if (_p1.Burst != null)
                {
                    _p1.Burst.OnBurstMeterChanged -= OnP1BurstChanged;
                    _p1.Burst.OnBurstTriggered -= OnP1BurstTriggered;
                }
            }
            if (_p2 != null)
            {
                _p2.OnHealthChanged -= OnP2HealthChanged;
                _p2.OnSuperMeterChanged -= OnP2SuperChanged;
                _p2.OnHitProcessed -= OnP2HitProcessed;
                _p2.OnRapidCanceled -= OnP2RapidCanceled;
                if (_p2.Burst != null)
                {
                    _p2.Burst.OnBurstMeterChanged -= OnP2BurstChanged;
                    _p2.Burst.OnBurstTriggered -= OnP2BurstTriggered;
                }
            }

            _p1 = p1;
            _p2 = p2;

            if (_p1 != null)
            {
                if (_p1NameText != null) _p1NameText.text = _p1.fighterName.ToUpper();
                _p1.OnHealthChanged += OnP1HealthChanged;
                _p1.OnSuperMeterChanged += OnP1SuperChanged;
                _p1.OnHitProcessed += OnP1HitProcessed;
                _p1.OnRapidCanceled += OnP1RapidCanceled;
                if (_p1.Burst != null)
                {
                    _p1.Burst.OnBurstMeterChanged += OnP1BurstChanged;
                    _p1.Burst.OnBurstTriggered += OnP1BurstTriggered;
                    OnP1BurstChanged(_p1.Burst.CurrentBurstMeter, PsychicBurstSystem.MAX_BURST_METER);
                }
                OnP1HealthChanged(_p1.currentHealth, _p1.maxHealth);
                OnP1SuperChanged(_p1.currentSuperMeter, _p1.maxSuperMeter);
            }

            if (_p2 != null)
            {
                if (_p2NameText != null) _p2NameText.text = _p2.fighterName.ToUpper();
                _p2.OnHealthChanged += OnP2HealthChanged;
                _p2.OnSuperMeterChanged += OnP2SuperChanged;
                _p2.OnHitProcessed += OnP2HitProcessed;
                _p2.OnRapidCanceled += OnP2RapidCanceled;
                if (_p2.Burst != null)
                {
                    _p2.Burst.OnBurstMeterChanged += OnP2BurstChanged;
                    _p2.Burst.OnBurstTriggered += OnP2BurstTriggered;
                    OnP2BurstChanged(_p2.Burst.CurrentBurstMeter, PsychicBurstSystem.MAX_BURST_METER);
                }
                OnP2HealthChanged(_p2.currentHealth, _p2.maxHealth);
                OnP2SuperChanged(_p2.currentSuperMeter, _p2.maxSuperMeter);
            }

            ResetPips();
        }

        private void OnP1BurstChanged(float cur, float max)
        {
            if (_p1BurstFill != null) _p1BurstFill.fillAmount = max > 0 ? Mathf.Clamp01(cur / max) : 0f;
        }

        private void OnP2BurstChanged(float cur, float max)
        {
            if (_p2BurstFill != null) _p2BurstFill.fillAmount = max > 0 ? Mathf.Clamp01(cur / max) : 0f;
        }

        private void OnP1RapidCanceled()
        {
            ShowCallout("RAPID CANCEL!", new Color(1f, 0.9f, 0.2f, 1f));
        }

        private void OnP2RapidCanceled()
        {
            ShowCallout("RAPID CANCEL!", new Color(1f, 0.9f, 0.2f, 1f));
        }

        private void OnP1BurstTriggered(BurstType type)
        {
            ShowCallout(type == BurstType.GoldOffensive ? "GOLD BURST!" : "PSYCHIC BURST!", new Color(1f, 0.75f, 0.1f, 1f));
        }

        private void OnP2BurstTriggered(BurstType type)
        {
            ShowCallout(type == BurstType.GoldOffensive ? "GOLD BURST!" : "PSYCHIC BURST!", new Color(1f, 0.75f, 0.1f, 1f));
        }

        public void SubscribeReferee(MatchRoundReferee referee)
        {
            referee.OnRoundIntro += ShowRoundIntro;
            referee.OnFightBegun += ShowFightCallout;
            referee.OnTimerUpdated += UpdateTimer;
            referee.OnRoundEnded += ShowRoundEnded;
            referee.OnMatchFinished += ShowMatchFinished;
        }

        private void Update()
        {
            if ((_p1 == null || _p2 == null) && Time.unscaledTime - _lastFighterSearchTime > 1.0f)
            {
                _lastFighterSearchTime = Time.unscaledTime;
                AutoBindFightersIfMissing();
            }

            bool escapePressed = false;
            try
            {
                escapePressed = UnityEngine.Input.GetKeyDown(KeyCode.Escape);
            }
            catch (System.InvalidOperationException)
            {
#if ENABLE_INPUT_SYSTEM
                escapePressed = UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame;
#endif
            }

            if (escapePressed)
            {
                ToggleInGamePause();
            }

            UpdateProjectileMeters();
            UpdateNetcodeHUD();
        }

        private void UpdateProjectileMeters()
        {
            if (_p1 != null && _p1ProjFill != null)
            {
                _p1ProjFill.fillAmount = _p1.ProjectileMeter;
                _p1ProjFill.color = _p1.ProjectileMeter >= 0.98f ? new Color(0.1f, 0.9f, 1.0f) : new Color(0.9f, 0.6f, 0.2f);
            }
            if (_p2 != null && _p2ProjFill != null)
            {
                _p2ProjFill.fillAmount = _p2.ProjectileMeter;
                _p2ProjFill.color = _p2.ProjectileMeter >= 0.98f ? new Color(0.1f, 0.9f, 1.0f) : new Color(0.9f, 0.6f, 0.2f);
            }
        }

        private int _netcodeUpdateFrame = 0;
        private void UpdateNetcodeHUD()
        {
            _netcodeUpdateFrame++;
            if (_netcodeUpdateFrame < 30) return;
            _netcodeUpdateFrame = 0;

            var pvp = DemonIdol.Network.QuantumPvPManager.Instance;
            var bridge = DemonIdol.Network.QuantumCombatBridge.Instance;
            bool isOnline = (pvp != null && (pvp.State == DemonIdol.Network.QuantumPvPState.InMatch || pvp.State == DemonIdol.Network.QuantumPvPState.StartingSimulation)) || (bridge != null && bridge.IsQuantumActive);

            if (_netcodeRibbon != null)
            {
                _netcodeRibbon.SetActive(isOnline);
            }

            if (!isOnline) return;

            int ping = pvp != null ? pvp.CurrentPingMs : 35;
            int frames = pvp != null ? pvp.EstimatedRollbackFrames : 1;

            if (_pingValueText != null)
            {
                _pingValueText.text = $"{ping} ms";
            }

            if (_rollbackFramesText != null)
            {
                _rollbackFramesText.text = $"ROLLBACK: {frames}F";
            }

            if (_pingStatusDot != null)
            {
                if (ping < 45) _pingStatusDot.color = new Color(0f, 1f, 0.4f, 1f); // Green
                else if (ping <= 70) _pingStatusDot.color = new Color(1f, 0.85f, 0.2f, 1f); // Yellow
                else _pingStatusDot.color = new Color(1f, 0.2f, 0.2f, 1f); // Red
            }
        }

        private void HandleOpponentDisconnectStatus(bool isReconnecting, float secondsRemaining)
        {
            if (_disconnectRecoveryBanner != null)
            {
                _disconnectRecoveryBanner.SetActive(isReconnecting);
                if (isReconnecting && _disconnectRecoveryText != null)
                {
                    _disconnectRecoveryText.text = $"⚠️ OPPONENT RECONNECTING... {secondsRemaining:F1}s";
                }
            }
        }

        #region Public Bridge Methods (UFE Native / Decoupled Callers)

        public void SetFighterNames(string p1Name, string p2Name)
        {
            if (_p1NameText != null && !string.IsNullOrEmpty(p1Name)) _p1NameText.text = p1Name.ToUpper();
            if (_p2NameText != null && !string.IsNullOrEmpty(p2Name)) _p2NameText.text = p2Name.ToUpper();
        }

        public void UpdateHealth(int player, float cur, float max)
        {
            if (player == 1) OnP1HealthChanged(cur, max);
            else OnP2HealthChanged(cur, max);
        }

        public void UpdateSuper(int player, float cur, float max)
        {
            if (player == 1) OnP1SuperChanged(cur, max);
            else OnP2SuperChanged(cur, max);
        }

        public void ShowRoundEndedUFE(string reason, int winningPlayer)
        {
            string text = string.Equals(reason, "TimeOver", StringComparison.OrdinalIgnoreCase) ? "TIME OVER" : "K.O.!";
            PlayBanner(text, new Color(0.9f, 0.15f, 0.15f, 1f), 2.0f);
            UpdateWinPips();
        }

        public void ShowMatchFinishedUFE(int winningPlayer, string winnerName)
        {
            _resultsSubmissionLocked = false;
            // MatchResultsOverlay is the authoritative post-match UI with once-per-match cosmetic grants.
            // Keep internal legacy victory modal inactive to prevent dual/overlapping victory screens.
            if (_victoryModal != null)
            {
                _victoryModal.SetActive(false);
            }
        }

        #endregion

        #region Health & Meter Callbacks

        private void OnP1HealthChanged(float cur, float max)
        {
            _p1TargetFill = max > 0 ? Mathf.Clamp01(cur / max) : 0f;
            if (_p1HealthFill != null) _p1HealthFill.fillAmount = _p1TargetFill;

            if (_p1LagRoutine != null) StopCoroutine(_p1LagRoutine);
            _p1LagRoutine = StartCoroutine(DrainLagBar(_p1HealthLagFill, _p1TargetFill));
        }

        private void OnP2HealthChanged(float cur, float max)
        {
            _p2TargetFill = max > 0 ? Mathf.Clamp01(cur / max) : 0f;
            if (_p2HealthFill != null) _p2HealthFill.fillAmount = _p2TargetFill;

            if (_p2LagRoutine != null) StopCoroutine(_p2LagRoutine);
            _p2LagRoutine = StartCoroutine(DrainLagBar(_p2HealthLagFill, _p2TargetFill));
        }

        private IEnumerator DrainLagBar(Image lagBar, float targetFill)
        {
            if (lagBar == null) yield break;
            yield return new WaitForSeconds(0.35f); // Hit lag delay
            while (Mathf.Abs(lagBar.fillAmount - targetFill) > 0.005f)
            {
                lagBar.fillAmount = Mathf.Lerp(lagBar.fillAmount, targetFill, Time.deltaTime * 6f);
                yield return null;
            }
            lagBar.fillAmount = targetFill;
        }

        private void OnP1SuperChanged(float cur, float max)
        {
            float fill = max > 0 ? Mathf.Clamp01(cur / max) : 0f;
            if (_p1SuperFill != null) _p1SuperFill.fillAmount = fill;
            if (_p1DomainReadyText != null) _p1DomainReadyText.gameObject.SetActive(fill >= 1.0f);
        }

        private void OnP2SuperChanged(float cur, float max)
        {
            float fill = max > 0 ? Mathf.Clamp01(cur / max) : 0f;
            if (_p2SuperFill != null) _p2SuperFill.fillAmount = fill;
            if (_p2DomainReadyText != null) _p2DomainReadyText.gameObject.SetActive(fill >= 1.0f);
        }

        private Coroutine _calloutRoutine;

        public void ShowCallout(string text, Color color)
        {
            if (_calloutBannerText == null || _calloutBannerGroup == null) return;
            if (_calloutRoutine != null) StopCoroutine(_calloutRoutine);
            _calloutRoutine = StartCoroutine(RunCallout(text, color));
        }

        private IEnumerator RunCallout(string text, Color color)
        {
            _calloutBannerText.text = text;
            _calloutBannerText.color = color;
            _calloutBannerGroup.alpha = 1f;
            yield return new WaitForSeconds(0.75f);
            float elapsed = 0f;
            while (elapsed < 0.25f)
            {
                elapsed += Time.deltaTime;
                _calloutBannerGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / 0.25f);
                yield return null;
            }
            _calloutBannerGroup.alpha = 0f;
        }

        #endregion

        #region Combo Tracking

        private void OnP1HitProcessed(HitResult res, AttackData atk, Vector3 pos)
        {
            if (res == HitResult.CounterHit)
            {
                ShowCallout("COUNTER HIT!", new Color(1f, 0.25f, 0.2f, 1f));
            }
            else if (res == HitResult.BarrierBlocked)
            {
                ShowCallout("BARRIER BLOCK", new Color(0.2f, 0.85f, 1f, 1f));
            }

            // Only confirm damaging clean hits or counter-hits as combo hits.
            // Explicitly ignore Grabbed (break window pending), Teched, Blocked, BarrierBlocked, Parried, and Whiff.
            if ((res == HitResult.Hit || res == HitResult.CounterHit) && atk != null)
            {
                RegisterComboHit(atk.damage);
            }
        }

        private void OnP2HitProcessed(HitResult res, AttackData atk, Vector3 pos)
        {
            if (res == HitResult.CounterHit)
            {
                ShowCallout("COUNTER HIT!", new Color(1f, 0.25f, 0.2f, 1f));
            }

            if ((res == HitResult.Hit || res == HitResult.CounterHit) && atk != null)
            {
                RegisterComboHit(atk.damage);
            }
        }

        public void RegisterComboHit(float dmg)
        {
            _currentComboHits++;
            _currentComboDamage += dmg;

            if (_comboCounterText != null)
            {
                if (SettingsPanel.ShowDamageNumbers)
                {
                    _comboCounterText.text = $"{_currentComboHits} HITS!\n<size=18>{Mathf.RoundToInt(_currentComboDamage)} DMG</size>";
                }
                else
                {
                    _comboCounterText.text = $"{_currentComboHits} HITS!";
                }
            }

            if (_comboGroup != null) _comboGroup.alpha = 1f;

            if (_comboFadeRoutine != null) StopCoroutine(_comboFadeRoutine);
            _comboFadeRoutine = StartCoroutine(FadeComboRoutine());
        }

        private IEnumerator FadeComboRoutine()
        {
            yield return new WaitForSeconds(1.6f);
            float elapsed = 0f;
            while (elapsed < 0.4f)
            {
                elapsed += Time.deltaTime;
                if (_comboGroup != null) _comboGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / 0.4f);
                yield return null;
            }
            _currentComboHits = 0;
            _currentComboDamage = 0f;
        }

        #endregion

        #region Round & Match Announcements

        public void ShowRoundIntro(int round)
        {
            if (round <= 1)
            {
                ResetPips();
            }
            UpdateWinPips();
            string roundText = round switch
            {
                1 => "ROUND 1",
                2 => "ROUND 2",
                _ => "FINAL ROUND"
            };
            PlayBanner(roundText, new Color(1f, 0.85f, 0.2f, 1f), 1.2f);
        }

        public void ShowFightCallout()
        {
            PlayBanner("FIGHT!", new Color(0.9f, 0.2f, 0.2f, 1f), 0.9f);
        }

        public void UpdateTimer(int sec)
        {
            if (_timerText != null)
            {
                if (sec < 0)
                {
                    _timerText.text = "∞";
                    _timerText.color = new Color(0.2f, 0.9f, 1f, 1f);
                }
                else
                {
                    _timerText.text = sec.ToString("00");
                    _timerText.color = sec <= 10 ? new Color(1f, 0.2f, 0.2f, 1f) : Color.white;
                }
            }
        }

        public void ShowRoundEnded(RoundWinner winner, EndReason reason)
        {
            string text = reason == EndReason.TimeOut ? "TIME OVER" : "K.O.!";
            PlayBanner(text, new Color(0.9f, 0.15f, 0.15f, 1f), 2.0f);
            UpdateWinPips();
        }

        public void ShowMatchFinished(RoundWinner matchWinner)
        {
            _resultsSubmissionLocked = false;
            string winnerName = matchWinner == RoundWinner.Player1
                ? (_p1 != null ? _p1.fighterName : "PLAYER 1")
                : (matchWinner == RoundWinner.Player2
                    ? (_p2 != null ? _p2.fighterName : "PLAYER 2")
                    : "DRAW");

            // Advance story progression checkpoint if won in Story mode
            if (matchWinner == RoundWinner.Player1 && CharacterSelectUI.Instance != null && CharacterSelectUI.Instance.ActiveGameMode == "Story")
            {
                if (DemonIdol.Cutscenes.StoryCheckpointManager.Instance != null)
                {
                    DemonIdol.Cutscenes.StoryCheckpointManager.Instance.CompleteCurrentCheckpoint(null);
                }
            }

            // Commit match result and grant cosmetic XP to persistent ledger
            int winningSlot = matchWinner == RoundWinner.Player1 ? 1 : (matchWinner == RoundWinner.Player2 ? 2 : 0);
            LaunchServices.CommitAndGrant(winningSlot, out int grantedXp);

            // Record match into Firestore service (ELO & Match history)
            if (DemonIdol.Firebase.FirestoreService.Instance != null)
            {
                string p1Id = _p1 != null ? _p1.fighterName : "P1";
                string p2Id = _p2 != null ? _p2.fighterName : "P2";
                string winnerId = matchWinner == RoundWinner.Player1 ? p1Id : (matchWinner == RoundWinner.Player2 ? p2Id : "Draw");
                float elapsed = 99f - (MatchRoundReferee.Instance != null ? MatchRoundReferee.Instance.SecondsRemaining : 0f);
                _ = DemonIdol.Firebase.FirestoreService.Instance.RecordMatchResultAsync(p1Id, p2Id, winnerId, "Knockout", elapsed);
            }

            if (_victoryModal != null)
            {
                _victoryModal.SetActive(true);
                if (_victoryWinnerText != null)
                {
                    _victoryWinnerText.text = matchWinner == RoundWinner.Draw
                        ? "MATCH DRAW"
                        : $"VICTORY: {winnerName.ToUpper()}";
                }
            }
        }

        private void PlayBanner(string message, Color col, float duration)
        {
            if (_bannerRoutine != null) StopCoroutine(_bannerRoutine);
            bool reduced = SettingsPanel.ReducedMotion;
            float actualDuration = reduced ? SumiStyleTokens.BannerDurationReduced : duration;
            _bannerRoutine = StartCoroutine(BannerSequence(message, col, actualDuration, reduced));
        }

        private IEnumerator BannerSequence(string message, Color col, float duration, bool reducedMotion)
        {
            if (_roundBannerText != null)
            {
                _roundBannerText.text = message;
                _roundBannerText.color = col;
            }

            if (_roundBannerGroup != null)
            {
                _roundBannerGroup.alpha = 1f;
                yield return new WaitForSeconds(duration);

                float fadeTime = reducedMotion ? 0.05f : 0.25f;
                float t = 0f;
                while (t < fadeTime)
                {
                    t += Time.deltaTime;
                    _roundBannerGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeTime);
                    yield return null;
                }
                _roundBannerGroup.alpha = 0f;
            }
        }

        private void UpdateWinPips()
        {
            if (MatchRoundReferee.Instance == null) return;
            int p1Wins = MatchRoundReferee.Instance.P1Wins;
            int p2Wins = MatchRoundReferee.Instance.P2Wins;
            var atlas = BloodlinesUIAtlas.Instance;

            if (_p1WinPips != null)
            {
                for (int i = 0; i < _p1WinPips.Length; i++)
                {
                    if (_p1WinPips[i] != null)
                    {
                        if (atlas != null)
                        {
                            _p1WinPips[i].sprite = i < p1Wins ? atlas.PBRoundFull_V1 : atlas.PBRoundEmpty_V1V4;
                            _p1WinPips[i].color = Color.white;
                        }
                        else
                        {
                            _p1WinPips[i].color = i < p1Wins ? new Color(1f, 0.85f, 0.2f, 1f) : new Color(0.2f, 0.2f, 0.2f, 0.8f);
                        }
                    }
                }
            }

            if (_p2WinPips != null)
            {
                for (int i = 0; i < _p2WinPips.Length; i++)
                {
                    if (_p2WinPips[i] != null)
                    {
                        if (atlas != null)
                        {
                            _p2WinPips[i].sprite = i < p2Wins ? atlas.PBRoundFull_V1 : atlas.PBRoundEmpty_V1V4;
                            _p2WinPips[i].color = Color.white;
                        }
                        else
                        {
                            _p2WinPips[i].color = i < p2Wins ? new Color(1f, 0.85f, 0.2f, 1f) : new Color(0.2f, 0.2f, 0.2f, 0.8f);
                        }
                    }
                }
            }
        }

        private void ResetPips()
        {
            var atlas = BloodlinesUIAtlas.Instance;
            if (_p1WinPips != null)
            {
                foreach (var p in _p1WinPips)
                {
                    if (p != null)
                    {
                        if (atlas != null && atlas.PBRoundEmpty_V1V4 != null) { p.sprite = atlas.PBRoundEmpty_V1V4; p.color = Color.white; }
                        else { p.color = new Color(0.2f, 0.2f, 0.2f, 0.8f); }
                    }
                }
            }
            if (_p2WinPips != null)
            {
                foreach (var p in _p2WinPips)
                {
                    if (p != null)
                    {
                        if (atlas != null && atlas.PBRoundEmpty_V1V4 != null) { p.sprite = atlas.PBRoundEmpty_V1V4; p.color = Color.white; }
                        else { p.color = new Color(0.2f, 0.2f, 0.2f, 0.8f); }
                    }
                }
            }
        }

        #endregion

        #region Buttons

        public void EnsureHUDComponents()
        {
            if (_p1HealthFill == null)
            {
                EnsureRuntimeHUDHierarchy();
            }

            // Touch Pause Button (Top Right)
            if (_btnBattlePause == null)
            {
                Transform existing = transform.Find("Btn_BattlePause");
                if (existing != null)
                {
                    _btnBattlePause = existing.GetComponent<Button>();
                }
                else
                {
                    BloodlinesUIAtlas.EnsureInstance();
                    var atlas = BloodlinesUIAtlas.Instance;

                    GameObject pauseBtnObj = new GameObject("Btn_BattlePause", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                    pauseBtnObj.transform.SetParent(transform, false);
                    RectTransform rt = pauseBtnObj.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(1f, 1f);
                    rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot = new Vector2(1f, 1f);
                    rt.anchoredPosition = new Vector2(-25f, -25f);
                    rt.sizeDelta = new Vector2(70f, 40f);

                    Image img = pauseBtnObj.GetComponent<Image>();
                    _btnBattlePause = pauseBtnObj.GetComponent<Button>();
                    _btnBattlePause.targetGraphic = img;
                    if (atlas != null) atlas.StyleButton(_btnBattlePause, 3, false);
                    else img.color = new Color(0.12f, 0.12f, 0.16f, 0.88f);

                    Font font = atlas != null ? atlas.BloodlinesFont : null;
                    if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

                    GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                    txtObj.transform.SetParent(pauseBtnObj.transform, false);
                    RectTransform trt = txtObj.GetComponent<RectTransform>();
                    trt.anchorMin = Vector2.zero;
                    trt.anchorMax = Vector2.one;
                    trt.sizeDelta = Vector2.zero;
                    Text txt = txtObj.GetComponent<Text>();
                    txt.font = font;
                    txt.text = "PAUSE";
                    txt.fontSize = 14;
                    txt.fontStyle = FontStyle.Bold;
                    txt.alignment = TextAnchor.MiddleCenter;
                    txt.color = new Color(0.95f, 0.95f, 0.95f, 1f);
                }
            }

            if (_btnBattlePause != null)
            {
                _btnBattlePause.onClick.RemoveListener(OnClickBattlePause);
                _btnBattlePause.onClick.AddListener(OnClickBattlePause);
            }

            // Ensure Victory, Pause and Netcode HUD Wiring
            EnsureVictoryModal();
            EnsurePauseModal();
            EnsureNetcodeHUD();
        }

        public void EnsureNetcodeHUD()
        {
            if (_netcodeRibbon == null)
            {
                Transform existing = transform.Find("Netcode_Ribbon");
                if (existing != null)
                {
                    _netcodeRibbon = existing.gameObject;
                    _pingStatusDot = existing.Find("Dot_Status")?.GetComponent<Image>();
                    _pingValueText = existing.Find("Text_Ping")?.GetComponent<Text>();
                    _rollbackFramesText = existing.Find("Text_Rollback")?.GetComponent<Text>();
                }
                else
                {
                    BloodlinesUIAtlas.EnsureInstance();
                    var atlas = BloodlinesUIAtlas.Instance;
                    Font font = atlas != null ? atlas.BloodlinesFont : null;
                    if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

                    _netcodeRibbon = new GameObject("Netcode_Ribbon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    _netcodeRibbon.transform.SetParent(transform, false);
                    RectTransform rt = _netcodeRibbon.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 1f);
                    rt.anchorMax = new Vector2(0.5f, 1f);
                    rt.pivot = new Vector2(0.5f, 1f);
                    rt.anchoredPosition = new Vector2(0f, -65f);
                    rt.sizeDelta = new Vector2(250f, 26f);

                    Image bg = _netcodeRibbon.GetComponent<Image>();
                    bg.color = new Color(0.05f, 0.05f, 0.08f, 0.85f);

                    // Dot
                    GameObject dotObj = new GameObject("Dot_Status", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    dotObj.transform.SetParent(_netcodeRibbon.transform, false);
                    RectTransform drt = dotObj.GetComponent<RectTransform>();
                    drt.anchorMin = new Vector2(0f, 0.5f);
                    drt.anchorMax = new Vector2(0f, 0.5f);
                    drt.pivot = new Vector2(0f, 0.5f);
                    drt.anchoredPosition = new Vector2(10f, 0f);
                    drt.sizeDelta = new Vector2(10f, 10f);
                    _pingStatusDot = dotObj.GetComponent<Image>();
                    _pingStatusDot.color = new Color(0f, 1f, 0.4f, 1f);

                    // Ping Text
                    GameObject pingObj = new GameObject("Text_Ping", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                    pingObj.transform.SetParent(_netcodeRibbon.transform, false);
                    RectTransform prt = pingObj.GetComponent<RectTransform>();
                    prt.anchorMin = new Vector2(0f, 0f);
                    prt.anchorMax = new Vector2(0.5f, 1f);
                    prt.anchoredPosition = new Vector2(26f, 0f);
                    prt.sizeDelta = Vector2.zero;
                    _pingValueText = pingObj.GetComponent<Text>();
                    _pingValueText.font = font;
                    _pingValueText.text = "35 ms";
                    _pingValueText.fontSize = 12;
                    _pingValueText.alignment = TextAnchor.MiddleLeft;
                    _pingValueText.color = Color.white;

                    // Rollback text
                    GameObject rbObj = new GameObject("Text_Rollback", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                    rbObj.transform.SetParent(_netcodeRibbon.transform, false);
                    RectTransform rbrt = rbObj.GetComponent<RectTransform>();
                    rbrt.anchorMin = new Vector2(0.5f, 0f);
                    rbrt.anchorMax = new Vector2(1f, 1f);
                    rbrt.anchoredPosition = new Vector2(-10f, 0f);
                    rbrt.sizeDelta = Vector2.zero;
                    _rollbackFramesText = rbObj.GetComponent<Text>();
                    _rollbackFramesText.font = font;
                    _rollbackFramesText.text = "ROLLBACK: 1F";
                    _rollbackFramesText.fontSize = 12;
                    _rollbackFramesText.alignment = TextAnchor.MiddleRight;
                    _rollbackFramesText.color = new Color(0.2f, 0.9f, 1f, 1f);
                }
            }

            // Disconnect banner
            if (_disconnectRecoveryBanner == null)
            {
                Transform existingBanner = transform.Find("Banner_OpponentDisconnect");
                if (existingBanner != null)
                {
                    _disconnectRecoveryBanner = existingBanner.gameObject;
                    _disconnectRecoveryText = existingBanner.Find("Text")?.GetComponent<Text>();
                }
                else
                {
                    BloodlinesUIAtlas.EnsureInstance();
                    var atlas = BloodlinesUIAtlas.Instance;
                    Font font = atlas != null ? atlas.BloodlinesFont : null;
                    if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

                    _disconnectRecoveryBanner = new GameObject("Banner_OpponentDisconnect", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    _disconnectRecoveryBanner.transform.SetParent(transform, false);
                    RectTransform rt = _disconnectRecoveryBanner.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 0.7f);
                    rt.anchorMax = new Vector2(0.5f, 0.7f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = new Vector2(580f, 55f);

                    Image bg = _disconnectRecoveryBanner.GetComponent<Image>();
                    bg.color = new Color(0.7f, 0.1f, 0.1f, 0.92f);

                    GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                    txtObj.transform.SetParent(_disconnectRecoveryBanner.transform, false);
                    RectTransform trt = txtObj.GetComponent<RectTransform>();
                    trt.anchorMin = Vector2.zero;
                    trt.anchorMax = Vector2.one;
                    trt.sizeDelta = Vector2.zero;
                    _disconnectRecoveryText = txtObj.GetComponent<Text>();
                    _disconnectRecoveryText.font = font;
                    _disconnectRecoveryText.text = "⚠️ OPPONENT RECONNECTING... 5.0s";
                    _disconnectRecoveryText.fontSize = 16;
                    _disconnectRecoveryText.fontStyle = FontStyle.Bold;
                    _disconnectRecoveryText.alignment = TextAnchor.MiddleCenter;
                    _disconnectRecoveryText.color = Color.white;

                    _disconnectRecoveryBanner.SetActive(false);
                }
            }
        }

        public void EnsureVictoryModal()
        {
            if (_victoryModal == null)
            {
                Transform v = transform.Find("Modal_Victory");
                if (v != null)
                {
                    _victoryModal = v.gameObject;
                }
                else
                {
                    _victoryModal = BuildVictoryModal();
                }
            }

            if (_victoryModal != null)
            {
                if (_victoryWinnerText == null) _victoryWinnerText = _victoryModal.GetComponentInChildren<Text>();
                if (_btnRematch == null) _btnRematch = FindButtonRecursive(_victoryModal, "Btn_Rematch", "Button_Rematch");
                if (_btnRetry == null) _btnRetry = FindButtonRecursive(_victoryModal, "Btn_Retry", "Button_Retry");
                if (_btnCharacterSelect == null) _btnCharacterSelect = FindButtonRecursive(_victoryModal, "Btn_CharacterSelect", "Button_CharSelect");
                if (_btnStageSelect == null) _btnStageSelect = FindButtonRecursive(_victoryModal, "Btn_StageSelect", "Button_StageSelect");
                if (_btnReturnToMenu == null) _btnReturnToMenu = FindButtonRecursive(_victoryModal, "Btn_ReturnToMenu", "Btn_Menu", "Button_ReturnToMenu");

                if (_btnRematch != null) { _btnRematch.onClick.RemoveListener(OnClickRematch); _btnRematch.onClick.AddListener(OnClickRematch); }
                if (_btnRetry != null) { _btnRetry.onClick.RemoveListener(OnClickRetry); _btnRetry.onClick.AddListener(OnClickRetry); }
                if (_btnCharacterSelect != null) { _btnCharacterSelect.onClick.RemoveListener(OnClickCharacterSelect); _btnCharacterSelect.onClick.AddListener(OnClickCharacterSelect); }
                if (_btnStageSelect != null) { _btnStageSelect.onClick.RemoveListener(OnClickStageSelect); _btnStageSelect.onClick.AddListener(OnClickStageSelect); }
                if (_btnReturnToMenu != null) { _btnReturnToMenu.onClick.RemoveListener(OnClickReturnToMenu); _btnReturnToMenu.onClick.AddListener(OnClickReturnToMenu); }
            }
        }

        public void EnsureRuntimeHUDHierarchy()
        {
            if (_p1HealthFill != null) return;

            BloodlinesUIAtlas.EnsureInstance();
            var atlas = BloodlinesUIAtlas.Instance;

            Font font = atlas != null ? atlas.BloodlinesFont : null;
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Ensure Canvas on parent/root if not present
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                var canvasGo = new GameObject("DemonIdolBattleCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 200;
                var scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                transform.SetParent(canvasGo.transform, false);
            }

            RectTransform rootRt = GetComponent<RectTransform>();
            if (rootRt == null) rootRt = gameObject.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.sizeDelta = Vector2.zero;

            Color goldAccent = new Color(1f, 0.84f, 0f, 1f);

            // P1 Health Bar — Bloodlines progress bar
            GameObject p1BarGo = CreateRuntimePanel(gameObject, "P1_HealthBar", Color.white, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(380f, -50f), new Vector2(650f, 42f));
            if (atlas != null) atlas.StyleProgressBar(p1BarGo.GetComponent<Image>(), null, 1);
            _p1HealthLagFill = CreateRuntimeFill(p1BarGo, "LagFill", new Color(0.95f, 0.75f, 0.1f, 1f), Image.FillMethod.Horizontal, false);
            _p1HealthFill = CreateRuntimeFill(p1BarGo, "HpFill", Color.white, Image.FillMethod.Horizontal, false);
            if (atlas != null) { atlas.StyleProgressBar(null, _p1HealthFill, 1); }
            _p1NameText = CreateRuntimeText(p1BarGo, "Text_P1Name", "KAI", new Vector2(-220f, 32f), 22, Color.white, FontStyle.Bold, font);

            // P1 Projectile Meter (1.8s Rapid Recharge)
            GameObject p1ProjBarGo = CreateRuntimePanel(gameObject, "P1_ProjMeter", new Color(0.08f, 0.1f, 0.14f, 0.85f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(300f, -76f), new Vector2(490f, 10f));
            _p1ProjFill = CreateRuntimeFill(p1ProjBarGo, "Fill", new Color(0.1f, 0.85f, 1.0f, 1f), Image.FillMethod.Horizontal, false);

            _p1WinPips = new Image[2];
            for (int i = 0; i < 2; i++)
            {
                GameObject pip = CreateRuntimePanel(p1BarGo, $"Pip_{i}", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-100f + (i * 24f), 32f), new Vector2(20f, 20f));
                _p1WinPips[i] = pip.GetComponent<Image>();
                if (atlas != null && atlas.PBRoundEmpty_V1V4 != null)
                {
                    _p1WinPips[i].sprite = atlas.PBRoundEmpty_V1V4;
                    _p1WinPips[i].type = Image.Type.Simple;
                    _p1WinPips[i].color = Color.white;
                }
                else
                {
                    _p1WinPips[i].color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                }
            }

            // P2 Health Bar — Bloodlines progress bar
            GameObject p2BarGo = CreateRuntimePanel(gameObject, "P2_HealthBar", Color.white, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-380f, -50f), new Vector2(650f, 42f));
            if (atlas != null) atlas.StyleProgressBar(p2BarGo.GetComponent<Image>(), null, 1);
            _p2HealthLagFill = CreateRuntimeFill(p2BarGo, "LagFill", new Color(0.95f, 0.75f, 0.1f, 1f), Image.FillMethod.Horizontal, true);
            _p2HealthFill = CreateRuntimeFill(p2BarGo, "HpFill", Color.white, Image.FillMethod.Horizontal, true);
            if (atlas != null) { atlas.StyleProgressBar(null, _p2HealthFill, 2); }
            _p2NameText = CreateRuntimeText(p2BarGo, "Text_P2Name", "OPPONENT", new Vector2(220f, 32f), 22, Color.white, FontStyle.Bold, font);

            // P2 Projectile Meter (1.8s Rapid Recharge)
            GameObject p2ProjBarGo = CreateRuntimePanel(gameObject, "P2_ProjMeter", new Color(0.08f, 0.1f, 0.14f, 0.85f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-300f, -76f), new Vector2(490f, 10f));
            _p2ProjFill = CreateRuntimeFill(p2ProjBarGo, "Fill", new Color(0.1f, 0.85f, 1.0f, 1f), Image.FillMethod.Horizontal, true);

            _p2WinPips = new Image[2];
            for (int i = 0; i < 2; i++)
            {
                GameObject pip = CreateRuntimePanel(p2BarGo, $"Pip_{i}", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(100f - (i * 24f), 32f), new Vector2(20f, 20f));
                _p2WinPips[i] = pip.GetComponent<Image>();
                if (atlas != null && atlas.PBRoundEmpty_V1V4 != null)
                {
                    _p2WinPips[i].sprite = atlas.PBRoundEmpty_V1V4;
                    _p2WinPips[i].type = Image.Type.Simple;
                    _p2WinPips[i].color = Color.white;
                }
                else
                {
                    _p2WinPips[i].color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                }
            }

            // Timer Box — Bloodlines frame
            GameObject timerBox = CreateRuntimePanel(gameObject, "TimerBox", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -55f), new Vector2(120f, 75f));
            if (atlas != null) atlas.StyleOutlineFrame(timerBox.GetComponent<Image>(), false, true);
            _timerText = CreateRuntimeText(timerBox, "Text_Timer", "99", Vector2.zero, 50, goldAccent, FontStyle.Bold, font);

            // P1 Super Meter — Bloodlines progress bar variant 3
            GameObject p1SuperGo = CreateRuntimePanel(gameObject, "P1_SuperMeter", Color.white, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(280f, 45f), new Vector2(480f, 26f));
            if (atlas != null) atlas.StyleProgressBar(p1SuperGo.GetComponent<Image>(), null, 3);
            _p1SuperFill = CreateRuntimeFill(p1SuperGo, "Fill", Color.white, Image.FillMethod.Horizontal, false);
            if (atlas != null) atlas.StyleProgressBar(null, _p1SuperFill, 3);
            _p1DomainReadyText = CreateRuntimeText(p1SuperGo, "DomainReady", "★ DOMAIN EXPANSION READY ★", new Vector2(0f, 26f), 15, new Color(0f, 0.85f, 1f, 1f), FontStyle.Bold, font);
            _p1DomainReadyText.gameObject.SetActive(false);

            // P2 Super Meter — Bloodlines progress bar variant 3
            GameObject p2SuperGo = CreateRuntimePanel(gameObject, "P2_SuperMeter", Color.white, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-280f, 45f), new Vector2(480f, 26f));
            if (atlas != null) atlas.StyleProgressBar(p2SuperGo.GetComponent<Image>(), null, 3);
            _p2SuperFill = CreateRuntimeFill(p2SuperGo, "Fill", Color.white, Image.FillMethod.Horizontal, true);
            if (atlas != null) atlas.StyleProgressBar(null, _p2SuperFill, 3);
            _p2DomainReadyText = CreateRuntimeText(p2SuperGo, "DomainReady", "★ DOMAIN EXPANSION READY ★", new Vector2(0f, 26f), 15, new Color(0f, 0.85f, 1f, 1f), FontStyle.Bold, font);
            _p2DomainReadyText.gameObject.SetActive(false);

            // Round Banner — Bloodlines notice frame
            GameObject bannerGo = CreateRuntimePanel(gameObject, "RoundBanner", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 140f), new Vector2(800f, 120f));
            if (atlas != null) atlas.StyleNotice(bannerGo.GetComponent<Image>(), 2);
            _roundBannerGroup = bannerGo.AddComponent<CanvasGroup>();
            _roundBannerGroup.alpha = 0f;
            _roundBannerGroup.blocksRaycasts = false;
            _roundBannerText = CreateRuntimeText(bannerGo, "BannerText", "FIGHT!", Vector2.zero, 64, new Color(0.9f, 0.15f, 0.15f, 1f), FontStyle.Bold, font);

            // Callout Floater — Bloodlines notice frame
            GameObject calloutGo = CreateRuntimePanel(gameObject, "CalloutBanner", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(800f, 80f));
            if (atlas != null) atlas.StyleNotice(calloutGo.GetComponent<Image>(), 1);
            _calloutBannerGroup = calloutGo.AddComponent<CanvasGroup>();
            _calloutBannerGroup.alpha = 0f;
            _calloutBannerGroup.blocksRaycasts = false;
            _calloutBannerText = CreateRuntimeText(calloutGo, "CalloutText", "", Vector2.zero, 36, goldAccent, FontStyle.Bold, font);

            // Combo Floater
            GameObject comboGo = CreateRuntimePanel(gameObject, "ComboFloater", Color.clear, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-220f, 50f), new Vector2(250f, 100f));
            _comboGroup = comboGo.AddComponent<CanvasGroup>();
            _comboGroup.alpha = 0f;
            _comboGroup.blocksRaycasts = false;
            _comboCounterText = CreateRuntimeText(comboGo, "ComboText", "2 HITS!", Vector2.zero, 32, goldAccent, FontStyle.Bold, font);
        }

        private static GameObject CreateRuntimePanel(GameObject parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent.transform, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            Image img = go.GetComponent<Image>();
            img.color = color;
            return go;
        }

        private static Image CreateRuntimeFill(GameObject parent, string name, Color color, Image.FillMethod fillMethod, bool reverse)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent.transform, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            Image img = go.GetComponent<Image>();
            img.color = color;
            img.type = Image.Type.Filled;
            img.fillMethod = fillMethod;
            img.fillOrigin = reverse ? (int)Image.OriginHorizontal.Right : (int)Image.OriginHorizontal.Left;
            img.fillAmount = 1f;
            return img;
        }

        private static Text CreateRuntimeText(GameObject parent, string name, string content, Vector2 anchoredPos, int fontSize, Color color, FontStyle style, Font font)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent.transform, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(500f, 60f);
            Text txt = go.GetComponent<Text>();
            txt.font = font;
            txt.text = content;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = color;
            return txt;
        }

        private GameObject BuildVictoryModal()
        {
            BloodlinesUIAtlas.EnsureInstance();
            var atlas = BloodlinesUIAtlas.Instance;

            GameObject modal = new GameObject("Modal_Victory", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            modal.transform.SetParent(transform, false);
            RectTransform rt = modal.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            Image img = modal.GetComponent<Image>();
            img.color = new Color(0.04f, 0.04f, 0.06f, 0.92f);

            Font font = atlas != null ? atlas.BloodlinesFont : null;
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Box — Bloodlines red main menu frame
            GameObject box = new GameObject("VictoryBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            box.transform.SetParent(modal.transform, false);
            RectTransform boxRt = box.GetComponent<RectTransform>();
            boxRt.sizeDelta = new Vector2(500f, 440f);
            boxRt.anchoredPosition = Vector2.zero;
            Image boxImg = box.GetComponent<Image>();
            if (atlas != null) atlas.StylePanel(boxImg, true);
            else boxImg.color = new Color(0.08f, 0.10f, 0.14f, 0.98f);

            // Title / Winner Text
            GameObject txtGo = new GameObject("Text_Winner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            txtGo.transform.SetParent(box.transform, false);
            RectTransform txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchoredPosition = new Vector2(0f, 150f);
            txtRt.sizeDelta = new Vector2(460f, 50f);
            _victoryWinnerText = txtGo.GetComponent<Text>();
            _victoryWinnerText.font = font;
            _victoryWinnerText.text = "MATCH OVER";
            _victoryWinnerText.fontSize = 24;
            _victoryWinnerText.fontStyle = FontStyle.Bold;
            _victoryWinnerText.alignment = TextAnchor.MiddleCenter;
            _victoryWinnerText.color = SumiStyleTokens.BrushGold;

            // Buttons — Bloodlines styled
            _btnRematch = CreateModalButton(box.transform, "Btn_Rematch", "REMATCH", new Vector2(0f, 75f), SumiStyleTokens.ToriiRed, font);
            if (atlas != null) atlas.StyleButton(_btnRematch, 1, true);
            _btnRematch.onClick.AddListener(OnClickRematch);

            _btnCharacterSelect = CreateModalButton(box.transform, "Btn_CharacterSelect", "CHARACTER SELECT", new Vector2(0f, 20f), new Color(0.18f, 0.22f, 0.30f, 1f), font);
            if (atlas != null) atlas.StyleButton(_btnCharacterSelect, 2, false);
            _btnCharacterSelect.onClick.AddListener(OnClickCharacterSelect);

            _btnStageSelect = CreateModalButton(box.transform, "Btn_StageSelect", "STAGE SELECT", new Vector2(0f, -35f), new Color(0.14f, 0.18f, 0.24f, 1f), font);
            if (atlas != null) atlas.StyleButton(_btnStageSelect, 2, false);
            _btnStageSelect.onClick.AddListener(OnClickStageSelect);

            _btnReturnToMenu = CreateModalButton(box.transform, "Btn_ReturnToMenu", "RETURN TO MENU", new Vector2(0f, -90f), SumiStyleTokens.CarbonInkWash, font);
            if (atlas != null) atlas.StyleButton(_btnReturnToMenu, 2, false);
            _btnReturnToMenu.onClick.AddListener(OnClickReturnToMenu);

            modal.SetActive(false);
            return modal;
        }

        private Button CreateModalButton(Transform parent, string name, string label, Vector2 pos, Color col, Font font)
        {
            GameObject btnGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);
            RectTransform rt = btnGo.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(320f, 44f);
            Image img = btnGo.GetComponent<Image>();
            img.color = col;

            Button btn = btnGo.GetComponent<Button>();
            btn.targetGraphic = img;

            GameObject tGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            tGo.transform.SetParent(btnGo.transform, false);
            RectTransform trt = tGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;
            Text t = tGo.GetComponent<Text>();
            t.font = font;
            t.text = label;
            t.fontSize = 14;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;

            return btn;
        }

        private void SafePlayButtonClick()
        {
            var audio = DemonIdol.Audio.CombatAudioDirector.Instance;
            if (audio != null)
            {
                audio.PlayButtonClick();
            }
        }

        public void OnClickBattlePause()
        {
            SafePlayButtonClick();
            ToggleInGamePause();
        }

        public void ToggleInGamePause()
        {
            EnsurePauseModal();
            if (_pauseModal == null) return;

            bool isNowPaused = !_pauseModal.activeSelf;
            _pauseModal.SetActive(isNowPaused);

            if (CombatTickEngine.Instance != null)
            {
                CombatTickEngine.Instance.PauseSimulation(isNowPaused);
            }

            if (BloodTechUIManager.Instance != null)
            {
                BloodTechUIManager.Instance.ToggleLocalPause(isNowPaused);
            }
        }

        public void EnsurePauseModal()
        {
            if (_pauseModal == null)
            {
                Transform p = transform.Find("Modal_InGamePause");
                if (p != null)
                {
                    _pauseModal = p.gameObject;
                }
                else
                {
                    _pauseModal = BuildPauseModal();
                }
            }

            if (_pauseModal != null)
            {
                if (_btnPauseResume == null) _btnPauseResume = FindButtonRecursive(_pauseModal, "Btn_PauseResume", "Btn_Resume");
                if (_btnPauseRestart == null) _btnPauseRestart = FindButtonRecursive(_pauseModal, "Btn_PauseRestart", "Btn_Restart");
                if (_btnPauseCharSelect == null) _btnPauseCharSelect = FindButtonRecursive(_pauseModal, "Btn_PauseCharSelect", "Btn_CharacterSelect");
                if (_btnPauseStageSelect == null) _btnPauseStageSelect = FindButtonRecursive(_pauseModal, "Btn_PauseStageSelect", "Btn_StageSelect");
                if (_btnPauseMenu == null) _btnPauseMenu = FindButtonRecursive(_pauseModal, "Btn_PauseMenu", "Btn_ReturnToMenu");

                if (_btnPauseResume != null) { _btnPauseResume.onClick.RemoveAllListeners(); _btnPauseResume.onClick.AddListener(ToggleInGamePause); }
                if (_btnPauseRestart != null) { _btnPauseRestart.onClick.RemoveAllListeners(); _btnPauseRestart.onClick.AddListener(() => { ToggleInGamePause(); OnClickRematch(); }); }
                if (_btnPauseCharSelect != null) { _btnPauseCharSelect.onClick.RemoveAllListeners(); _btnPauseCharSelect.onClick.AddListener(OnClickCharacterSelect); }
                if (_btnPauseStageSelect != null) { _btnPauseStageSelect.onClick.RemoveAllListeners(); _btnPauseStageSelect.onClick.AddListener(OnClickStageSelect); }
                if (_btnPauseMenu != null) { _btnPauseMenu.onClick.RemoveAllListeners(); _btnPauseMenu.onClick.AddListener(OnClickReturnToMenu); }
            }
        }

        private GameObject BuildPauseModal()
        {
            BloodlinesUIAtlas.EnsureInstance();
            var atlas = BloodlinesUIAtlas.Instance;

            GameObject modal = new GameObject("Modal_InGamePause", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            modal.transform.SetParent(transform, false);
            RectTransform modalRt = modal.GetComponent<RectTransform>();
            modalRt.anchorMin = Vector2.zero;
            modalRt.anchorMax = Vector2.one;
            modalRt.sizeDelta = Vector2.zero;
            Image modalImg = modal.GetComponent<Image>();
            modalImg.color = new Color(0f, 0f, 0f, 0.78f);

            Font font = atlas != null ? atlas.BloodlinesFont : null;
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Box — Bloodlines frame
            GameObject box = new GameObject("PauseBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            box.transform.SetParent(modal.transform, false);
            RectTransform boxRt = box.GetComponent<RectTransform>();
            boxRt.sizeDelta = new Vector2(460f, 420f);
            boxRt.anchoredPosition = Vector2.zero;
            Image boxImg = box.GetComponent<Image>();
            if (atlas != null) atlas.StylePanel(boxImg, false);
            else boxImg.color = new Color(0.08f, 0.10f, 0.14f, 0.98f);

            // Title
            GameObject txtGo = new GameObject("Text_PauseTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            txtGo.transform.SetParent(box.transform, false);
            RectTransform txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchoredPosition = new Vector2(0f, 150f);
            txtRt.sizeDelta = new Vector2(400f, 40f);
            Text pTitle = txtGo.GetComponent<Text>();
            pTitle.font = font;
            pTitle.text = "PAUSED";
            pTitle.fontSize = 24;
            pTitle.fontStyle = FontStyle.Bold;
            pTitle.alignment = TextAnchor.MiddleCenter;
            pTitle.color = SumiStyleTokens.BrushGold;

            // Buttons — Bloodlines styled
            _btnPauseResume = CreateModalButton(box.transform, "Btn_PauseResume", "RESUME", new Vector2(0f, 80f), SumiStyleTokens.ToriiRed, font);
            if (atlas != null) atlas.StyleButton(_btnPauseResume, 1, true);
            _btnPauseResume.onClick.AddListener(ToggleInGamePause);

            _btnPauseRestart = CreateModalButton(box.transform, "Btn_PauseRestart", "RESTART MATCH", new Vector2(0f, 25f), new Color(0.18f, 0.22f, 0.30f, 1f), font);
            if (atlas != null) atlas.StyleButton(_btnPauseRestart, 2, false);
            _btnPauseRestart.onClick.AddListener(() =>
            {
                ToggleInGamePause();
                OnClickRematch();
            });

            _btnPauseCharSelect = CreateModalButton(box.transform, "Btn_PauseCharSelect", "CHARACTER SELECT", new Vector2(0f, -30f), new Color(0.14f, 0.18f, 0.24f, 1f), font);
            if (atlas != null) atlas.StyleButton(_btnPauseCharSelect, 2, false);
            _btnPauseCharSelect.onClick.AddListener(OnClickCharacterSelect);

            _btnPauseStageSelect = CreateModalButton(box.transform, "Btn_PauseStageSelect", "STAGE SELECT", new Vector2(0f, -85f), new Color(0.14f, 0.18f, 0.24f, 1f), font);
            if (atlas != null) atlas.StyleButton(_btnPauseStageSelect, 2, false);
            _btnPauseStageSelect.onClick.AddListener(OnClickStageSelect);

            _btnPauseMenu = CreateModalButton(box.transform, "Btn_PauseMenu", "RETURN TO MENU", new Vector2(0f, -140f), SumiStyleTokens.CarbonInkWash, font);
            if (atlas != null) atlas.StyleButton(_btnPauseMenu, 3, false);
            _btnPauseMenu.onClick.AddListener(OnClickReturnToMenu);

            modal.SetActive(false);
            return modal;
        }

        private Button FindButtonRecursive(GameObject root, params string[] candidateNames)
        {
            if (root == null) return null;
            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                foreach (var name in candidateNames)
                {
                    if (string.Equals(b.gameObject.name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        return b;
                    }
                }
            }
            return null;
        }

        public void OnClickRematch()
        {
            if (_resultsSubmissionLocked) return;
            _resultsSubmissionLocked = true;
            SafePlayButtonClick();

            if (_victoryModal != null) _victoryModal.SetActive(false);
            if (DemonIdol.Combat.HomegrownCombatDirector.Instance != null)
            {
                DemonIdol.Combat.HomegrownCombatDirector.Instance.Rematch();
            }
            else if (MatchRoundReferee.Instance != null)
            {
                MatchRoundReferee.Instance.Rematch();
            }
        }

        public void OnClickRetry()
        {
            if (_resultsSubmissionLocked) return;
            _resultsSubmissionLocked = true;
            SafePlayButtonClick();

            if (_victoryModal != null) _victoryModal.SetActive(false);
            if (MatchRoundReferee.Instance != null)
            {
                MatchRoundReferee.Instance.StartMatch();
            }
        }

        public void OnClickCharacterSelect()
        {
            if (_resultsSubmissionLocked) return;
            _resultsSubmissionLocked = true;
            SafePlayButtonClick();

            if (_victoryModal != null) _victoryModal.SetActive(false);
            if (MatchRoundReferee.Instance != null)
            {
                MatchRoundReferee.Instance.StopMatch();
            }
            if (DemonIdol.Network.QuantumPvPManager.Instance != null)
            {
                DemonIdol.Network.QuantumPvPManager.Instance.LeaveMatch();
            }
            PlayerPrefs.SetString("MDI_MenuInitialState", "CharacterSelect");
            PlayerPrefs.SetInt("DemonIdol_ReturnToCharSelect", 1);
            PlayerPrefs.Save();
            if (BloodTechUIManager.Instance != null)
            {
                string mode = CharacterSelectUI.Instance != null ? CharacterSelectUI.Instance.ActiveGameMode : "Training";
                BloodTechUIManager.Instance.TransitionToCharacterSelect(mode);
            }
            else
            {
                SceneManager.LoadScene("MainMenu");
            }
        }

        public void OnClickStageSelect()
        {
            if (_resultsSubmissionLocked) return;
            _resultsSubmissionLocked = true;
            SafePlayButtonClick();

            if (_victoryModal != null) _victoryModal.SetActive(false);
            if (MatchRoundReferee.Instance != null)
            {
                MatchRoundReferee.Instance.StopMatch();
            }
            if (DemonIdol.Network.QuantumPvPManager.Instance != null)
            {
                DemonIdol.Network.QuantumPvPManager.Instance.LeaveMatch();
            }
            PlayerPrefs.SetString("MDI_MenuInitialState", "StageSelect");
            PlayerPrefs.Save();
            SceneManager.LoadScene("MainMenu");
        }

        public void OnClickReturnToMenu()
        {
            if (_resultsSubmissionLocked) return;
            _resultsSubmissionLocked = true;
            SafePlayButtonClick();

            if (_victoryModal != null) _victoryModal.SetActive(false);
            if (MatchRoundReferee.Instance != null)
            {
                MatchRoundReferee.Instance.StopMatch();
            }
            if (DemonIdol.Network.QuantumPvPManager.Instance != null)
            {
                DemonIdol.Network.QuantumPvPManager.Instance.LeaveMatch();
            }
            PlayerPrefs.SetString("MDI_MenuInitialState", "Title");
            PlayerPrefs.Save();
            LaunchServices.Session.MarkMenu();
            if (BloodTechUIManager.Instance != null)
            {
                BloodTechUIManager.Instance.TransitionToModeSelect();
            }
            else
            {
                SceneManager.LoadScene("MainMenu");
            }
        }

        #endregion
    }
}
