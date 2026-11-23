using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using DemonIdol.Combat;
using DemonIdol.UI;

namespace DemonIdol.Combat
{
    [Serializable]
    public class LessonRecord
    {
        public string status = "pending"; // "pending", "done", "skipped", "locked"
        public int attempts = 0;
        public bool skipped = false;
    }

    [Serializable]
    public class OnboardingSaveData
    {
        public int schema = 1;
        public string lastLessonId = "move_walk";
        public bool onboardingComplete = false;
        public List<string> lessonKeys = new List<string>();
        public List<LessonRecord> lessonValues = new List<LessonRecord>();

        public LessonRecord GetRecord(string id)
        {
            int idx = lessonKeys.IndexOf(id);
            if (idx >= 0 && idx < lessonValues.Count) return lessonValues[idx];
            var rec = new LessonRecord();
            lessonKeys.Add(id);
            lessonValues.Add(rec);
            return rec;
        }

        public void SetRecord(string id, LessonRecord record)
        {
            int idx = lessonKeys.IndexOf(id);
            if (idx >= 0 && idx < lessonValues.Count)
            {
                lessonValues[idx] = record;
            }
            else
            {
                lessonKeys.Add(id);
                lessonValues.Add(record);
            }
        }
    }

    public enum LessonId
    {
        MoveWalk,
        DashBackdash,
        GuardIdle,
        CrouchLowMidHigh,
        Parry,
        ChainLMH,
        LauncherAirEnder,
        Wakeup,
        MeterSandbox
    }

    /// <summary>
    /// Master Onboarding and Combat Dojo Curriculum Director.
    /// Implements the 9 fundamental fighting lessons inspired by SF6 modern teaching:
    /// Walk -> Dash/Backdash -> Neutral Auto-Block -> High/Low/Mid Triad -> Parry Read ->
    /// Gatling Chains -> Launchers -> Ground Okizeme -> Meter & Overdrive.
    /// Features atomic persistence in demonidol/onboarding.json.
    /// </summary>
    public class OnboardingDirector : MonoBehaviour
    {
        public static OnboardingDirector Instance { get; private set; }

        private const string SAVE_FOLDER = "demonidol";
        private const string SAVE_FILE = "onboarding.json";
        private const string PREFS_BACKUP_KEY = "demonidol_onboarding_data";

        [Header("Fighter References")]
        [SerializeField] private FighterController _player;
        [SerializeField] private FighterController _dummy;
        [SerializeField] private TrainingDummyAI _dummyAi;

        [Header("UI Overlay")]
        [SerializeField] private GameObject _onboardingPanel;
        [SerializeField] private Text _lessonTitleText;
        [SerializeField] private Text _lessonPromptText;
        [SerializeField] private Text _lessonProgressText;
        [SerializeField] private Text _feedbackBannerText;
        [SerializeField] private Button _btnSkip;
        [SerializeField] private Button _btnRetry;

        public LessonId CurrentLesson { get; private set; } = LessonId.MoveWalk;
        public bool IsRunningLesson { get; private set; } = false;

        private OnboardingSaveData _saveData = new OnboardingSaveData();
        private Coroutine _activeLessonRoutine;

        // Progress trackers within active lesson
        private bool _conditionMet = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadProgress();

            if (_btnSkip != null) _btnSkip.onClick.AddListener(OnClickSkip);
            if (_btnRetry != null) _btnRetry.onClick.AddListener(OnClickRetry);
        }

        private void Start()
        {
            FindFightersIfMissing();
        }

        public void BindFighters(FighterController player, FighterController dummy)
        {
            _player = player;
            _dummy = dummy;
            if (_dummy != null)
            {
                _dummyAi = _dummy.GetComponent<TrainingDummyAI>();
            }
        }

        private void FindFightersIfMissing()
        {
            if (_player == null || _dummy == null)
            {
                var fighters = FindObjectsByType<FighterController>();
                foreach (var f in fighters)
                {
                    if (f.isPlayerControlled && _player == null) _player = f;
                    else if (_dummy == null) _dummy = f;
                }
                if (_dummy != null && _dummyAi == null)
                {
                    _dummyAi = _dummy.GetComponent<TrainingDummyAI>();
                }
            }
        }

