using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DemonIdol.Combat;

namespace DemonIdol.UI
{
    /// <summary>
    /// Master Arena Stage Selection Screen UI (Valorant / Tekken / Street Fighter Style).
    /// Features:
    /// - 4 Active Theatres: Reactor Core Silo, Heavy Cyber Hangar, Neo Shibuya Colosseum, Ink Fortress Cathedral.
    /// - Locked "COMING SOON" expansion slots for future combat sectors.
    /// - Left: Comprehensive Arena Intel Dossier (Location, Hazards, Boundaries, Wall Splats, BGM).
    /// - Right: High-contrast 4-Stage selection grid.
    /// - Bottom: "DEPLOY TO BATTLE ARENA ▶" tactical action button and Back to Operatives button.
    /// 100% native procedural Unity UI with zero external sprite/video dependencies.
    /// </summary>
    [System.Obsolete("Superseded by 3-scene DemonIdolMainMenuController / DemonIdolCharacterSelectManager")]
    [RequireComponent(typeof(RectTransform))]
    public class StageSelectUI : MonoBehaviour
    {
        public static StageSelectUI Instance { get; private set; }

        public int SelectedStageIndex = 0;
        public string ActiveGameMode = "Training";

        [Header("Canvas & Hierarchy")]
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Header Elements")]
        [SerializeField] private Text _screenTitleText;
        [SerializeField] private Text _screenSubtitleText;
        [SerializeField] private Text _modeBadgeText;

        [Header("Left Spotlight Dossier")]
        [SerializeField] private Text _dossierSerialText;
        [SerializeField] private Text _dossierStatusText;
        [SerializeField] private Text _stageNameText;
        [SerializeField] private Text _stageSubtitleText;
        [SerializeField] private Text _stageHazardText;
        [SerializeField] private Text _stageDescText;

        [Header("Tactical Parameters (Meters & Bounds)")]
        [SerializeField] private Image _widthBarFill;
        [SerializeField] private Image _wallBarFill;
        [SerializeField] private Image _camBarFill;
        [SerializeField] private Image _surfaceBarFill;
        [SerializeField] private Text _widthValueText;
        [SerializeField] private Text _wallValueText;
        [SerializeField] private Text _camValueText;
        [SerializeField] private Text _surfaceValueText;

        [Header("Audio & BGM Dossier")]
        [SerializeField] private Text _stageBgmText;
        [SerializeField] private Text _stageBgmSubtext;

        [Header("Stage Cards Grid (4 Active Slots)")]
        [SerializeField] private Button[] _stageButtons;
        [SerializeField] private Image[] _stageHighlights;
        [SerializeField] private Text[] _stageCardNames;
        [SerializeField] private Text[] _stageCardIds;
        [SerializeField] private Text[] _stageCardArchetypes;

        [Header("Action Buttons")]
        [SerializeField] private Button _btnDeploy;
        [SerializeField] private Button _btnBack;
        [SerializeField] private Text _deployButtonText;

        private static readonly string[] StageSubtitles = new[]
        {
            "ACADEMY COURTYARD // CLOCKTOWER PLAZA",
            "ACADEMY MAIN GATE // SAKURA AVENUE",
            "CAMPUS ROOFTOP // METROPOLIS SKYLINE",
            "CAMPUS DOJO // TRADITIONAL PAVILION",
            "SPORTS COMPLEX // ATHLETIC TRACK",
            "CAMPUS PROMENADE // TRANSIT EDGE",
            "ACADEMY COURTYARD // THUNDERSTORM MIDNIGHT"
        };

        private static readonly string[] StageHazards = new[]
        {
            "[ CLOCKTOWER FACADE • PLANAR 2.5D ARENA ]",
            "[ CHERRY BLOSSOM SWARM • MORNING DAWN ]",
            "[ METROPOLIS SKYLINE • DRAMATIC SUNSET ]",
            "[ TRADITIONAL WOODEN DOJO • LANTERN ACCENTS ]",
            "[ ATHLETIC BLEACHERS • OVERCAST FLOODLIGHTS ]",
            "[ TRANSIT PROMENADE • DUSK NEON GLOW ]",
            "[ SAKURA COURTYARD • MIDNIGHT RAIN REFLECTIONS ]"
        };

        private static readonly string[] StageSurfaces = new[]
        {
            "WEATHERED COURTYARD STONE PAVERS",
            "SAKURA-STREWN ASPHALT & STONE PILLARS",
            "REINFORCED ROOFTOP DECK & GRAVEL",
            "POLISHED TRADITIONAL DOJO CEDAR DECKING",
            "SYNTHETIC ATHLETIC RUNNING TRACK",
            "POLISHED CONCRETE PROMENADE TILES",
            "RAIN-SLICKED POLISHED STONE WITH WET REFLECTIONS"
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (_btnDeploy != null) _btnDeploy.onClick.AddListener(OnClickDeploy);
            if (_btnBack != null) _btnBack.onClick.AddListener(OnClickBack);

            SetupStageButtons();
            gameObject.SetActive(false);
        }

