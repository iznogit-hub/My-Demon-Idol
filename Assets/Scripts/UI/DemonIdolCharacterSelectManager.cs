using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using DemonIdol.Combat;
using DemonIdol.Launch;
using DemonIdol.Audio;
using DemonIdol.BlazBlue;

namespace DemonIdol.UI
{
    public enum RosterCategory
    {
        All = 0,
        MainCast = 1,
        ShadowKnights = 2,
        Expansion = 3
    }

    /// <summary>
    /// Master Character Selection Screen UI Controller for Assets/Scenes/CharacterSelect.unity (Modern AAA Overhaul).
    /// Features:
    /// - Dual Live 3D Face-Off Pedestals (P1 Left vs P2/CPU Right) with pooled preview models and lock-in taunts.
    /// - Tactical Fighter Info Wings (Left = P1, Right = P2: Name, Title, Archetype Badge, Drive Name, Reach/Speed/Power Bars).
    /// - Categorized 25-Fighter Bottom Dock (Main Cast C01-C14, Shadow Knights SK02-SK08, Expansion EX01-EX07, Random Tile).
    /// - Distinct High-Contrast P1 (Vermilion) & P2 (IndigoVoid/ParryGold) Cursor Frames with Mirror Match (C01 vs C01) support.
    /// - Inline Match Options Strip (Stage ST07, Rounds 1/2/3, CPU Difficulty) without extra scene hops.
    /// - Dramatic Sumi-e Ink-Wipe VS Transition into BattleArena.unity.
    /// - 100% Controller + Keyboard + Mouse/Touch Parity with zero-GC Update loop.
    /// </summary>
    public class DemonIdolCharacterSelectManager : MonoBehaviour
    {
        public static DemonIdolCharacterSelectManager Instance { get; private set; }

        public static readonly string[] RosterIds = new string[]
        {
            "C01", "C02", "C04", "C05", "C06",
            "C07", "C08", "C09", "C10", "C11",
            "C12", "C13", "C14", "SK02", "SK03",
            "SK05", "SK06", "SK08", "EX01", "EX02",
            "EX03", "EX04", "EX05", "EX06", "EX07"
        };

        public static readonly string[] LaunchRosterIds = new string[]
        {
            "C01", "C02", "C05", "C06", "C08", "C09",
            "C11", "C12", "C14", "EX01", "EX04", "SK08"
        };

        public static readonly string[] StageIds = new string[]
        {
            "ST07", "ST01", "ST02", "ST03", "ST04"
        };

        public static readonly string[] StageNames = new string[]
        {
            "COLLEGE RAIMON (MIDNIGHT RAIN)",
            "CENTRAL COURTYARD (SHRINE)",
            "HEAVY CYBER HANGAR",
            "NEO SHIBUYA COLOSSEUM",
            "INK FORTRESS CATHEDRAL"
        };

        public static readonly string[] DifficultyNames = new string[]
        {
            "EASY", "NORMAL", "HARD", "EXPERT", "DEMON LORD"
        };

        [Header("Selection State")]
        public int p1Index = 0; // Default C01
        public int p2Index = 18; // Default EX01 (Hugo)
        public bool p1Locked = false;
        public bool p2Locked = false;
        public bool isVersus = false;

        [Header("Legacy Compatible UI Grid & Cards")]
        public RectTransform gridRoot;
        public Button[] characterButtons;
        public Image[] p1Borders;
        public Image[] p2Borders;
        public Text[] characterLabels;

        [Header("Legacy Compatible Dossier UI")]
        public Text p1StatusText;
        public Text p2StatusText;
        public Text dossierName;
        public Text dossierArchetype;
        public Text dossierBio;

        [Header("Dual 3D Preview Pedestals")]
        public Transform previewPedestal; // P1 or legacy single pedestal
        public Transform p1Pedestal;
        public Transform p2Pedestal;
        public Camera previewCamera;
        private GameObject _activePreviewModel;
        private GameObject _activeP1Model;
        private GameObject _activeP2Model;
        private string _activeP1Id = "";
        private string _activeP2Id = "";

        // Model Pool (Zero GC on hover)
        private readonly Dictionary<string, GameObject> _p1ModelPool = new Dictionary<string, GameObject>(StringComparer.Ordinal);
        private readonly Dictionary<string, GameObject> _p2ModelPool = new Dictionary<string, GameObject>(StringComparer.Ordinal);

        [Header("Tactical Fighter Info Wings (Phase 2)")]
        // P1 Wing (Left)
        public Text p1NameWing;
        public Text p1TitleWing;
        public Text p1ArchetypeBadge;
        public Text p1DriveWing;
        public Slider p1ReachBar;
        public Slider p1SpeedBar;
        public Slider p1PowerBar;