        public void StartCurriculum()
        {
            FindFightersIfMissing();
            EnsureUIOverlay();
            if (_onboardingPanel != null) _onboardingPanel.SetActive(true);

            // Resume from last pending lesson
            LessonId startLesson = GetNextIncompleteLesson();
            StartLesson(startLesson);
        }

        private void EnsureUIOverlay()
        {
            if (_onboardingPanel != null) return;

            Canvas masterCanvas = null;
            var canvasGo = GameObject.Find("Master_BloodTech_Canvas");
            if (canvasGo != null) masterCanvas = canvasGo.GetComponent<Canvas>();
            if (masterCanvas == null) masterCanvas = FindAnyObjectByType<Canvas>();
            if (masterCanvas == null) return;

            Transform existing = masterCanvas.transform.Find("Panel_OnboardingCurriculum");
            if (existing != null)
            {
                _onboardingPanel = existing.gameObject;
                _lessonTitleText = existing.Find("Text_LessonTitle")?.GetComponent<Text>();
                _lessonPromptText = existing.Find("Text_LessonPrompt")?.GetComponent<Text>();
                _lessonProgressText = existing.Find("Text_LessonProgress")?.GetComponent<Text>();
                _btnSkip = existing.Find("Btn_Skip")?.GetComponent<Button>();
                _btnRetry = existing.Find("Btn_Retry")?.GetComponent<Button>();
                if (_btnSkip != null) _btnSkip.onClick.AddListener(OnClickSkip);
                if (_btnRetry != null) _btnRetry.onClick.AddListener(OnClickRetry);
                return;
            }

            // Create stylish Top-Center Onboarding Curriculum Card
            _onboardingPanel = new GameObject("Panel_OnboardingCurriculum");
            _onboardingPanel.transform.SetParent(masterCanvas.transform, false);

            RectTransform cardRect = _onboardingPanel.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 1f);
            cardRect.anchorMax = new Vector2(0.5f, 1f);
            cardRect.pivot = new Vector2(0.5f, 1f);
            cardRect.anchoredPosition = new Vector2(0, -28f);
            cardRect.sizeDelta = new Vector2(740f, 120f);

