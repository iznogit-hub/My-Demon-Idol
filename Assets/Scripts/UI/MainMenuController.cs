using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DemonIdol.Combat;
using DemonIdol.Launch;

namespace DemonIdol.UI
{
    /// <summary>
    /// Master UI Controller for Assets/Scenes/MainMenu.unity.
    /// Manages the full menu flow: Title Screen -> Mode Select -> Character Select -> Stage Select -> Launch into ArcadeArena.
    /// Handles 3D character preview pedestal, live stat display, audio feedback, and session initialization.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        public static MainMenuController Instance { get; private set; }

        public enum MenuState
        {
            Title,
            ModeSelect,
            CharacterSelect,
            StageSelect
        }

        [Header("Menu State")]
        public MenuState CurrentState = MenuState.Title;
        public string ActiveMode = "Arcade"; // "Arcade", "LocalVersus", "Training"
        public string SelectedP1Id = "C12";   // Default: C12 Fina
        public string SelectedP2Id = "C07";   // Default: C07 StellarWitch
        public string SelectedStageId = "ST01"; // Default: ST01
        public bool IsSelectingP2 = false;

        [Header("Screens / Panels")]
        public GameObject TitlePanel;
        public GameObject ModeSelectPanel;
        public GameObject CharacterSelectPanel;
        public GameObject StageSelectPanel;

        [Header("Title Screen UI")]
        public Button BtnTitleStart;
        public Text TitleQuoteText;

        [Header("Mode Select UI")]
        public Button BtnModeArcade;
        public Button BtnModeVersus;
        public Button BtnModeTraining;
        public Button BtnModeBack;

        [Header("Character Select UI - Faction Tabs")]
        public Button BtnFactionAll;
        public Button BtnFactionSorcerers;
        public Button BtnFactionSidekicks;
        public Button BtnFactionDota;
        public Image FactionTabHighlightAll;
        public Image FactionTabHighlightSorcerers;
        public Image FactionTabHighlightSidekicks;
        public Image FactionTabHighlightDota;
        public int ActiveFactionIndex = 0; // 0=All (30), 1=Sorcerers (14), 2=Sidekicks (8), 3=Dota Heroes (8)

        [Header("Character Select UI - Roster Grid")]
        public Transform RosterGridRoot;
        public Button[] CharacterButtons;
        public Image[] CharacterHighlights;
        public Text[] CharacterNameLabels;

        [Header("Character Select UI - Dossier")]
        public Text DossierNameText;
        public Text DossierTitleText;
        public Text DossierArchetypeText;
        public Text DossierDomainText;
        public Text DossierDescText;
        public Image StatSpeedFill;
        public Image StatPowerFill;
        public Image StatReachFill;
        public Image StatDefenseFill;
        public Text StatSpeedValue;
        public Text StatPowerValue;
        public Text StatReachValue;
        public Text StatDefenseValue;
        public Text SelectionBadgeText;
        public Button BtnTabP1;
        public Button BtnTabP2;
        public Button BtnCharSelectNext;
        public Button BtnCharSelectBack;

        [Header("Stage Select UI")]
        public Button[] StageButtons;
        public Image[] StageHighlights;
        public Text StageNameText;
        public Text StageHazardText;
        public Text StageSurfaceText;
        public Button BtnFight;
        public Button BtnStageBack;

        [Header("3D Character Pedestal")]
        public Transform PedestalSpawnPoint;
        private GameObject _activePedestalModel;

        public static readonly string[] CharacterIds = new[]
        {
            // Faction 1: Demon Sorcerers (13). C03 Ren was removed.
            "C01", "C02", "C04", "C05", "C06", "C07",
            "C08", "C09", "C10", "C11", "C12", "C13", "C14",

            // Faction 2: Synty Sidekicks (5). Valen, Jin, and Malakar were removed. CommanderRen stays.
            "SK02", "SK03", "SK05", "SK06", "SK08",

            // Faction 3: External Dota Heroes (7). Garrick was removed.
            "EX01", "EX02", "EX03", "EX04", "EX05", "EX06", "EX07"
        };

