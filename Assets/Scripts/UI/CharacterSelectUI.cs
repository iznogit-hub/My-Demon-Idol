using System;
using UnityEngine;
using UnityEngine.UI;
using DemonIdol.Combat;
using DemonIdol.Launch;

namespace DemonIdol.UI
{
    /// <summary>
    /// Master Character Selection Screen UI (Valorant / Tekken / Street Fighter style).
    /// Tactical operative selection matrix featuring:
    /// - Left: Comprehensive Operative Dossier (Identity, Bio, 4-Parameter Attribute Matrix, Domain Expansion preview)
    /// - Right: 14-Agent Roster Grid (2 rows of 7 Operative cards with Cyan P1 & Crimson P2 highlights)
    /// - Top: Operative Switcher Tabs ([ P1: NAME ] / [ P2: NAME ])
    /// - Bottom: "LOCK IN OPERATIVE ▶" tactical confirmation button and Back to Hub.
    /// 100% native procedural Unity UI with zero external sprite/video dependencies.
    /// </summary>
    public class CharacterSelectUI : MonoBehaviour
    {
        public static CharacterSelectUI Instance { get; private set; }

        [Header("Selection State")]
        public int SelectedP1Index = 0; // Default: Akio (C01)
        public int SelectedP2Index = 1; // Default: Aika (C02)
        public bool IsSelectingP2 = false; // Toggle whether clicking selects P1 or P2
        public string ActiveGameMode = "Training"; // "PvP", "Arcade", "Story", "Training", "LocalVersus"

        public CharacterDefinition SelectedP1Definition { get; private set; }
        public CharacterDefinition SelectedP2Definition { get; private set; }

        [Header("Fighter Cards (14 Slots)")]
        [SerializeField] private Button[] _rosterButtons;
        [SerializeField] private Image[] _rosterHighlights;
        [SerializeField] private Text[] _rosterNames;
        [SerializeField] private Image[] _rosterP1Badges;
        [SerializeField] private Image[] _rosterP2Badges;

        [Header("Player Target Selectors")]
        [SerializeField] private Button _btnTabP1;
        [SerializeField] private Button _btnTabP2;
        [SerializeField] private Text _tabP1Text;
        [SerializeField] private Text _tabP2Text;
        [SerializeField] private Image _tabP1Border;
        [SerializeField] private Image _tabP2Border;

        [Header("Active Selection Stats & Details")]
        [SerializeField] private Text _detailNameText;
        [SerializeField] private Text _detailTitleText;
        [SerializeField] private Text _detailArchetypeText;
        [SerializeField] private Text _detailDescriptionText;
        [SerializeField] private Text _activeSelectionBadgeText;

        [Header("Stat Bars")]
        [SerializeField] private Image _speedBarFill;
        [SerializeField] private Image _powerBarFill;
        [SerializeField] private Image _reachBarFill;
        [SerializeField] private Image _defenseBarFill;

        [Header("Stat Numeric Values")]
        [SerializeField] private Text _speedValueText;
        [SerializeField] private Text _powerValueText;
        [SerializeField] private Text _reachValueText;
        [SerializeField] private Text _defenseValueText;

        [Header("Domain Expansion Preview")]
        [SerializeField] private Text _domainNameText;
        [SerializeField] private Text _domainDescText;

        [Header("Action Buttons")]
        [SerializeField] private Button _btnDeploy;
        [SerializeField] private Button _btnBackToModes;
        [SerializeField] private Text _deployButtonText;

        [Header("Runtime Prefab References")]
        [SerializeField] private GameObject[] _characterPrefabs;

        public event Action<FighterProfile, FighterProfile, string> OnFightersDeployed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (_btnDeploy != null) _btnDeploy.onClick.AddListener(OnClickDeploy);
            if (_btnBackToModes != null) _btnBackToModes.onClick.AddListener(OnClickBack);
            if (_btnTabP1 != null) _btnTabP1.onClick.AddListener(SetSelectingP1);
            if (_btnTabP2 != null) _btnTabP2.onClick.AddListener(SetSelectingP2);

            SetupRosterButtons();
        }

        private void OnEnable()
        {
            IsSelectingP2 = false;
            SelectFighter(SelectedP1Index);
        }

        public void SetSelectingP1()
        {
            IsSelectingP2 = false;
            UpdateSelectionTabs();
            DisplayActiveDetails(SelectedP1Definition);
            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();
        }