            var bg = _onboardingPanel.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.09f, 0.12f, 0.94f);

            // Title
            GameObject titleGo = new GameObject("Text_LessonTitle");
            titleGo.transform.SetParent(_onboardingPanel.transform, false);
            RectTransform titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0, -18f);
            titleRect.sizeDelta = new Vector2(500f, 30f);
            _lessonTitleText = titleGo.AddComponent<Text>();
            _lessonTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _lessonTitleText.fontSize = 20;
            _lessonTitleText.fontStyle = FontStyle.Bold;
            _lessonTitleText.alignment = TextAnchor.MiddleCenter;
            _lessonTitleText.color = new Color(0.92f, 0.78f, 0.38f, 1f);

            // Prompt
            GameObject promptGo = new GameObject("Text_LessonPrompt");
            promptGo.transform.SetParent(_onboardingPanel.transform, false);
            RectTransform promptRect = promptGo.AddComponent<RectTransform>();
            promptRect.anchoredPosition = new Vector2(0, -48f);
            promptRect.sizeDelta = new Vector2(680f, 26f);
            _lessonPromptText = promptGo.AddComponent<Text>();
            _lessonPromptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _lessonPromptText.fontSize = 15;
            _lessonPromptText.alignment = TextAnchor.MiddleCenter;
            _lessonPromptText.color = Color.white;

            // Progress
            GameObject progGo = new GameObject("Text_LessonProgress");
            progGo.transform.SetParent(_onboardingPanel.transform, false);
            RectTransform progRect = progGo.AddComponent<RectTransform>();
            progRect.anchoredPosition = new Vector2(0, -80f);
            progRect.sizeDelta = new Vector2(400f, 26f);
            _lessonProgressText = progGo.AddComponent<Text>();
            _lessonProgressText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _lessonProgressText.fontSize = 17;
            _lessonProgressText.fontStyle = FontStyle.Bold;
            _lessonProgressText.alignment = TextAnchor.MiddleCenter;
            _lessonProgressText.color = new Color(0.2f, 0.85f, 1f, 1f);

            // Skip Button
            GameObject skipGo = new GameObject("Btn_Skip");
            skipGo.transform.SetParent(_onboardingPanel.transform, false);
            RectTransform skipRect = skipGo.AddComponent<RectTransform>();
            skipRect.anchorMin = new Vector2(1f, 0.5f);
            skipRect.anchorMax = new Vector2(1f, 0.5f);
            skipRect.pivot = new Vector2(1f, 0.5f);
            skipRect.anchoredPosition = new Vector2(-15f, 0);
            skipRect.sizeDelta = new Vector2(85f, 40f);
            var skipImg = skipGo.AddComponent<Image>();
            skipImg.color = new Color(0.25f, 0.15f, 0.15f, 0.9f);
            _btnSkip = skipGo.AddComponent<Button>();
            _btnSkip.onClick.AddListener(OnClickSkip);

            GameObject skipTxtGo = new GameObject("Text");
            skipTxtGo.transform.SetParent(skipGo.transform, false);
            RectTransform skipTxtRect = skipTxtGo.AddComponent<RectTransform>();
            skipTxtRect.sizeDelta = skipRect.sizeDelta;
            var skipTxt = skipTxtGo.AddComponent<Text>();
            skipTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            skipTxt.text = "SKIP";
            skipTxt.fontSize = 14;
            skipTxt.alignment = TextAnchor.MiddleCenter;
            skipTxt.color = Color.white;

            // Retry Button
            GameObject retryGo = new GameObject("Btn_Retry");
            retryGo.transform.SetParent(_onboardingPanel.transform, false);
            RectTransform retryRect = retryGo.AddComponent<RectTransform>();
            retryRect.anchorMin = new Vector2(0f, 0.5f);
            retryRect.anchorMax = new Vector2(0f, 0.5f);
            retryRect.pivot = new Vector2(0f, 0.5f);
            retryRect.anchoredPosition = new Vector2(15f, 0);
            retryRect.sizeDelta = new Vector2(85f, 40f);
            var retryImg = retryGo.AddComponent<Image>();
            retryImg.color = new Color(0.18f, 0.22f, 0.28f, 0.9f);
            _btnRetry = retryGo.AddComponent<Button>();
            _btnRetry.onClick.AddListener(OnClickRetry);

            GameObject retryTxtGo = new GameObject("Text");
            retryTxtGo.transform.SetParent(retryGo.transform, false);
            RectTransform retryTxtRect = retryTxtGo.AddComponent<RectTransform>();
            retryTxtRect.sizeDelta = retryRect.sizeDelta;
            var retryTxt = retryTxtGo.AddComponent<Text>();
            retryTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            retryTxt.text = "RETRY";
            retryTxt.fontSize = 14;
            retryTxt.alignment = TextAnchor.MiddleCenter;
            retryTxt.color = Color.white;

            // Feedback Banner (Floating Centered)
            GameObject fbGo = new GameObject("Text_FeedbackBanner");
            fbGo.transform.SetParent(masterCanvas.transform, false);
            RectTransform fbRect = fbGo.AddComponent<RectTransform>();
            fbRect.anchoredPosition = new Vector2(0, 100f);
            fbRect.sizeDelta = new Vector2(800f, 60f);
            _feedbackBannerText = fbGo.AddComponent<Text>();
            _feedbackBannerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _feedbackBannerText.fontSize = 32;
            _feedbackBannerText.fontStyle = FontStyle.Bold;
            _feedbackBannerText.alignment = TextAnchor.MiddleCenter;
            _feedbackBannerText.color = Color.green;
            _feedbackBannerText.gameObject.SetActive(false);
        }

        public void StopCurriculum()
        {
            if (_activeLessonRoutine != null)
            {
                StopCoroutine(_activeLessonRoutine);
                _activeLessonRoutine = null;
            }
            IsRunningLesson = false;
            if (_onboardingPanel != null) _onboardingPanel.SetActive(false);
            if (_feedbackBannerText != null) _feedbackBannerText.gameObject.SetActive(false);
        }

        public void StartLesson(LessonId lesson)
        {
            if (_activeLessonRoutine != null) StopCoroutine(_activeLessonRoutine);
            CurrentLesson = lesson;
            _activeLessonRoutine = StartCoroutine(RunLesson(lesson));
        }

        private IEnumerator RunLesson(LessonId lesson)
        {
            IsRunningLesson = true;
            _conditionMet = false;
            string key = lesson.ToString();
            var record = _saveData.GetRecord(key);
            record.attempts++;
            SaveProgress();

            ResetArenaPositions();

            switch (lesson)
            {
                case LessonId.MoveWalk:
                    yield return Lesson_MoveWalk();
                    break;
                case LessonId.DashBackdash:
                    yield return Lesson_DashBackdash();
                    break;
                case LessonId.GuardIdle:
                    yield return Lesson_GuardIdle();
                    break;
                case LessonId.CrouchLowMidHigh:
                    yield return Lesson_CrouchLowMidHigh();
                    break;
                case LessonId.Parry:
                    yield return Lesson_Parry();
                    break;
                case LessonId.ChainLMH:
                    yield return Lesson_ChainLMH();
                    break;
                case LessonId.LauncherAirEnder:
                    yield return Lesson_LauncherAirEnder();
                    break;
                case LessonId.Wakeup:
                    yield return Lesson_Wakeup();
                    break;
                case LessonId.MeterSandbox:
                    yield return Lesson_MeterSandbox();
                    break;
            }

            // Lesson complete!
            record.status = "done";
            _saveData.lastLessonId = lesson.ToString();
            SaveProgress();

            yield return ShowFeedback("LESSON COMPLETE!", Color.green, 1.5f);

            // Advance to next lesson
            LessonId next = (LessonId)((int)lesson + 1);
            if ((int)next < Enum.GetValues(typeof(LessonId)).Length)
            {
                StartLesson(next);
            }
            else
            {
                _saveData.onboardingComplete = true;
                SaveProgress();
                UpdateUI("COMBAT MASTERY ACHIEVED", "You are now fully trained for competitive versus combat.", "100%");
                yield return new WaitForSeconds(2.0f);
            }
        }

        #region Lesson Implementations

        private IEnumerator Lesson_MoveWalk()
        {
            UpdateUI("LESSON 1: GROUND SPACING", "Hold Forward to advance, then hold Back to retreat.", "0/2 seconds");
            if (_dummyAi != null) _dummyAi.mode = DummyMode.StandStill;

            float forwardTime = 0f;
            float backTime = 0f;

            while (forwardTime < 1.0f || backTime < 1.0f)
            {
                if (_player != null && _player.State == FighterState.WalkCreep)
                {
                    if (_player.FacingDirection * _player.transform.position.x < 0) forwardTime += Time.deltaTime;
                    else backTime += Time.deltaTime;
                }
                float total = Mathf.Clamp01((forwardTime + backTime) / 2.0f);
                UpdateProgress($"Spacing: {Mathf.RoundToInt(total * 100f)}%");
                yield return null;
            }
        }

        private IEnumerator Lesson_DashBackdash()
        {
            UpdateUI("LESSON 2: BURST DASHING", "Tap Forward Dash (W/E or Dash button), then tap Backdash (Space/Q).", "Dash: 0/1 | Backdash: 0/1");
            if (_dummyAi != null) _dummyAi.mode = DummyMode.StandStill;

            bool dashed = false;
            bool backdashed = false;

            while (!dashed || !backdashed)
            {
                if (_player != null)
                {
                    if (_player.State == FighterState.ForwardDash) dashed = true;
                    if (_player.State == FighterState.Backdash) backdashed = true;
                }
                UpdateProgress($"Dash: {(dashed ? 1 : 0)}/1 | Backdash: {(backdashed ? 1 : 0)}/1");
                yield return null;
            }
        }

        private IEnumerator Lesson_GuardIdle()
        {
            UpdateUI("LESSON 3: NEUTRAL AUTO-GUARD", "Release the joystick to enter Idle auto-guard. Block 3 incoming Mid attacks standing.", "Blocks: 0/3");
            if (_dummyAi != null)
            {
                _dummyAi.mode = DummyMode.CounterHitPractice;
                _dummyAi.attackIntervalTicks = 80;
            }

            int blocks = 0;
            Action<HitResult, AttackData, Vector3> onHit = (res, atk, pt) =>
            {
                if (res == HitResult.Blocked || res == HitResult.BarrierBlocked) blocks++;
            };

            if (_player != null) _player.OnHitProcessed += onHit;
            try
            {
                while (blocks < 3)
                {
                    UpdateProgress($"Blocks: {blocks}/3");
                    yield return null;
                }
            }
            finally
            {
                if (_player != null) _player.OnHitProcessed -= onHit;
            }
        }

        private IEnumerator Lesson_CrouchLowMidHigh()
        {
            UpdateUI("LESSON 4: HIGH-LOW TRIAD", "Crouch ducks Highs. Lows hit standing — crouch to block them!", "Guards: 0/2");
            if (_dummyAi != null)
            {
                _dummyAi.mode = DummyMode.Aggressive;
            }

            int successes = 0;
            while (successes < 2)
            {
                if (_player != null && _player.State == FighterState.Blockstun)
                {
                    successes++;
                    yield return new WaitForSeconds(0.5f);
                }
                UpdateProgress($"Crouch Guards: {successes}/2");
                yield return null;
            }
        }

        private IEnumerator Lesson_Parry()
        {
            UpdateUI("LESSON 5: PARRY TIMING", "Press Parry (F) just before impact. Active window is 5 ticks (1/12s).", "Parries: 0/2");
            if (_dummyAi != null)
            {
                _dummyAi.mode = DummyMode.CounterHitPractice;
                _dummyAi.attackIntervalTicks = 90;
            }

            int parries = 0;
            while (parries < 2)
            {
                if (_player != null && _player.State == FighterState.ParrySuccess)
                {
                    parries++;
                    yield return ShowFeedback("PERFECT PARRY!", Color.yellow, 0.8f);
                }
                UpdateProgress($"Parries: {parries}/2");
                yield return null;
            }
        }

        private IEnumerator Lesson_ChainLMH()
        {
            UpdateUI("LESSON 6: GATLING MAGIC SERIES", "Perform a 3-hit combo on the dummy: Light -> Medium -> Heavy.", "Hit String: 0/3");
            if (_dummyAi != null) _dummyAi.mode = DummyMode.StandStill;

            while (!_conditionMet)
            {
                int hits = BattleHUDController.Instance != null ? BattleHUDController.Instance.CurrentComboHits : 0;
                if (hits >= 3)
                {
                    _conditionMet = true;
                }
                UpdateProgress($"Combo Hits: {hits}/3");
                yield return null;
            }
        }

        private IEnumerator Lesson_LauncherAirEnder()
        {
            UpdateUI("LESSON 7: LAUNCHER & AERIAL ENDER", "Hit with Heavy (launcher) to send rival airborne, then cancel into Hopkick!", "Launcher Ender: 0/1");
            if (_dummyAi != null) _dummyAi.mode = DummyMode.StandStill;

            while (!_conditionMet)
            {
                if (_dummy != null && _dummy.State == FighterState.AirborneJuggle)
                {
                    if (_player != null && _player.CurrentAttack != null && _player.CurrentAttack.attackType == AttackType.JumpAttack)
                    {
                        _conditionMet = true;
                    }
                }
                yield return null;
            }
        }

        private IEnumerator Lesson_Wakeup()
        {
            UpdateUI("LESSON 8: GROUND OKIZEME", "When knocked down, tap Sidestep (R/V) to tech-roll, or press Light for a wakeup kick.", "Wakeup Tech: 0/1");
            if (_dummyAi != null) _dummyAi.mode = DummyMode.StandStill;

            // Knock player down
            if (_player != null)
            {
                _player.EnterKnockdown();
            }

            while (!_conditionMet)
            {
                if (_player != null && _player.State == FighterState.Wakeup)
                {
                    _conditionMet = true;
                }
                yield return null;
            }
        }

        private IEnumerator Lesson_MeterSandbox()
        {
            UpdateUI("LESSON 9: METER & DOMAIN EXPANSION", "Build 100% Super Meter and unleash your Domain Expansion finisher!", "Domain: 0/1");
            if (_dummyAi != null) _dummyAi.mode = DummyMode.StandStill;

            if (_player != null)
            {
                _player.AddSuperMeter(100f);
            }

            while (!_conditionMet)
            {
                if (CombatTickEngine.Instance != null && CombatTickEngine.Instance.IsSuperfrozen)
                {
                    _conditionMet = true;
                }
                yield return null;
            }
        }

        #endregion

        #region Helpers & UI

        private void ResetArenaPositions()
        {
            if (_player != null)
            {
                var cc = _player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                _player.transform.position = new Vector3(-2.5f, 0, 0);
                if (cc != null) cc.enabled = true;
                _player.SetFacing(1);
                _player.ResetToIdle();
                if (_player.brain != null) _player.brain.ResetForRound();
            }
            if (_dummy != null)
            {
                var cc = _dummy.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                _dummy.transform.position = new Vector3(2.5f, 0, 0);
                if (cc != null) cc.enabled = true;
                _dummy.SetFacing(-1);
                _dummy.ResetToIdle();
                if (_dummy.brain != null) _dummy.brain.ResetForRound();
            }
        }

        private void UpdateUI(string title, string prompt, string progress)
        {
            if (_lessonTitleText != null) _lessonTitleText.text = title;
            if (_lessonPromptText != null) _lessonPromptText.text = prompt;
            if (_lessonProgressText != null) _lessonProgressText.text = progress;
            if (_feedbackBannerText != null) _feedbackBannerText.gameObject.SetActive(false);
        }

        private void UpdateProgress(string progress)
        {
            if (_lessonProgressText != null) _lessonProgressText.text = progress;
        }

        private IEnumerator ShowFeedback(string text, Color col, float duration)
        {
            if (_feedbackBannerText != null)
            {
                _feedbackBannerText.text = text;
                _feedbackBannerText.color = col;
                _feedbackBannerText.gameObject.SetActive(true);
            }
            yield return new WaitForSeconds(duration);
            if (_feedbackBannerText != null) _feedbackBannerText.gameObject.SetActive(false);
        }

        public void OnClickSkip()
        {
            string key = CurrentLesson.ToString();
            var record = _saveData.GetRecord(key);
            record.status = "skipped";
            record.skipped = true;
            SaveProgress();

            LessonId next = (LessonId)((int)CurrentLesson + 1);
            if ((int)next < Enum.GetValues(typeof(LessonId)).Length)
            {
                StartLesson(next);
            }
        }

        public void OnClickRetry()
        {
            StartLesson(CurrentLesson);
        }

        private LessonId GetNextIncompleteLesson()
        {
            foreach (LessonId id in Enum.GetValues(typeof(LessonId)))
            {
                var rec = _saveData.GetRecord(id.ToString());
                if (rec.status != "done" && rec.status != "skipped")
                {
                    return id;
                }
            }
            return LessonId.MoveWalk;
        }

        private void SaveProgress()
        {
            try
            {
                string json = JsonUtility.ToJson(_saveData, true);
                string dir = Path.Combine(Application.persistentDataPath, SAVE_FOLDER);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string filePath = Path.Combine(dir, SAVE_FILE);
                string tempPath = filePath + ".tmp";

                File.WriteAllText(tempPath, json);
                if (File.Exists(filePath)) File.Delete(filePath);
                File.Move(tempPath, filePath);

                PlayerPrefs.SetString(PREFS_BACKUP_KEY, json);
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OnboardingDirector] Save failed: {ex.Message}");
            }
        }

        private void LoadProgress()
        {
            try
            {
                string filePath = Path.Combine(Application.persistentDataPath, SAVE_FOLDER, SAVE_FILE);
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    _saveData = JsonUtility.FromJson<OnboardingSaveData>(json) ?? new OnboardingSaveData();
                    return;
                }
            }
            catch
            {
                // Fallback to PlayerPrefs backup
            }

            string prefJson = PlayerPrefs.GetString(PREFS_BACKUP_KEY, string.Empty);
            if (!string.IsNullOrEmpty(prefJson))
            {
                _saveData = JsonUtility.FromJson<OnboardingSaveData>(prefJson) ?? new OnboardingSaveData();
            }
            else
            {
                _saveData = new OnboardingSaveData();
            }
        }

        #endregion
    }
}