        private static readonly string[] StageIds = new[]
        {
            "ST01", "ST02", "ST03", "ST04", "ST05", "ST06", "ST07"
        };

        private static readonly string[] StageNames = new[]
        {
            "CENTRAL COURTYARD // CLOCKTOWER PLAZA",
            "SAKURA MAIN GATE // ACADEMY AVENUE",
            "ROOFTOP OBSERVATORY // METROPOLIS SKYLINE",
            "MARTIAL ARTS DOJO // TRADITIONAL PAVILION",
            "ATHLETIC STADIUM // SPORTS TRACK & BLEACHERS",
            "TRANSIT PROMENADE // CAMPUS BIKE HUB",
            "COLLEGE RAIMON // MIDNIGHT RAIN COURTYARD"
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

        private void Awake()
        {
            Instance = this;
            WireEvents();
        }

        private void Start()
        {
            MenuState initial = MenuState.Title;
            if (PlayerPrefs.HasKey("MDI_MenuInitialState"))
            {
                string stateStr = PlayerPrefs.GetString("MDI_MenuInitialState");
                PlayerPrefs.DeleteKey("MDI_MenuInitialState");
                if (System.Enum.TryParse<MenuState>(stateStr, out var parsed))
                {
                    initial = parsed;
                }
            }
            else if (PlayerPrefs.GetInt("DemonIdol_ReturnToCharSelect", 0) == 1)
            {
                PlayerPrefs.DeleteKey("DemonIdol_ReturnToCharSelect");
                initial = MenuState.CharacterSelect;
            }

            if (PlayerPrefs.HasKey("MDI_P1_CharacterId"))
            {
                string p1Pref = PlayerPrefs.GetString("MDI_P1_CharacterId");
                if (!string.IsNullOrEmpty(p1Pref) && System.Array.IndexOf(CharacterIds, p1Pref) >= 0) SelectedP1Id = p1Pref;
            }
            if (PlayerPrefs.HasKey("MDI_P2_CharacterId"))
            {
                string p2Pref = PlayerPrefs.GetString("MDI_P2_CharacterId");
                if (!string.IsNullOrEmpty(p2Pref) && System.Array.IndexOf(CharacterIds, p2Pref) >= 0) SelectedP2Id = p2Pref;
            }

            SetMenuState(initial);
            SetFactionFilter(0);
            UpdateCharacterDossier(SelectedP1Id);
            UpdatePedestalModel(SelectedP1Id);
            UpdateStageDossier(0);
        }

        private void WireEvents()
        {
            if (BtnTitleStart != null) BtnTitleStart.onClick.AddListener(() => SetMenuState(MenuState.ModeSelect));

            if (BtnModeArcade != null) BtnModeArcade.onClick.AddListener(() => SelectMode("Arcade"));
            if (BtnModeVersus != null) BtnModeVersus.onClick.AddListener(() => SelectMode("LocalVersus"));
            if (BtnModeTraining != null) BtnModeTraining.onClick.AddListener(() => SelectMode("Training"));
            if (BtnModeBack != null) BtnModeBack.onClick.AddListener(() => SetMenuState(MenuState.Title));

            if (BtnFactionAll != null) BtnFactionAll.onClick.AddListener(() => SetFactionFilter(0));
            if (BtnFactionSorcerers != null) BtnFactionSorcerers.onClick.AddListener(() => SetFactionFilter(1));
            if (BtnFactionSidekicks != null) BtnFactionSidekicks.onClick.AddListener(() => SetFactionFilter(2));
            if (BtnFactionDota != null) BtnFactionDota.onClick.AddListener(() => SetFactionFilter(3));

            if (BtnTabP1 != null) BtnTabP1.onClick.AddListener(() => SetSelectingP1(true));
            if (BtnTabP2 != null) BtnTabP2.onClick.AddListener(() => SetSelectingP1(false));

            if (BtnCharSelectNext != null) BtnCharSelectNext.onClick.AddListener(() => SetMenuState(MenuState.StageSelect));
            if (BtnCharSelectBack != null) BtnCharSelectBack.onClick.AddListener(() => SetMenuState(MenuState.ModeSelect));

            if (BtnFight != null) BtnFight.onClick.AddListener(OnLaunchBattle);
            if (BtnStageBack != null) BtnStageBack.onClick.AddListener(() => SetMenuState(MenuState.CharacterSelect));

            // Wire character buttons
            if (CharacterButtons != null)
            {
                for (int i = 0; i < CharacterButtons.Length; i++)
                {
                    int index = i;
                    if (CharacterButtons[i] != null)
                    {
                        CharacterButtons[i].onClick.AddListener(() => OnCharacterButtonClicked(index));
                    }
                }
            }

            // Wire stage buttons
            if (StageButtons != null)
            {
                for (int i = 0; i < StageButtons.Length; i++)
                {
                    int index = i;
                    if (StageButtons[i] != null)
                    {
                        StageButtons[i].onClick.AddListener(() => OnStageButtonClicked(index));
                    }
                }
            }
        }

        public void SetMenuState(MenuState state)
        {
            CurrentState = state;

            if (TitlePanel != null) TitlePanel.SetActive(state == MenuState.Title);
            if (ModeSelectPanel != null) ModeSelectPanel.SetActive(state == MenuState.ModeSelect);
            if (CharacterSelectPanel != null) CharacterSelectPanel.SetActive(state == MenuState.CharacterSelect);
            if (StageSelectPanel != null) StageSelectPanel.SetActive(state == MenuState.StageSelect);

            if (state == MenuState.CharacterSelect)
            {
                FilterRosterCards();
                UpdateRosterHighlights();
                UpdateCharacterDossier(IsSelectingP2 ? SelectedP2Id : SelectedP1Id);
                UpdatePedestalModel(IsSelectingP2 ? SelectedP2Id : SelectedP1Id);
            }
            else if (state == MenuState.StageSelect)
            {
                int stageIdx = Array.IndexOf(StageIds, SelectedStageId);
                if (stageIdx < 0) stageIdx = 0;
                UpdateStageDossier(stageIdx);
            }
        }

        public void SelectMode(string mode)
        {
            ActiveMode = mode;
            IsSelectingP2 = false;
            SetMenuState(MenuState.CharacterSelect);
        }

        public void SetSelectingP1(bool selectingP1)
        {
            IsSelectingP2 = !selectingP1;
            string targetId = IsSelectingP2 ? SelectedP2Id : SelectedP1Id;
            UpdateRosterHighlights();
            UpdateCharacterDossier(targetId);
            UpdatePedestalModel(targetId);
        }

        public void SetFactionFilter(int factionIndex)
        {
            ActiveFactionIndex = factionIndex;
            UpdateFactionTabs();
            FilterRosterCards();
        }

        private void UpdateFactionTabs()
        {
            Color activeCol = new Color(0.9f, 0.15f, 0.35f, 1f);
            Color inactiveCol = new Color(0.15f, 0.18f, 0.25f, 0.8f);

            if (FactionTabHighlightAll != null) FactionTabHighlightAll.color = (ActiveFactionIndex == 0) ? activeCol : inactiveCol;
            if (FactionTabHighlightSorcerers != null) FactionTabHighlightSorcerers.color = (ActiveFactionIndex == 1) ? activeCol : inactiveCol;
            if (FactionTabHighlightSidekicks != null) FactionTabHighlightSidekicks.color = (ActiveFactionIndex == 2) ? activeCol : inactiveCol;
            if (FactionTabHighlightDota != null) FactionTabHighlightDota.color = (ActiveFactionIndex == 3) ? activeCol : inactiveCol;
        }

        public void FilterRosterCards()
        {
            if (CharacterButtons == null) return;

            for (int i = 0; i < CharacterButtons.Length && i < CharacterIds.Length; i++)
            {
                if (CharacterButtons[i] == null) continue;
                string id = CharacterIds[i];
                bool show = ActiveFactionIndex switch
                {
                    1 => id.StartsWith("C"),   // Sorcerers (C01-C14)
                    2 => id.StartsWith("SK"),  // Sidekicks (SK01-SK08)
                    3 => id.StartsWith("EX"),  // Dota Heroes (EX01-EX08)
                    _ => true                  // All 30
                };
                CharacterButtons[i].gameObject.SetActive(show);
            }
        }

        private void OnCharacterButtonClicked(int index)
        {
            if (index < 0 || index >= CharacterIds.Length) return;
            string pickedId = CharacterIds[index];

            if (IsSelectingP2)
            {
                SelectedP2Id = pickedId;
                if (ActiveMode == "LocalVersus")
                {
                    IsSelectingP2 = false; // Toggle back to P1
                }
            }
            else
            {
                SelectedP1Id = pickedId;
                if (ActiveMode == "LocalVersus")
                {
                    IsSelectingP2 = true; // Prompt P2 selection next
                }
            }

            UpdateRosterHighlights();
            UpdateCharacterDossier(pickedId);
            UpdatePedestalModel(pickedId);
        }

        private void OnStageButtonClicked(int index)
        {
            if (index < 0 || index >= StageIds.Length) return;
            SelectedStageId = StageIds[index];
            UpdateStageDossier(index);
        }

        private void UpdateRosterHighlights()
        {
            if (CharacterHighlights == null) return;

            for (int i = 0; i < CharacterHighlights.Length && i < CharacterIds.Length; i++)
            {
                if (CharacterHighlights[i] == null) continue;
                string id = CharacterIds[i];
                bool isP1 = (id == SelectedP1Id);
                bool isP2 = (id == SelectedP2Id);

                if (isP1 && isP2)
                {
                    CharacterHighlights[i].color = new Color(0.7f, 0.4f, 1f, 1f); // Purple hybrid
                }
                else if (isP1)
                {
                    CharacterHighlights[i].color = new Color(0f, 0.9f, 1f, 1f); // Cyan P1
                }
                else if (isP2)
                {
                    CharacterHighlights[i].color = new Color(1f, 0.2f, 0.35f, 1f); // Crimson P2
                }
                else
                {
                    CharacterHighlights[i].color = new Color(1f, 1f, 1f, 0.1f); // Dim outline
                }
            }

            if (SelectionBadgeText != null)
            {
                SelectionBadgeText.text = IsSelectingP2 ? "[ SELECTING PLAYER 2 (OPPONENT) ]" : "[ SELECTING PLAYER 1 (CHALLENGER) ]";
                SelectionBadgeText.color = IsSelectingP2 ? new Color(1f, 0.25f, 0.35f, 1f) : new Color(0f, 0.9f, 1f, 1f);
            }
        }

        private void UpdateCharacterDossier(string charId)
        {
            CharacterDefinition def = CharacterRegistry.GetCharacterById(charId);
            if (def == null) return;

            if (DossierNameText != null) DossierNameText.text = def.displayName.ToUpper();
            if (DossierTitleText != null) DossierTitleText.text = def.title.ToUpper();
            if (DossierArchetypeText != null) DossierArchetypeText.text = $"ARCHETYPE: {def.archetype.ToUpper()}";
            if (DossierDomainText != null) DossierDomainText.text = $"⚡ DOMAIN: {def.domainName.ToUpper()}";
            if (DossierDescText != null) DossierDescText.text = def.description;

            float spd = Mathf.Clamp01(def.speed / 100f);
            float pwr = Mathf.Clamp01(def.power / 100f);
            float rch = Mathf.Clamp01(def.reach / 100f);
            float defn = Mathf.Clamp01(def.defense / 100f);

            if (StatSpeedFill != null) StatSpeedFill.fillAmount = spd;
            if (StatPowerFill != null) StatPowerFill.fillAmount = pwr;
            if (StatReachFill != null) StatReachFill.fillAmount = rch;
            if (StatDefenseFill != null) StatDefenseFill.fillAmount = defn;

            if (StatSpeedValue != null) StatSpeedValue.text = $"{(int)def.speed}%";
            if (StatPowerValue != null) StatPowerValue.text = $"{(int)def.power}%";
            if (StatReachValue != null) StatReachValue.text = $"{(int)def.reach}%";
            if (StatDefenseValue != null) StatDefenseValue.text = $"{(int)def.defense}%";
        }

        private void UpdateStageDossier(int index)
        {
            if (index < 0 || index >= StageNames.Length) return;

            if (StageNameText != null) StageNameText.text = StageNames[index];
            if (StageHazardText != null) StageHazardText.text = StageHazards[index];
            if (StageSurfaceText != null) StageSurfaceText.text = $"SURFACE: PLATFORM // BOUNDS: ±14.0 METERS";

            if (StageHighlights != null)
            {
                for (int i = 0; i < StageHighlights.Length; i++)
                {
                    if (StageHighlights[i] == null) continue;
                    StageHighlights[i].color = (i == index)
                        ? new Color(1f, 0.85f, 0.2f, 1f) // Gold highlight
                        : new Color(1f, 1f, 1f, 0.15f);
                }
            }
        }

        private void UpdatePedestalModel(string charId)
        {
            if (PedestalSpawnPoint == null) return;

            if (_activePedestalModel != null)
            {
                Destroy(_activePedestalModel);
                _activePedestalModel = null;
            }

            var look = FighterVisualCatalog.Find(charId);
            if (look == null || string.IsNullOrEmpty(look.PrefabAssetPath)) return;

            GameObject prefab = null;
#if UNITY_EDITOR
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(look.PrefabAssetPath);
#endif
            if (prefab == null) return;

            _activePedestalModel = Instantiate(prefab, PedestalSpawnPoint);
            _activePedestalModel.transform.localPosition = Vector3.zero;
            _activePedestalModel.transform.localRotation = Quaternion.Euler(0, 180, 0); // Face the camera
            _activePedestalModel.transform.localScale = Vector3.one;

            // Apply Idle animation
            Animator anim = _activePedestalModel.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.applyRootMotion = false;
            }
        }