        public void OpenForMode(string mode)
        {
            ActiveGameMode = mode;
            gameObject.SetActive(true);
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = true;
            }

            if (_modeBadgeText != null)
            {
                _modeBadgeText.text = $"[ MODE: {ActiveGameMode.ToUpper()} ]";
            }

            SelectStage(SelectedStageIndex);
        }

        public void Close()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
            }
            gameObject.SetActive(false);
        }

        private void SetupStageButtons()
        {
            int count = _stageButtons != null ? _stageButtons.Length : 0;
            for (int i = 0; i < count; i++)
            {
                int index = i;
                if (_stageButtons[i] != null)
                {
                    _stageButtons[i].onClick.RemoveAllListeners();
                    _stageButtons[i].onClick.AddListener(() => SelectStage(index));
                }
            }
        }

        public void SelectStage(int index)
        {
            var stages = StageRegistry.GetAllStages();
            if (stages == null || stages.Count == 0) return;

            // Constrain to the 7 approved active battle stages
            SelectedStageIndex = Mathf.Clamp(index, 0, Mathf.Min(6, stages.Count - 1));
            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();

            StageDefinition stage = stages[SelectedStageIndex];

            // 1. Update Left Dossier
            if (_dossierSerialText != null)
            {
                _dossierSerialText.text = $"// SECTOR DOSSIER • ARENA {stage.id.ToUpper()}";
            }
            if (_dossierStatusText != null)
            {
                _dossierStatusText.text = "[ COMBAT ZONE AUTHORIZED ]";
            }
            if (_stageNameText != null)
            {
                _stageNameText.text = stage.displayName.ToUpper();
            }
            if (_stageSubtitleText != null)
            {
                _stageSubtitleText.text = (SelectedStageIndex < StageSubtitles.Length) 
                    ? StageSubtitles[SelectedStageIndex] 
                    : "AUTHOR-APPROVED SECTOR";
            }
            if (_stageHazardText != null)
            {
                _stageHazardText.text = (SelectedStageIndex < StageHazards.Length) 
                    ? StageHazards[SelectedStageIndex] 
                    : "[ STANDARD COMBAT BOUNDARIES ]";
            }
            if (_stageDescText != null)
            {
                _stageDescText.text = stage.description;
            }

            // 2. Tactical Parameters
            if (_widthBarFill != null) _widthBarFill.fillAmount = Mathf.Clamp01(stage.playableWidth / 30f);
            if (_widthValueText != null) _widthValueText.text = $"{stage.playableWidth:F1}M (±{stage.ArenaLimitX:F1}M)";

            if (_wallBarFill != null) _wallBarFill.fillAmount = Mathf.Clamp01(stage.wallThreshold / 15f);
            if (_wallValueText != null) _wallValueText.text = $"{stage.wallThreshold:F1}M (WALL SPLAT ACTIVE)";

            if (_camBarFill != null) _camBarFill.fillAmount = 0.75f;
            if (_camValueText != null) _camValueText.text = $"DYNAMIC [{stage.cameraMaxZ:F1}M, {stage.cameraMinZ:F1}M]";

            if (_surfaceBarFill != null) _surfaceBarFill.fillAmount = 0.85f;
            if (_surfaceValueText != null)
            {
                _surfaceValueText.text = (SelectedStageIndex < StageSurfaces.Length) 
                    ? StageSurfaces[SelectedStageIndex] 
                    : "REINFORCED CARBON SLAB";
            }

            // 3. Audio & BGM
            if (_stageBgmText != null)
            {
                _stageBgmText.text = $"🎵 BGM: {stage.bgmTrackName.ToUpper()}";
            }
            if (_stageBgmSubtext != null)
            {
                _stageBgmSubtext.text = "60HZ DYNAMIC STEREO // 128 BPM COMBAT PACE";
            }

            // 4. Update Card Highlights
            if (_stageHighlights != null)
            {
                for (int i = 0; i < _stageHighlights.Length; i++)
                {
                    if (_stageHighlights[i] != null)
                    {
                        bool isSelected = (i == SelectedStageIndex);
                        _stageHighlights[i].color = isSelected 
                            ? new Color(0f, 0.90f, 1f, 1f) // Radianite Cyan active glow
                            : new Color(1f, 1f, 1f, 0.08f); // Inactive dark border
                    }
                }
            }

            if (_deployButtonText != null)
            {
                _deployButtonText.text = "DEPLOY TO BATTLE ARENA ▶";
            }
        }

        public void OnClickDeploy()
        {
            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();
            var stages = StageRegistry.GetAllStages();
            StageDefinition selectedStage = (stages != null && SelectedStageIndex < stages.Count) 
                ? stages[SelectedStageIndex] 
                : null;

            Close();

            if (BloodTechUIManager.Instance != null)
            {
                BloodTechUIManager.Instance.TransitionToFight(selectedStage);
            }
        }

        public void OnClickBack()
        {
            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();
            Close();

            if (BloodTechUIManager.Instance != null)
            {
                BloodTechUIManager.Instance.TransitionToCharacterSelect(ActiveGameMode);
            }
        }
    }
}