        // P2 Wing (Right)
        public Text p2NameWing;
        public Text p2TitleWing;
        public Text p2ArchetypeBadge;
        public Text p2DriveWing;
        public Slider p2ReachBar;
        public Slider p2SpeedBar;
        public Slider p2PowerBar;

        [Header("Categorized Bottom Dock (Phase 2)")]
        public Button tabCatAll;
        public Button tabCatMain;
        public Button tabCatKnights;
        public Button tabCatExpansion;
        public Button btnRandomPick;
        public RosterCategory currentCategory = RosterCategory.All;

        [Header("Inline Match Options Strip (Phase 2)")]
        public GameObject matchOptionsStrip;
        public Button btnStagePrev;
        public Button btnStageNext;
        public Text txtStageSelected;
        public Button btnRoundsPrev;
        public Button btnRoundsNext;
        public Text txtRoundsSelected;
        public Button btnDifficultyPrev;
        public Button btnDifficultyNext;
        public Text txtDifficultySelected;
        public Button btnStartFight;
        public int selectedStageIndex = 0; // Default ST07
        public int roundsToWin = 2; // Default best of 3
        public int cpuDifficultyIndex = 1; // Default Normal

        [Header("AMV Lock-In Presentation (Phase 4)")]
        public Image lockInFlashOverlay;
        public Text p1LockInSlamText;
        public Text p2LockInSlamText;

        [Header("VS Overlay & Dramatic Transition")]
        public GameObject vsOverlay;
        public Image vsP1Portrait;
        public Image vsP2Portrait;
        public Text vsP1Name;
        public Text vsP2Name;
        public Text vsStageName;

        [Header("Footer Prompts")]
        public Text footerPromptsText;

        private float _lastP1InputTime = 0f;
        private float _lastP2InputTime = 0f;
        private const float InputRepeatDelay = 0.16f;
        private bool _isTransitioning = false;

        private void Awake()
        {
            Instance = this;
            string mode = PlayerPrefs.GetString("MDI_GameMode", "Arcade");
            isVersus = string.Equals(mode, "LocalVersus", StringComparison.OrdinalIgnoreCase);

            EnsureDualPedestals();
            EnsureOptionsDefaults();
        }

        private void Start()
        {
            // Restore previous picks if valid
            string savedP1 = PlayerPrefs.GetString("MDI_P1_CharacterId", "C01");
            string savedP2 = PlayerPrefs.GetString("MDI_P2_CharacterId", "EX01");
            int idx1 = Array.IndexOf(RosterIds, savedP1);
            int idx2 = Array.IndexOf(RosterIds, savedP2);
            if (idx1 >= 0) p1Index = idx1;
            if (idx2 >= 0) p2Index = idx2;

            if (vsOverlay != null) vsOverlay.SetActive(false);
            if (matchOptionsStrip != null) matchOptionsStrip.SetActive(false);

            SetupCategoryButtons();
            SetupMatchOptionsButtons();
            UpdateHighlights();
            UpdateDualPreviews();
            UpdateWings();
            UpdateDossier(RosterIds[p1Index]);
            UpdateFooterPrompts();
        }