        public void OnLaunchBattle()
        {
            Debug.Log($"<color=#00FF66>[MainMenu] Launching Battle: P1={SelectedP1Id} vs P2={SelectedP2Id} | Stage={SelectedStageId} | Mode={ActiveMode}</color>");

            // Commit session state
            if (LaunchServices.Session != null)
            {
                LaunchServices.Session.TrySelectMode(ActiveMode, out _);
                LaunchServices.Session.BeginMatch(SelectedP1Id, SelectedP2Id, SelectedStageId);
            }

            // Save player prefs for runtime battle arena initialization
            PlayerPrefs.SetString("MDI_P1_CharacterId", SelectedP1Id);
            PlayerPrefs.SetString("MDI_P2_CharacterId", SelectedP2Id);
            PlayerPrefs.SetString("MDI_StageId", SelectedStageId);
            PlayerPrefs.SetString("MDI_GameMode", ActiveMode);
            PlayerPrefs.Save();

            // Check if PreMatchVSOverlay is present in scene
            var vsOverlay = FindAnyObjectByType<PreMatchVSOverlay>();
            if (vsOverlay != null)
            {
                vsOverlay.PlayMatchIntro(SelectedP1Id, SelectedP2Id, SelectedStageId, ActiveMode, () =>
                {
                    SceneManager.LoadScene("ArcadeArena");
                });
            }
            else
            {
                SceneManager.LoadScene("ArcadeArena");
            }
        }
    }
}