        public void SetSelectingP2()
        {
            IsSelectingP2 = true;
            UpdateSelectionTabs();
            DisplayActiveDetails(SelectedP2Definition);
            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();
        }

        public void TogglePlayerSelection()
        {
            if (IsSelectingP2) SetSelectingP1();
            else SetSelectingP2();
        }

        private void UpdateSelectionTabs()
        {
            var characters = CharacterRegistry.GetAllCharacters();
            string p1Name = (SelectedP1Index < characters.Count) ? characters[SelectedP1Index].displayName : "P1";
            string p2Name = (SelectedP2Index < characters.Count) ? characters[SelectedP2Index].displayName : "P2";

            if (_tabP1Text != null)
            {
                _tabP1Text.text = !IsSelectingP2 ? $"▶ [ P1: {p1Name.ToUpper()} ]" : $"[ P1: {p1Name.ToUpper()} ]";
                _tabP1Text.color = !IsSelectingP2 ? new Color(0f, 0.90f, 1f, 1f) : new Color(0.6f, 0.65f, 0.75f, 1f);
            }
            if (_tabP2Text != null)
            {
                _tabP2Text.text = IsSelectingP2 ? $"▶ [ P2: {p2Name.ToUpper()} ]" : $"[ P2: {p2Name.ToUpper()} ]";
                _tabP2Text.color = IsSelectingP2 ? new Color(1f, 0.22f, 0.25f, 1f) : new Color(0.6f, 0.65f, 0.75f, 1f);
            }

            if (_tabP1Border != null)
            {
                _tabP1Border.color = !IsSelectingP2 ? new Color(0f, 0.90f, 1f, 1f) : new Color(1f, 1f, 1f, 0.1f);
            }
            if (_tabP2Border != null)
            {
                _tabP2Border.color = IsSelectingP2 ? new Color(1f, 0.22f, 0.25f, 1f) : new Color(1f, 1f, 1f, 0.1f);
            }

            if (_deployButtonText != null)
            {
                _deployButtonText.text = "LOCK IN OPERATIVE ▶";
            }
        }

        private void SetupRosterButtons()
        {
            var characters = CharacterRegistry.GetAllCharacters();
            EnsureRosterButtonsRuntime(characters);

            int count = _rosterButtons != null ? _rosterButtons.Length : 0;
            for (int i = 0; i < count; i++)
            {
                int index = i;
                if (_rosterButtons[i] != null)
                {
                    _rosterButtons[i].onClick.RemoveAllListeners();
                    _rosterButtons[i].onClick.AddListener(() => SelectFighter(index));
                }
                if (_rosterNames != null && i < _rosterNames.Length && _rosterNames[i] != null)
                {
                    _rosterNames[i].text = (i < characters.Count) ? characters[i].displayName.ToUpper() : $"FIGHTER {i + 1}";
                }
            }
            UpdateSelectionTabs();
        }