        private void Update()
        {
            if (_isTransitioning) return;

            float now = Time.unscaledTime;

            // =========================================================================
            // P1 NAVIGATION (WASD / DPad / Left Stick)
            // =========================================================================
            if (!p1Locked && (now - _lastP1InputTime >= InputRepeatDelay))
            {
                int newP1 = p1Index;
                bool moved = false;

#if ENABLE_INPUT_SYSTEM
                var pad = Gamepad.current;
                if (pad != null)
                {
                    if (pad.dpad.up.wasPressedThisFrame || pad.leftStick.up.wasPressedThisFrame) { newP1 = StepGrid(newP1, 0, -1); moved = true; }
                    else if (pad.dpad.down.wasPressedThisFrame || pad.leftStick.down.wasPressedThisFrame) { newP1 = StepGrid(newP1, 0, 1); moved = true; }
                    else if (pad.dpad.left.wasPressedThisFrame || pad.leftStick.left.wasPressedThisFrame) { newP1 = StepGrid(newP1, -1, 0); moved = true; }
                    else if (pad.dpad.right.wasPressedThisFrame || pad.leftStick.right.wasPressedThisFrame) { newP1 = StepGrid(newP1, 1, 0); moved = true; }
                }

                var kb = Keyboard.current;
                if (kb != null)
                {
                    if (kb.wKey.wasPressedThisFrame) { newP1 = StepGrid(newP1, 0, -1); moved = true; }
                    else if (kb.sKey.wasPressedThisFrame) { newP1 = StepGrid(newP1, 0, 1); moved = true; }
                    else if (kb.aKey.wasPressedThisFrame) { newP1 = StepGrid(newP1, -1, 0); moved = true; }
                    else if (kb.dKey.wasPressedThisFrame) { newP1 = StepGrid(newP1, 1, 0); moved = true; }
                }
#endif
                if (!moved)
                {
                    if (UnityEngine.Input.GetKeyDown(KeyCode.W) || UnityEngine.Input.GetAxisRaw("Vertical") > 0.5f) { newP1 = StepGrid(newP1, 0, -1); moved = true; }
                    else if (UnityEngine.Input.GetKeyDown(KeyCode.S) || UnityEngine.Input.GetAxisRaw("Vertical") < -0.5f) { newP1 = StepGrid(newP1, 0, 1); moved = true; }
                    else if (UnityEngine.Input.GetKeyDown(KeyCode.A) || UnityEngine.Input.GetAxisRaw("Horizontal") < -0.5f) { newP1 = StepGrid(newP1, -1, 0); moved = true; }
                    else if (UnityEngine.Input.GetKeyDown(KeyCode.D) || UnityEngine.Input.GetAxisRaw("Horizontal") > 0.5f) { newP1 = StepGrid(newP1, 1, 0); moved = true; }
                }

                if (moved && newP1 != p1Index)
                {
                    p1Index = newP1;
                    _lastP1InputTime = now;
                    UpdateHighlights();
                    UpdateDualPreviews();
                    UpdateWings();
                    UpdateDossier(RosterIds[p1Index]);
                    CombatAudioDirector.Instance?.PlayButtonHover();
                }
            }

            // P1 Confirm (J / Space / Enter / KeypadEnter / South button)
            if (!p1Locked)
            {
                bool p1Confirm = false;
#if ENABLE_INPUT_SYSTEM
                if (Gamepad.current != null && Gamepad.current.aButton.wasPressedThisFrame) p1Confirm = true;
                if (Keyboard.current != null && (Keyboard.current.jKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame)) p1Confirm = true;
#endif
                if (UnityEngine.Input.GetKeyDown(KeyCode.J) || UnityEngine.Input.GetKeyDown(KeyCode.Space) || UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton0)) p1Confirm = true;

                if (p1Confirm)
                {
                    LockInFighter(isP1: true);
                }
            }

            // P1 Back (K / Escape / Backspace / East button)
            bool p1Back = false;
#if ENABLE_INPUT_SYSTEM
            if (Gamepad.current != null && Gamepad.current.bButton.wasPressedThisFrame) p1Back = true;
            if (Keyboard.current != null && (Keyboard.current.kKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.backspaceKey.wasPressedThisFrame)) p1Back = true;
#endif
            if (UnityEngine.Input.GetKeyDown(KeyCode.K) || UnityEngine.Input.GetKeyDown(KeyCode.Escape) || UnityEngine.Input.GetKeyDown(KeyCode.Backspace) || UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton1)) p1Back = true;

            if (p1Back)
            {
                if (p1Locked)
                {
                    p1Locked = false;
                    UpdateHighlights();
                    UpdateFooterPrompts();
                    CombatAudioDirector.Instance?.PlayButtonHover();
                    if (matchOptionsStrip != null) matchOptionsStrip.SetActive(false);
                }
                else
                {
                    SceneManager.LoadScene("MainMenu");
                    return;
                }
            }