        private void EnsureRosterButtonsRuntime(System.Collections.Generic.IReadOnlyList<CharacterDefinition> characters)
        {
            if (characters == null || characters.Count == 0) return;

            int targetCount = characters.Count;
            if (_rosterButtons != null && _rosterButtons.Length == targetCount && _rosterButtons.Length > 0 && _rosterButtons[targetCount - 1] != null)
            {
                return; // Already configured with matching slots
            }

            // Purge legacy placeholders
            string[] legacyNames = new[] { "Card_MIRA", "Card_TITAN", "Card_REN", "Card_YUNA", "DemonLordNarratorWidget" };
            foreach (var leg in legacyNames)
            {
                Transform legT = transform.Find(leg);
                if (legT != null) Destroy(legT.gameObject);
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            var btnList = new System.Collections.Generic.List<Button>();
            var hlList = new System.Collections.Generic.List<Image>();
            var nameList = new System.Collections.Generic.List<Text>();

            int cols = 7;
            float cardW = 122f;
            float cardH = 220f;
            float spacingX = 14f;
            float totalW = (cols * cardW) + ((cols - 1) * spacingX);
            float startX = 360f - (totalW * 0.5f) + (cardW * 0.5f);

            for (int i = 0; i < targetCount; i++)
            {
                string charName = characters[i].displayName;
                string cardName = $"Card_{charName.Replace(" ", "_")}";
                Transform existingCard = transform.Find(cardName) ?? transform.Find($"Card_{characters[i].id}");

                int col = i % cols;
                int row = i / cols;
                float posX = startX + (col * (cardW + spacingX));
                float posY = (row == 0) ? 140f : -100f;

                GameObject cardGo;
                if (existingCard != null)
                {
                    cardGo = existingCard.gameObject;
                    RectTransform crt = cardGo.GetComponent<RectTransform>();
                    crt.sizeDelta = new Vector2(cardW, cardH);
                    crt.anchoredPosition = new Vector2(posX, posY);
                }
                else
                {
                    cardGo = new GameObject(cardName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                    cardGo.transform.SetParent(transform, false);
                    RectTransform crt = cardGo.GetComponent<RectTransform>();
                    crt.sizeDelta = new Vector2(cardW, cardH);
                    crt.anchoredPosition = new Vector2(posX, posY);
                    Image cImg = cardGo.GetComponent<Image>();
                    cImg.color = new Color(0.10f, 0.13f, 0.19f, 0.95f);
                }

                Button btn = cardGo.GetComponent<Button>();
                if (btn == null) btn = cardGo.AddComponent<Button>();
                btnList.Add(btn);

                // 1. Character Mugshot Portrait (512x512 C12 90s Anime Style)
                Transform existingPortrait = cardGo.transform.Find("Portrait_Mugshot");
                GameObject portraitGo;
                if (existingPortrait != null)
                {
                    portraitGo = existingPortrait.gameObject;
                }
                else
                {
                    portraitGo = new GameObject("Portrait_Mugshot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    portraitGo.transform.SetParent(cardGo.transform, false);
                }
                RectTransform prt = portraitGo.GetComponent<RectTransform>();
                prt.anchorMin = new Vector2(0.06f, 0.22f);
                prt.anchorMax = new Vector2(0.94f, 0.94f);
                prt.offsetMin = Vector2.zero;
                prt.offsetMax = Vector2.zero;
                Image pImg = portraitGo.GetComponent<Image>();
                pImg.preserveAspect = true;
                pImg.raycastTarget = false;
                if (characters[i].portraitSprite != null)
                {
                    pImg.sprite = characters[i].portraitSprite;
                    pImg.color = Color.white;
                }
                else
                {
                    Sprite mug = Resources.Load<Sprite>($"UI/Portraits/Mugshots/Mugshot_{characters[i].id}_{characters[i].displayName}");
                    if (mug != null)
                    {
                        pImg.sprite = mug;
                        pImg.color = Color.white;
                    }
                }

                // 2. Ornate Fantasy Shield Display Frame (Shields - 40 Fantasy Icons [512x512 HD])
                Transform existingShield = cardGo.transform.Find("Display_ShieldFrame");
                GameObject shieldGo;
                if (existingShield != null)
                {
                    shieldGo = existingShield.gameObject;
                }
                else
                {
                    shieldGo = new GameObject("Display_ShieldFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    shieldGo.transform.SetParent(cardGo.transform, false);
                }
                RectTransform srt = shieldGo.GetComponent<RectTransform>();
                srt.anchorMin = Vector2.zero;
                srt.anchorMax = Vector2.one;
                srt.offsetMin = new Vector2(-4f, -4f);
                srt.offsetMax = new Vector2(4f, 4f);
                Image sImg = shieldGo.GetComponent<Image>();
                sImg.raycastTarget = false;
                Sprite frameSpr = Resources.Load<Sprite>("UI/Frames/Frame_FantasyShield_GoldCrest");
                if (frameSpr != null)
                {
                    sImg.sprite = frameSpr;
                    sImg.color = Color.white;
                }

                // 3. Highlight border
                Transform existingBorder = cardGo.transform.Find("HighlightBorder");
                GameObject borderGo;
                if (existingBorder != null)
                {
                    borderGo = existingBorder.gameObject;
                }
                else
                {
                    borderGo = new GameObject("HighlightBorder", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    borderGo.transform.SetParent(cardGo.transform, false);
                    RectTransform brt = borderGo.GetComponent<RectTransform>();
                    brt.anchorMin = Vector2.zero;
                    brt.anchorMax = Vector2.one;
                    brt.sizeDelta = new Vector2(6f, 6f);
                }
                Image borderImg = borderGo.GetComponent<Image>();
                if (borderImg == null) borderImg = borderGo.AddComponent<Image>();
                borderImg.color = new Color(1f, 1f, 1f, 0.08f);
                hlList.Add(borderImg);

                // Card Name Text
                Transform existingTxt = cardGo.transform.Find("Text_Name") ?? cardGo.transform.Find($"Label_{charName}");
                Text nameTxt;
                if (existingTxt != null)
                {
                    nameTxt = existingTxt.GetComponent<Text>();
                }
                else
                {
                    GameObject txtGo = new GameObject("Text_Name", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                    txtGo.transform.SetParent(cardGo.transform, false);
                    RectTransform trt = txtGo.GetComponent<RectTransform>();
                    trt.anchoredPosition = new Vector2(0f, -44f);
                    trt.sizeDelta = new Vector2(cardW - 10f, 26f);
                    nameTxt = txtGo.GetComponent<Text>();
                    nameTxt.font = font;
                    nameTxt.fontSize = 15;
                    nameTxt.fontStyle = FontStyle.Bold;
                    nameTxt.alignment = TextAnchor.MiddleCenter;
                    nameTxt.color = Color.white;
                }
                nameTxt.text = charName.ToUpper();
                nameList.Add(nameTxt);
            }

            _rosterButtons = btnList.ToArray();
            _rosterHighlights = hlList.ToArray();
            _rosterNames = nameList.ToArray();
        }

        public void OpenForMode(string mode)
        {
            ActiveGameMode = mode;
            IsSelectingP2 = false;
            gameObject.SetActive(true);
            SelectFighter(SelectedP1Index);
        }

        public void SelectFighter0() => SelectFighter(0);
        public void SelectFighter1() => SelectFighter(1);
        public void SelectFighter2() => SelectFighter(2);
        public void SelectFighter3() => SelectFighter(3);
        public void SelectFighter4() => SelectFighter(4);
        public void SelectFighter5() => SelectFighter(5);

        public void SelectFighter(int index)
        {
            var characters = CharacterRegistry.GetAllCharacters();
            if (characters == null || characters.Count == 0) return;

            int clampedIndex = Mathf.Clamp(index, 0, characters.Count - 1);
            CharacterDefinition pickedDef = characters[clampedIndex];
            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();

            if (IsSelectingP2)
            {
                SelectedP2Index = clampedIndex;
                SelectedP2Definition = pickedDef;
                if (ActiveGameMode == "LocalVersus")
                {
                    IsSelectingP2 = false; // Alternate back to P1 after choosing P2
                }
            }
            else
            {
                SelectedP1Index = clampedIndex;
                SelectedP1Definition = pickedDef;

                if (ActiveGameMode == "Arcade")
                {
                    SelectedP2Definition = CharacterRegistry.GetCharacterById(LaunchServices.Session.Player2Id);
                    if (SelectedP2Definition == null && characters.Count > 1)
                        SelectedP2Definition = characters[1];
                }
                else if (ActiveGameMode == "LocalVersus")
                {
                    IsSelectingP2 = true; // Guide player to select Player 2 next
                }
                else if (ActiveGameMode == "Training")
                {
                    if (SelectedP2Definition == null)
                    {
                        int p2Idx = (SelectedP1Index == 1) ? 0 : 1; // Default sparring partner
                        if (p2Idx >= characters.Count) p2Idx = 0;
                        SelectedP2Index = p2Idx;
                        SelectedP2Definition = characters[SelectedP2Index];
                    }
                }
                else
                {
                    if (SelectedP2Definition == null)
                        SelectedP2Definition = characters.Count > 1 ? characters[1] : SelectedP1Definition;
                }
            }

            // Update Highlights & Badges across all roster cards
            UpdateRosterHighlights();

            // Display Details for the currently selected fighter
            CharacterDefinition activeDisplayDef = IsSelectingP2 ? SelectedP2Definition : SelectedP1Definition;
            DisplayActiveDetails(activeDisplayDef ?? pickedDef);

            UpdateSelectionTabs();
        }

        private void UpdateRosterHighlights()
        {
            int count = _rosterHighlights != null ? _rosterHighlights.Length : 0;
            for (int i = 0; i < count; i++)
            {
                bool isP1 = (i == SelectedP1Index);
                bool isP2 = (i == SelectedP2Index);

                if (_rosterHighlights[i] != null)
                {
                    _rosterHighlights[i].gameObject.SetActive(true);
                    if (isP1 && isP2)
                    {
                        _rosterHighlights[i].color = new Color(0.74f, 0f, 1f, 1f); // Purple for mirror match
                    }
                    else if (isP1)
                    {
                        _rosterHighlights[i].color = new Color(0f, 0.90f, 1f, 1f); // Radianite Cyan for P1
                    }
                    else if (isP2)
                    {
                        _rosterHighlights[i].color = new Color(1f, 0.16f, 0.33f, 1f); // Crimson for P2
                    }
                    else
                    {
                        _rosterHighlights[i].color = new Color(1f, 1f, 1f, 0.08f); // Inactive dark border
                    }
                }

                if (_rosterP1Badges != null && i < _rosterP1Badges.Length && _rosterP1Badges[i] != null)
                {
                    _rosterP1Badges[i].gameObject.SetActive(isP1);
                }

                if (_rosterP2Badges != null && i < _rosterP2Badges.Length && _rosterP2Badges[i] != null)
                {
                    _rosterP2Badges[i].gameObject.SetActive(isP2);
                }
            }
        }

        private void DisplayActiveDetails(CharacterDefinition charDef)
        {
            if (charDef == null) return;

            string playerTag = IsSelectingP2 ? "[ P2 SELECTION ]" : "[ P1 SELECTION ]";
            Color tagColor = IsSelectingP2 ? new Color(1f, 0.22f, 0.25f, 1f) : new Color(0f, 0.90f, 1f, 1f);

            if (_activeSelectionBadgeText != null)
            {
                _activeSelectionBadgeText.text = playerTag;
                _activeSelectionBadgeText.color = tagColor;
            }

            if (_detailNameText != null)
            {
                _detailNameText.text = charDef.displayName.ToUpper();
            }

            if (_detailTitleText != null)
            {
                _detailTitleText.text = charDef.title.ToUpper();
            }

            if (_detailArchetypeText != null)
            {
                _detailArchetypeText.text = $"ARCHETYPE: {charDef.archetype.ToUpper()}";
            }

            if (_detailDescriptionText != null)
            {
                _detailDescriptionText.text = charDef.description;
            }

            // Stat Bars
            if (_speedBarFill != null) _speedBarFill.fillAmount = charDef.speed / 100f;
            if (_powerBarFill != null) _powerBarFill.fillAmount = charDef.power / 100f;
            if (_reachBarFill != null) _reachBarFill.fillAmount = charDef.reach / 100f;
            if (_defenseBarFill != null) _defenseBarFill.fillAmount = charDef.defense / 100f;

            // Stat Numeric Values
            if (_speedValueText != null) _speedValueText.text = $"{charDef.speed}%";
            if (_powerValueText != null) _powerValueText.text = $"{charDef.power}%";
            if (_reachValueText != null) _reachValueText.text = $"{charDef.reach}%";
            if (_defenseValueText != null) _defenseValueText.text = $"{charDef.defense}%";

            // Domain Expansion Info
            if (_domainNameText != null) _domainNameText.text = $"⚡ DOMAIN: {charDef.domainName.ToUpper()}";
            if (_domainDescText != null) _domainDescText.text = charDef.domainDescription;
        }

        public void OnClickDeploy()
        {
            var characters = CharacterRegistry.GetAllCharacters();
            CharacterDefinition p1Def = SelectedP1Definition ?? (characters.Count > 0 ? characters[0] : null);
            CharacterDefinition p2Def = SelectedP2Definition ?? (characters.Count > 1 ? characters[1] : p1Def);

            Debug.Log($"<color=#00FF66>[CharacterSelectUI] Selected match: {p1Def?.displayName} ({p1Def?.id}) vs {p2Def?.displayName} ({p2Def?.id}) in Mode: {ActiveGameMode}</color>");

            // Commit to session
            if (p1Def != null && p2Def != null)
            {
                LaunchServices.Session.BeginMatch(p1Def.id, p2Def.id, LaunchServices.Session.StageId);
            }

            FighterProfile p1Prof = p1Def != null ? p1Def.ToFighterProfile() : FighterRosterCatalog.Roster[0];
            FighterProfile p2Prof = p2Def != null ? p2Def.ToFighterProfile() : FighterRosterCatalog.Roster[1];
            OnFightersDeployed?.Invoke(p1Prof, p2Prof, ActiveGameMode);

            // Transition to Stage Select
            if (BloodTechUIManager.Instance != null)
            {
                BloodTechUIManager.Instance.TransitionToStageSelect();
            }
        }

        public void OnClickBack()
        {
            if (BloodTechUIManager.Instance != null)
            {
                BloodTechUIManager.Instance.TransitionToModeSelect();
            }
        }
    }
}