            // =========================================================================
            // P2 NAVIGATION (Versus: Arrow Keys / Numpad / Gamepad 2)
            // =========================================================================
            if (isVersus && !p2Locked && (now - _lastP2InputTime >= InputRepeatDelay))
            {
                int newP2 = p2Index;
                bool movedP2 = false;

#if ENABLE_INPUT_SYSTEM
                var kb = Keyboard.current;
                if (kb != null)
                {
                    if (kb.upArrowKey.wasPressedThisFrame) { newP2 = StepGrid(newP2, 0, -1); movedP2 = true; }
                    else if (kb.downArrowKey.wasPressedThisFrame) { newP2 = StepGrid(newP2, 0, 1); movedP2 = true; }
                    else if (kb.leftArrowKey.wasPressedThisFrame) { newP2 = StepGrid(newP2, -1, 0); movedP2 = true; }
                    else if (kb.rightArrowKey.wasPressedThisFrame) { newP2 = StepGrid(newP2, 1, 0); movedP2 = true; }
                }
#endif
                if (!movedP2)
                {
                    if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow)) { newP2 = StepGrid(newP2, 0, -1); movedP2 = true; }
                    else if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow)) { newP2 = StepGrid(newP2, 0, 1); movedP2 = true; }
                    else if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow)) { newP2 = StepGrid(newP2, -1, 0); movedP2 = true; }
                    else if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow)) { newP2 = StepGrid(newP2, 1, 0); movedP2 = true; }
                }

                if (movedP2 && newP2 != p2Index)
                {
                    p2Index = newP2;
                    _lastP2InputTime = now;
                    UpdateHighlights();
                    UpdateDualPreviews();
                    UpdateWings();
                    CombatAudioDirector.Instance?.PlayButtonHover();
                }
            }

            // P2 Confirm in Versus (Num0 / Keypad 0 / Keypad Enter / Right Control)
            if (isVersus && !p2Locked)
            {
                bool p2Confirm = false;
#if ENABLE_INPUT_SYSTEM
                if (Keyboard.current != null && (Keyboard.current.numpad0Key.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)) p2Confirm = true;
#endif
                if (UnityEngine.Input.GetKeyDown(KeyCode.Keypad0) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter) || UnityEngine.Input.GetKeyDown(KeyCode.RightControl)) p2Confirm = true;

                if (p2Confirm)
                {
                    LockInFighter(isP1: false);
                }
            }

            // Options Strip Navigation once both are locked
            if (p1Locked && (p2Locked || !isVersus))
            {
                if (matchOptionsStrip != null && !matchOptionsStrip.activeSelf)
                {
                    matchOptionsStrip.SetActive(true);
                    if (btnStartFight != null && EventSystem.current != null)
                    {
                        EventSystem.current.SetSelectedGameObject(btnStartFight.gameObject);
                    }
                }
            }
        }

        private int StepGrid(int currentIndex, int dx, int dy)
        {
            int row = currentIndex / 5;
            int col = currentIndex % 5;

            col += dx;
            row += dy;

            if (col < 0) col = 4;
            else if (col > 4) col = 0;

            if (row < 0) row = 4;
            else if (row > 4) row = 0;

            int target = row * 5 + col;
            return Mathf.Clamp(target, 0, RosterIds.Length - 1);
        }

        public void LockInFighter(bool isP1)
        {
            if (isP1)
            {
                p1Locked = true;
                PlayLockInFeedback(_activeP1Model, isP1: true);
            }
            else
            {
                p2Locked = true;
                PlayLockInFeedback(_activeP2Model, isP1: false);
            }

            UpdateHighlights();
            UpdateWings();
            UpdateFooterPrompts();

            if (p1Locked && (p2Locked || !isVersus))
            {
                // Both locked in: display options strip or launch match
                if (matchOptionsStrip != null)
                {
                    matchOptionsStrip.SetActive(true);
                }
                else
                {
                    StartCoroutine(LaunchMatchSequence());
                }
            }
        }

        private void PlayLockInFeedback(GameObject model, bool isP1)
        {
            CombatAudioDirector.Instance?.PlayButtonClick();
            CombatAudioDirector.Instance?.PlayVoiceDomainSuper();

            if (model != null)
            {
                Animator anim = model.GetComponentInChildren<Animator>();
                if (anim != null)
                {
                    anim.SetTrigger("Taunt");
                }
            }

            // AMV Lock-in Choreography: 0.15s color-field flash + giant condensed name slam
            StartCoroutine(AnimateLockInFeedback(isP1));
        }

        private IEnumerator AnimateLockInFeedback(bool isP1)
        {
            string charId = isP1 ? RosterIds[p1Index] : RosterIds[p2Index];
            var def = CharacterRegistry.GetCharacterById(charId);
            string charName = def != null ? def.displayName.ToUpperInvariant() : charId;

            Text slamText = isP1 ? p1LockInSlamText : p2LockInSlamText;
            if (slamText != null)
            {
                slamText.text = charName;
                slamText.gameObject.SetActive(true);
            }

            // Camera chromatic pulse
            var rig = previewCamera != null ? previewCamera.GetComponent<DemonIdol.Cinematics.MenuFighterCameraRig>() : null;
            if (rig != null)
            {
                rig.PulseChromaticAberration(0.35f, 0.15f);
            }

            // 0.15s color field flash (KineticYellow for P1, KineticRed for P2)
            if (lockInFlashOverlay != null)
            {
                lockInFlashOverlay.gameObject.SetActive(true);
                lockInFlashOverlay.color = isP1 ? SumiStyleTokens.KineticYellow : SumiStyleTokens.KineticRed;
                float duration = SettingsPanel.ReducedMotion ? 0.05f : 0.15f;
                yield return new WaitForSecondsRealtime(duration);
                lockInFlashOverlay.color = Color.clear;
                lockInFlashOverlay.gameObject.SetActive(false);
            }
            else
            {
                yield return new WaitForSecondsRealtime(0.15f);
            }
        }

        // =========================================================================
        // DUAL LIVE 3D PREVIEWS (PHASE 2)
        // =========================================================================
        public void UpdateDualPreviews()
        {
            string p1Id = RosterIds[p1Index];
            string p2Id = RosterIds[p2Index];

            UpdatePedestalModel(p1Id, isP1: true);
            UpdatePedestalModel(p2Id, isP1: false);
        }

        private void UpdatePedestalModel(string charId, bool isP1)
        {
            ref string activeId = ref isP1 ? ref _activeP1Id : ref _activeP2Id;
            ref GameObject activeModel = ref isP1 ? ref _activeP1Model : ref _activeP2Model;
            var pool = isP1 ? _p1ModelPool : _p2ModelPool;
            Transform pedestal = isP1 ? (p1Pedestal != null ? p1Pedestal : previewPedestal) : (p2Pedestal != null ? p2Pedestal : previewPedestal);

            if (activeId == charId && activeModel != null) return;
            activeId = charId;

            // Hide previous active model
            if (activeModel != null)
            {
                activeModel.SetActive(false);
            }

            // Retrieve from pool or instantiate
            if (!pool.TryGetValue(charId, out GameObject model) || model == null)
            {
                var def = CharacterRegistry.GetCharacterById(charId);
                if (def == null || def.modelPrefab == null) return;

                Transform parent = pedestal != null ? pedestal : transform;
                model = Instantiate(def.modelPrefab, parent.position, parent.rotation, parent);
                model.name = $"Preview_{(isP1 ? "P1" : "P2")}_{charId}";

                Animator anim = model.GetComponentInChildren<Animator>();
                if (anim != null)
                {
                    anim.applyRootMotion = false;
                    if (def.animatorController != null)
                    {
                        anim.runtimeAnimatorController = def.animatorController;
                    }
                }
                pool[charId] = model;
            }

            model.SetActive(true);
            activeModel = model;

            // Dynamically lock Camera Rig focus distance and eye-level framing to active fighter
            if (isP1 && previewCamera != null && activeModel != null)
            {
                var rig = previewCamera.GetComponent<DemonIdol.Cinematics.MenuFighterCameraRig>();
                if (rig != null)
                {
                    rig.SetTargetFighter(activeModel, charId);
                }
            }

            // Mirror compatibility
            if (isP1) _activePreviewModel = activeModel;
        }

        // Backward compatibility
        public void UpdatePreviewModel(string charId)
        {
            UpdatePedestalModel(charId, isP1: true);
        }

        // =========================================================================
        // TACTICAL FIGHTER INFO WINGS (PHASE 2)
        // =========================================================================
        public void UpdateWings()
        {
            // P1 Wing
            string p1Id = RosterIds[p1Index];
            var def1 = CharacterRegistry.GetCharacterById(p1Id);
            var info1 = BBRosterArchetype.Get(p1Id);

            if (p1NameWing != null) p1NameWing.text = def1 != null ? def1.displayName.ToUpper() : p1Id;
            if (p1TitleWing != null) p1TitleWing.text = def1 != null ? def1.title.ToUpper() : "TACTICAL SORCERER";
            if (p1ArchetypeBadge != null)
            {
                p1ArchetypeBadge.text = $"[ {info1.archetypeClass.ToString().ToUpper()} ]";
                p1ArchetypeBadge.color = GetArchetypeColor(info1.archetypeClass);
            }
            if (p1DriveWing != null) p1DriveWing.text = $"DRIVE: {info1.driveName.ToUpper()}";

            GetFighterStats(p1Id, out float r1, out float s1, out float p1);
            if (p1ReachBar != null) p1ReachBar.value = r1;
            if (p1SpeedBar != null) p1SpeedBar.value = s1;
            if (p1PowerBar != null) p1PowerBar.value = p1;

            // P2 Wing
            string p2Id = RosterIds[p2Index];
            var def2 = CharacterRegistry.GetCharacterById(p2Id);
            var info2 = BBRosterArchetype.Get(p2Id);

            if (p2NameWing != null) p2NameWing.text = def2 != null ? def2.displayName.ToUpper() : p2Id;
            if (p2TitleWing != null) p2TitleWing.text = def2 != null ? def2.title.ToUpper() : (isVersus ? "OPPONENT" : "CPU CHALLENGER");
            if (p2ArchetypeBadge != null)
            {
                p2ArchetypeBadge.text = $"[ {info2.archetypeClass.ToString().ToUpper()} ]";
                p2ArchetypeBadge.color = GetArchetypeColor(info2.archetypeClass);
            }
            if (p2DriveWing != null) p2DriveWing.text = $"DRIVE: {info2.driveName.ToUpper()}";

            GetFighterStats(p2Id, out float r2, out float s2, out float p2);
            if (p2ReachBar != null) p2ReachBar.value = r2;
            if (p2SpeedBar != null) p2SpeedBar.value = s2;
            if (p2PowerBar != null) p2PowerBar.value = p2;
        }

        public static void GetFighterStats(string charId, out float reach, out float speed, out float power)
        {
            var info = BBRosterArchetype.Get(charId);
            reach = Mathf.Clamp01((info.weaponReach - 0.45f) / 0.70f);
            switch (info.archetypeClass)
            {
                case BBArchetypeClass.Rushdown:
                    speed = 0.95f; power = 0.65f; break;
                case BBArchetypeClass.Zoner:
                    speed = 0.60f; power = 0.70f; break;
                case BBArchetypeClass.Grappler:
                    speed = 0.40f; power = 0.95f; break;
                case BBArchetypeClass.Swordsman:
                    speed = 0.75f; power = 0.85f; break;
                case BBArchetypeClass.AllRounder:
                default:
                    speed = 0.75f; power = 0.75f; break;
            }
        }

        public static Color GetArchetypeColor(BBArchetypeClass arch)
        {
            switch (arch)
            {
                case BBArchetypeClass.Rushdown: return SumiStyleTokens.Vermilion;
                case BBArchetypeClass.Zoner: return new Color(0.2f, 0.75f, 1f, 1f);
                case BBArchetypeClass.Grappler: return SumiStyleTokens.ToriiRed;
                case BBArchetypeClass.Swordsman: return SumiStyleTokens.ParryGold;
                case BBArchetypeClass.AllRounder:
                default: return SumiStyleTokens.PaperWhite;
            }
        }

        // =========================================================================
        // CATEGORIZED BOTTOM DOCK & HIGHLIGHTS
        // =========================================================================
        public void UpdateHighlights()
        {
            bool isMirrorMatch = (p1Index == p2Index);

            for (int i = 0; i < RosterIds.Length; i++)
            {
                bool isP1 = (i == p1Index);
                bool isP2 = (i == p2Index);

                if (p1Borders != null && i < p1Borders.Length && p1Borders[i] != null)
                {
                    p1Borders[i].gameObject.SetActive(isP1);
                    p1Borders[i].color = p1Locked ? SumiStyleTokens.Vermilion : new Color(1f, 0.45f, 0.35f, 0.95f);
                }

                if (p2Borders != null && i < p2Borders.Length && p2Borders[i] != null)
                {
                    p2Borders[i].gameObject.SetActive(isP2);
                    // Mirror match handling: high contrast ParryGold inner frame if mirror
                    Color p2Col = isMirrorMatch ? SumiStyleTokens.ParryGold : (p2Locked ? SumiStyleTokens.IndigoVoid : new Color(0.45f, 0.65f, 1f, 0.95f));
                    p2Borders[i].color = p2Col;
                }
            }

            string p1Id = RosterIds[p1Index];
            string p2Id = RosterIds[p2Index];
            var def1 = CharacterRegistry.GetCharacterById(p1Id);
            var def2 = CharacterRegistry.GetCharacterById(p2Id);

            if (p1StatusText != null)
            {
                p1StatusText.text = $"P1: {(def1 != null ? def1.displayName : p1Id)} {(p1Locked ? "[LOCKED]" : "[CHOOSING]")}";
            }

            if (p2StatusText != null)
            {
                string tag = isVersus ? (p2Locked ? "[LOCKED]" : "[CHOOSING]") : "[CPU]";
                p2StatusText.text = $"P2: {(def2 != null ? def2.displayName : p2Id)} {tag}";
            }
        }

        private void SetupCategoryButtons()
        {
            if (tabCatAll != null) tabCatAll.onClick.AddListener(() => FilterCategory(RosterCategory.All));
            if (tabCatMain != null) tabCatMain.onClick.AddListener(() => FilterCategory(RosterCategory.MainCast));
            if (tabCatKnights != null) tabCatKnights.onClick.AddListener(() => FilterCategory(RosterCategory.ShadowKnights));
            if (tabCatExpansion != null) tabCatExpansion.onClick.AddListener(() => FilterCategory(RosterCategory.Expansion));
            if (btnRandomPick != null) btnRandomPick.onClick.AddListener(PickRandomFighter);
        }

        public void FilterCategory(RosterCategory cat)
        {
            currentCategory = cat;
            CombatAudioDirector.Instance?.PlayButtonHover();

            for (int i = 0; i < RosterIds.Length; i++)
            {
                if (characterButtons != null && i < characterButtons.Length && characterButtons[i] != null)
                {
                    bool match = MatchesCategory(RosterIds[i], cat);
                    characterButtons[i].gameObject.SetActive(match);
                }
            }
        }

        private static bool MatchesCategory(string id, RosterCategory cat)
        {
            switch (cat)
            {
                case RosterCategory.MainCast: return id.StartsWith("C");
                case RosterCategory.ShadowKnights: return id.StartsWith("SK");
                case RosterCategory.Expansion: return id.StartsWith("EX");
                case RosterCategory.All:
                default: return true;
            }
        }

        public void PickRandomFighter()
        {
            CombatAudioDirector.Instance?.PlayButtonClick();
            int rand = UnityEngine.Random.Range(0, RosterIds.Length);
            if (!p1Locked)
            {
                p1Index = rand;
                LockInFighter(isP1: true);
            }
            else if (isVersus && !p2Locked)
            {
                p2Index = rand;
                LockInFighter(isP1: false);
            }
            UpdateHighlights();
            UpdateDualPreviews();
            UpdateWings();
        }

        // =========================================================================
        // INLINE MATCH OPTIONS STRIP (PHASE 2)
        // =========================================================================
        private void EnsureOptionsDefaults()
        {
            selectedStageIndex = 0; // ST07
            roundsToWin = PlayerPrefs.GetInt("MDI_RoundsToWin", 2);
            cpuDifficultyIndex = PlayerPrefs.GetInt("MDI_CpuDifficulty", 1);
            UpdateOptionsLabels();
        }

        private void SetupMatchOptionsButtons()
        {
            if (btnStagePrev != null) btnStagePrev.onClick.AddListener(() => CycleStage(-1));
            if (btnStageNext != null) btnStageNext.onClick.AddListener(() => CycleStage(1));

            if (btnRoundsPrev != null) btnRoundsPrev.onClick.AddListener(() => CycleRounds(-1));
            if (btnRoundsNext != null) btnRoundsNext.onClick.AddListener(() => CycleRounds(1));

            if (btnDifficultyPrev != null) btnDifficultyPrev.onClick.AddListener(() => CycleDifficulty(-1));
            if (btnDifficultyNext != null) btnDifficultyNext.onClick.AddListener(() => CycleDifficulty(1));

            if (btnStartFight != null) btnStartFight.onClick.AddListener(() =>
            {
                StartCoroutine(LaunchMatchSequence());
            });
        }

        private void CycleStage(int dir)
        {
            selectedStageIndex = (selectedStageIndex + dir + StageIds.Length) % StageIds.Length;
            UpdateOptionsLabels();
            CombatAudioDirector.Instance?.PlayButtonHover();
        }

        private void CycleRounds(int dir)
        {
            roundsToWin = Mathf.Clamp(roundsToWin + dir, 1, 3);
            PlayerPrefs.SetInt("MDI_RoundsToWin", roundsToWin);
            UpdateOptionsLabels();
            CombatAudioDirector.Instance?.PlayButtonHover();
        }

        private void CycleDifficulty(int dir)
        {
            cpuDifficultyIndex = (cpuDifficultyIndex + dir + DifficultyNames.Length) % DifficultyNames.Length;
            PlayerPrefs.SetInt("MDI_CpuDifficulty", cpuDifficultyIndex);
            UpdateOptionsLabels();
            CombatAudioDirector.Instance?.PlayButtonHover();
        }

        private void UpdateOptionsLabels()
        {
            if (txtStageSelected != null && selectedStageIndex < StageNames.Length)
            {
                txtStageSelected.text = $"STAGE: {StageNames[selectedStageIndex]}";
            }
            if (txtRoundsSelected != null)
            {
                txtRoundsSelected.text = $"ROUNDS: BEST OF {(roundsToWin * 2 - 1)} ({roundsToWin} WINS)";
            }
            if (txtDifficultySelected != null && cpuDifficultyIndex < DifficultyNames.Length)
            {
                txtDifficultySelected.text = $"AI DIFFICULTY: {DifficultyNames[cpuDifficultyIndex]}";
            }
        }

        private void UpdateFooterPrompts()
        {
            if (footerPromptsText == null) return;

            if (!p1Locked)
            {
                footerPromptsText.text = "[WASD / DPad] CHOOSE FIGHTER    [J / A] LOCK IN    [K / B] BACK TO MENU";
            }
            else if (isVersus && !p2Locked)
            {
                footerPromptsText.text = "P1 LOCKED! P2: [ARROWS] CHOOSE    [NUM0 / SOUTH] LOCK IN    [K / B] UNLOCK P1";
            }
            else
            {
                footerPromptsText.text = "FIGHTERS READY! [ENTER / J / SOUTH] START MATCH    [K / B] CANCEL LOCK";
            }
        }

        // =========================================================================
        // SELECTION & MATCH LAUNCH
        // =========================================================================
        public void SelectSlot(int index, bool isP1)
        {
            if (index < 0 || index >= RosterIds.Length) return;
            if (isP1)
            {
                p1Index = index;
                LockInFighter(isP1: true);
            }
            else
            {
                p2Index = index;
                LockInFighter(isP1: false);
            }
            UpdateHighlights();
            UpdateDualPreviews();
            UpdateWings();
            UpdateDossier(RosterIds[isP1 ? p1Index : p2Index]);
        }

        public void UpdateDossier(string charId)
        {
            var def = CharacterRegistry.GetCharacterById(charId);
            if (def == null) return;

            if (dossierName != null) dossierName.text = $"{def.displayName.ToUpper()} ({charId})";
            if (dossierArchetype != null) dossierArchetype.text = $"ROLE: {def.archetype.ToUpper()}";
            if (dossierBio != null) dossierBio.text = string.IsNullOrEmpty(def.description) ? "Elite martial combatant tuned for planar arena battles." : def.description;
        }

        public IEnumerator LaunchMatchSequence()
        {
            if (_isTransitioning) yield break;
            _isTransitioning = true;

            string p1Id = RosterIds[p1Index];
            string p2Id = RosterIds[p2Index];
            string stageId = StageIds[selectedStageIndex];

            // Save persistent preferences
            PlayerPrefs.SetString("MDI_P1_CharacterId", p1Id);
            PlayerPrefs.SetString("MDI_P2_CharacterId", p2Id);
            PlayerPrefs.SetString("MDI_StageId", stageId);
            PlayerPrefs.SetInt("MDI_RoundsToWin", roundsToWin);
            PlayerPrefs.SetInt("MDI_CpuDifficulty", cpuDifficultyIndex);
            PlayerPrefs.Save();

            LaunchServices.Session.BeginMatch(p1Id, p2Id, stageId);

            // Trigger Dramatic Ink-Wipe VS Overlay
            if (vsOverlay != null)
            {
                vsOverlay.SetActive(true);
                var def1 = CharacterRegistry.GetCharacterById(p1Id);
                var def2 = CharacterRegistry.GetCharacterById(p2Id);
                if (vsP1Name != null) vsP1Name.text = def1 != null ? def1.displayName.ToUpper() : p1Id;
                if (vsP2Name != null) vsP2Name.text = def2 != null ? def2.displayName.ToUpper() : p2Id;
                if (vsStageName != null) vsStageName.text = StageNames[selectedStageIndex];

                Sprite mug1 = Resources.Load<Sprite>($"UI/Portraits/Mugshots/Mugshot_{p1Id}_{(def1 != null ? def1.displayName : "")}");
                Sprite mug2 = Resources.Load<Sprite>($"UI/Portraits/Mugshots/Mugshot_{p2Id}_{(def2 != null ? def2.displayName : "")}");
                if (vsP1Portrait != null && mug1 != null) vsP1Portrait.sprite = mug1;
                if (vsP2Portrait != null && mug2 != null) vsP2Portrait.sprite = mug2;

                float waitDuration = SumiStyleTokens.InkWipeDuration;
                yield return new WaitForSecondsRealtime(Mathf.Max(0.15f, waitDuration));
            }
            else
            {
                yield return null;
            }

            SceneManager.LoadScene("BattleArena");
        }

        private void EnsureDualPedestals()
        {
            if (p1Pedestal == null)
            {
                var p1 = GameObject.Find("Preview_Pedestal_P1");
                if (p1 == null)
                {
                    p1 = new GameObject("Preview_Pedestal_P1");
                    p1.transform.position = new Vector3(-2.2f, 0f, 0f);
                    p1.transform.rotation = Quaternion.Euler(0f, 115f, 0f);
                }
                p1Pedestal = p1.transform;
            }

            if (p2Pedestal == null)
            {
                var p2 = GameObject.Find("Preview_Pedestal_P2");
                if (p2 == null)
                {
                    p2 = new GameObject("Preview_Pedestal_P2");
                    p2.transform.position = new Vector3(2.2f, 0f, 0f);
                    p2.transform.rotation = Quaternion.Euler(0f, -115f, 0f);
                }
                p2Pedestal = p2.transform;
            }

            if (previewPedestal == null) previewPedestal = p1Pedestal;
            if (previewCamera == null) previewCamera = Camera.main;

            // Ensure SafeAreaFitter is present
            if (GetComponent<SafeAreaFitter>() == null && GetComponent<RectTransform>() != null)
            {
                gameObject.AddComponent<SafeAreaFitter>();
            }
        }
    }

    /// <summary>
    /// Type alias for DemonIdolCharacterSelectManager.
    /// </summary>
    public class DemonIdolCharacterSelectController : DemonIdolCharacterSelectManager
    {
    }
}
