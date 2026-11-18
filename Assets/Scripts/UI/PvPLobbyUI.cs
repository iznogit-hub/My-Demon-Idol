using System;
using UnityEngine;
using UnityEngine.UI;
using DemonIdol.Network;

namespace DemonIdol.UI
{
    /// <summary>
    /// UI Controller for the 1v1 Quantum PvP Matchmaking Modal:
    /// Quick Play search, Private Room Code creation/joining, practice loopback simulation,
    /// live ping indicators, and immediate Local Versus (Offline 1v1) transition.
    /// Fully wired with Photon Quantum 3.0 deterministic rollback networking.
    /// </summary>
    public class PvPLobbyUI : MonoBehaviour
    {
        public const string ONLINE_PREVIEW_STATUS = "Photon Quantum 3.0 Rollback Ready";
        public const string ONLINE_LOCKED_MESSAGE = 
            "Photon Quantum 3.0 Rollback Ready\nSelect Quick Match, Private Room, or Practice Loopback.";

        [Header("Panels")]
        [SerializeField] private GameObject _mainLobbyPanel;
        [SerializeField] private GameObject _searchingPanel;
        [SerializeField] private GameObject _matchFoundPanel;
        [SerializeField] private GameObject _networkWarningPanel;

        [Header("Search UI Elements")]
        [SerializeField] private Text _statusText;
        [SerializeField] private Text _pingText;
        [SerializeField] private Text _opponentNameText;
        [SerializeField] private InputField _roomCodeInputField;

        [Header("Network Gatekeeper Warning Modal")]
        [SerializeField] private Text _warningTitleText;
        [SerializeField] private Text _warningMessageText;
        [SerializeField] private Text _warningMetricsText;
        [SerializeField] private Button _btnRetestConnection;
        [SerializeField] private Button _btnGoOfflineDojo;
        [SerializeField] private Button _btnCloseWarning;

        [Header("Action Buttons")]
        [SerializeField] private Button _btnQuickMatch;
        [SerializeField] private Button _btnCreatePrivate;
        [SerializeField] private Button _btnJoinPrivate;
        [SerializeField] private Button _btnCopyRoomCode;
        [SerializeField] private Button _btnGenerateCode;
        [SerializeField] private Button _btnLoopbackMatch;
        [SerializeField] private Button _btnCancelSearch;
        [SerializeField] private Button _btnLocalVersus;
        [SerializeField] private Button _btnClose;

        // Public accessors for test harnesses and inspection
        public Button BtnQuickMatch => _btnQuickMatch;
        public Button BtnCreatePrivate => _btnCreatePrivate;
        public Button BtnJoinPrivate => _btnJoinPrivate;
        public Button BtnCopyRoomCode => _btnCopyRoomCode;
        public Button BtnGenerateCode => _btnGenerateCode;
        public Button BtnLoopbackMatch => _btnLoopbackMatch;
        public Button BtnCancelSearch => _btnCancelSearch;
        public Button BtnLocalVersus => _btnLocalVersus;
        public Button BtnClose => _btnClose;
        public Button BtnRetestConnection => _btnRetestConnection;
        public Button BtnGoOfflineDojo => _btnGoOfflineDojo;
        public Button BtnCloseWarning => _btnCloseWarning;
        public Text StatusText => _statusText;
        public Text WarningTitleText => _warningTitleText;
        public Text WarningMessageText => _warningMessageText;
        public Text WarningMetricsText => _warningMetricsText;
        public InputField RoomCodeInputField => _roomCodeInputField;
        public GameObject MainLobbyPanel => _mainLobbyPanel;
        public GameObject SearchingPanel => _searchingPanel;
        public GameObject MatchFoundPanel => _matchFoundPanel;
        public GameObject NetworkWarningPanel => _networkWarningPanel;

        private void Awake()
        {
            EnsureLobbyUI();
        }

        private void OnEnable()
        {
            EnsureLobbyUI();
            if (_statusText != null)
            {
                _statusText.text = "Photon Quantum 3.0 Deterministic Rollback Ready • Select Match Mode";
            }
        }

        private void Start()
        {
            if (QuantumPvPManager.Instance != null)
            {
                QuantumPvPManager.Instance.OnStateChanged += HandleStateChanged;
                QuantumPvPManager.Instance.OnMatchFound += HandleMatchFound;
                QuantumPvPManager.Instance.OnCountdownTick += HandleCountdownTick;
                QuantumPvPManager.Instance.OnError += HandleError;
            }

            ShowPanel(_mainLobbyPanel);

            if (_statusText != null)
            {
                _statusText.text = "Photon Quantum 3.0 Deterministic Rollback Ready • Select Match Mode";
            }
        }

        private void OnDestroy()
        {
            if (QuantumPvPManager.Instance != null)
            {
                QuantumPvPManager.Instance.OnStateChanged -= HandleStateChanged;
                QuantumPvPManager.Instance.OnMatchFound -= HandleMatchFound;
                QuantumPvPManager.Instance.OnCountdownTick -= HandleCountdownTick;
                QuantumPvPManager.Instance.OnError -= HandleError;
            }
        }

        private void HandleCountdownTick(int secondsRemaining)
        {
            if (_statusText != null)
            {
                _statusText.text = $"DUEL CONFIRMED! LAUNCHING QUANTUM ROLLBACK IN {secondsRemaining}...";
            }
        }

        public void EnsureLobbyUI()
        {
            BloodlinesUIAtlas.EnsureInstance();
            var atlas = BloodlinesUIAtlas.Instance;

            Font font = atlas != null ? atlas.BloodlinesFont : null;
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // 1. Ensure Main Lobby Panel
            if (_mainLobbyPanel == null)
            {
                _mainLobbyPanel = FindChildRecursive(gameObject, "PvPBox", "MainLobbyPanel", "Panel_MainLobby");
                if (_mainLobbyPanel == null)
                {
                    _mainLobbyPanel = CreatePanelObject("MainLobbyPanel", transform, new Vector2(750f, 520f), new Color(0.08f, 0.10f, 0.14f, 0.95f));
                }
            }

            // 2. Ensure Searching Panel
            if (_searchingPanel == null)
            {
                _searchingPanel = FindChildRecursive(gameObject, "Panel_Searching", "SearchingPanel");
                if (_searchingPanel == null)
                {
                    _searchingPanel = CreatePanelObject("Panel_Searching", transform, new Vector2(700f, 440f), new Color(0.06f, 0.08f, 0.12f, 0.98f));
                    CreateTextChild(_searchingPanel.transform, "Title", "⚔️ SEARCHING QUANTUM CLOUD ⚔️", new Vector2(0f, 120f), new Vector2(600f, 40f), SumiStyleTokens.ToriiRed, 26, font);
                    CreateTextChild(_searchingPanel.transform, "SubTitle", "Matching against rivals with lowest latency...", new Vector2(0f, 60f), new Vector2(600f, 30f), SumiStyleTokens.PaperWhite, 16, font);
                }
            }

            // 3. Ensure Match Found Panel
            if (_matchFoundPanel == null)
            {
                _matchFoundPanel = FindChildRecursive(gameObject, "Panel_MatchFound", "MatchFoundPanel");
                if (_matchFoundPanel == null)
                {
                    _matchFoundPanel = CreatePanelObject("Panel_MatchFound", transform, new Vector2(700f, 440f), new Color(0.06f, 0.08f, 0.12f, 0.98f));
                    CreateTextChild(_matchFoundPanel.transform, "Title", "⚔️ RIVAL DETECTED! ⚔️", new Vector2(0f, 130f), new Vector2(600f, 40f), SumiStyleTokens.ParryGold, 28, font);
                    CreateTextChild(_matchFoundPanel.transform, "SyncLabel", "SYNCHRONIZING DETERMINISTIC TIMELINE (TICK 0)...", new Vector2(0f, -60f), new Vector2(600f, 30f), SumiStyleTokens.PaperWhite, 15, font);
                }
            }

            // 4. Ensure Text Elements
            if (_statusText == null)
            {
                _statusText = FindComponentRecursive<Text>("Text_Status", "StatusText", "Label_Status");
                if (_statusText == null && _mainLobbyPanel != null)
                {
                    _statusText = CreateTextChild(_mainLobbyPanel.transform, "Text_Status", $"{ONLINE_PREVIEW_STATUS} (Offline route available below)", new Vector2(0f, 130f), new Vector2(650f, 40f), SumiStyleTokens.PaperWhite, 15, font);
                }
            }

            if (_opponentNameText == null)
            {
                _opponentNameText = FindComponentRecursive<Text>("Text_OpponentName", "OpponentNameText", "Label_Opponent");
                if (_opponentNameText == null && _matchFoundPanel != null)
                {
                    _opponentNameText = CreateTextChild(_matchFoundPanel.transform, "Text_OpponentName", "OPPONENT: SEARCHING...", new Vector2(0f, 50f), new Vector2(600f, 40f), Color.white, 22, font);
                }
            }

            if (_pingText == null)
            {
                _pingText = FindComponentRecursive<Text>("Text_Ping", "PingText", "Label_Ping");
                if (_pingText == null && _matchFoundPanel != null)
                {
                    _pingText = CreateTextChild(_matchFoundPanel.transform, "Text_Ping", "PING: -- ms • ROLLBACK 60HZ", new Vector2(0f, 0f), new Vector2(600f, 30f), new Color(0.2f, 0.9f, 0.4f, 1f), 18, font);
                }
            }

            // 5. Ensure Input Field for Room Code
            if (_roomCodeInputField == null)
            {
                _roomCodeInputField = FindComponentRecursive<InputField>("InputField_RoomCode", "RoomCodeInputField", "Input_RoomCode");
                if (_roomCodeInputField == null && _mainLobbyPanel != null)
                {
                    _roomCodeInputField = CreateInputFieldObject(_mainLobbyPanel.transform, "InputField_RoomCode", "ENTER ROOM CODE", new Vector2(-110f, -40f), new Vector2(210f, 45f), font);
                }
            }

            // 6. Ensure Action Buttons in Main Panel
            if (_btnQuickMatch == null)
            {
                _btnQuickMatch = FindComponentRecursive<Button>("Btn_QuickMatch", "Button_QuickMatch");
                if (_btnQuickMatch == null && _mainLobbyPanel != null)
                {
                    _btnQuickMatch = CreateButtonObject(_mainLobbyPanel.transform, "Btn_QuickMatch", "FIND 1v1 RANKED MATCH", new Vector2(0f, 70f), new Vector2(440f, 50f), SumiStyleTokens.ToriiRed, font);
                }
            }

            if (_btnCreatePrivate == null)
            {
                _btnCreatePrivate = FindComponentRecursive<Button>("Btn_CreatePrivate", "Btn_CreateRoom", "Button_CreatePrivate");
                if (_btnCreatePrivate == null && _mainLobbyPanel != null)
                {
                    _btnCreatePrivate = CreateButtonObject(_mainLobbyPanel.transform, "Btn_CreatePrivate", "CREATE PRIVATE ROOM (CODE)", new Vector2(0f, 15f), new Vector2(440f, 45f), new Color(0.18f, 0.22f, 0.30f, 1f), font);
                }
            }

            if (_btnJoinPrivate == null)
            {
                _btnJoinPrivate = FindComponentRecursive<Button>("Btn_JoinPrivate", "Btn_JoinRoom", "Button_JoinRoom");
                if (_btnJoinPrivate == null && _mainLobbyPanel != null)
                {
                    _btnJoinPrivate = CreateButtonObject(_mainLobbyPanel.transform, "Btn_JoinPrivate", "JOIN ROOM", new Vector2(140f, -40f), new Vector2(160f, 45f), new Color(0.22f, 0.28f, 0.38f, 1f), font);
                }
            }

            if (_btnCopyRoomCode == null)
            {
                _btnCopyRoomCode = FindComponentRecursive<Button>("Btn_CopyRoomCode", "Btn_CopyCode");
                if (_btnCopyRoomCode == null && _mainLobbyPanel != null)
                {
                    _btnCopyRoomCode = CreateButtonObject(_mainLobbyPanel.transform, "Btn_CopyRoomCode", "📋 COPY", new Vector2(-150f, -40f), new Vector2(90f, 45f), new Color(0.25f, 0.35f, 0.45f, 1f), font);
                }
            }

            if (_btnGenerateCode == null)
            {
                _btnGenerateCode = FindComponentRecursive<Button>("Btn_GenerateCode", "Btn_NewCode");
                if (_btnGenerateCode == null && _mainLobbyPanel != null)
                {
                    _btnGenerateCode = CreateButtonObject(_mainLobbyPanel.transform, "Btn_GenerateCode", "🎲 NEW", new Vector2(-60f, -40f), new Vector2(80f, 45f), new Color(0.20f, 0.25f, 0.35f, 1f), font);
                }
            }

            if (_btnLoopbackMatch == null)
            {
                _btnLoopbackMatch = FindComponentRecursive<Button>("Btn_LoopbackMatch", "Btn_SimulatedMatch", "Btn_DevPreview");
                if (_btnLoopbackMatch == null && _mainLobbyPanel != null)
                {
                    _btnLoopbackMatch = CreateButtonObject(_mainLobbyPanel.transform, "Btn_LoopbackMatch", "PRACTICE ROLLBACK (OFFLINE LOOPBACK)", new Vector2(0f, -95f), new Vector2(440f, 45f), new Color(0.25f, 0.32f, 0.42f, 1f), font);
                }
            }

            if (_btnLocalVersus == null)
            {
                _btnLocalVersus = FindComponentRecursive<Button>("Btn_LocalVersus", "Button_LocalVersus", "Btn_OfflineVersus");
                if (_btnLocalVersus == null && _mainLobbyPanel != null)
                {
                    _btnLocalVersus = CreateButtonObject(_mainLobbyPanel.transform, "Btn_LocalVersus", "PLAY LOCAL VERSUS (OFFLINE)", new Vector2(0f, -150f), new Vector2(440f, 45f), SumiStyleTokens.ToriiRed, font);
                }
            }

            if (_btnClose == null)
            {
                _btnClose = FindComponentRecursive<Button>("Btn_Close", "Btn_ClosePvP", "Button_Close", "Btn_Back");
                if (_btnClose == null && _mainLobbyPanel != null)
                {
                    _btnClose = CreateButtonObject(_mainLobbyPanel.transform, "Btn_ClosePvP", "BACK TO HUB", new Vector2(0f, -205f), new Vector2(260f, 40f), new Color(0.3f, 0.3f, 0.35f, 1f), font);
                }
            }

            // 7. Ensure Cancel Button in Searching Panel
            if (_btnCancelSearch == null && _searchingPanel != null)
            {
                _btnCancelSearch = FindComponentRecursive<Button>("Btn_CancelSearch", "Button_CancelSearch");
                if (_btnCancelSearch == null)
                {
                    _btnCancelSearch = CreateButtonObject(_searchingPanel.transform, "Btn_CancelSearch", "CANCEL MATCHMAKING", new Vector2(0f, -130f), new Vector2(280f, 45f), new Color(0.5f, 0.2f, 0.2f, 1f), font);
                }
            }

            // 8. Ensure Network Gatekeeper Warning Modal
            if (_networkWarningPanel == null)
            {
                _networkWarningPanel = FindChildRecursive(gameObject, "Panel_NetworkWarning", "NetworkWarningPanel", "Modal_NetworkGateWarning");
                if (_networkWarningPanel == null)
                {
                    _networkWarningPanel = CreatePanelObject("Modal_NetworkGateWarning", transform, new Vector2(740f, 480f), new Color(0.08f, 0.06f, 0.08f, 0.98f));
                    _warningTitleText = CreateTextChild(_networkWarningPanel.transform, "Title_GateWarning", "⚠️ NETWORK STABILITY WARNING", new Vector2(0f, 160f), new Vector2(680f, 40f), SumiStyleTokens.ToriiRed, 20, font);
                    _warningMessageText = CreateTextChild(_networkWarningPanel.transform, "Message_GateWarning", "Photon Quantum 3.0 deterministic rollback requires low-latency 5G NR or Wi-Fi.\nOffline Dojo and Story modes remain accessible.", new Vector2(0f, 70f), new Vector2(660f, 90f), SumiStyleTokens.PaperWhite, 14, font);
                    _warningMetricsText = CreateTextChild(_networkWarningPanel.transform, "Metrics_GateWarning", "PING: -- ms | JITTER: -- ms | NETWORK: Unknown", new Vector2(0f, -10f), new Vector2(660f, 40f), SumiStyleTokens.ParryGold, 15, font);

                    _btnRetestConnection = CreateButtonObject(_networkWarningPanel.transform, "Btn_RetestConnection", "⚡ RE-TEST CONNECTION", new Vector2(0f, -70f), new Vector2(400f, 45f), SumiStyleTokens.ToriiRed, font);
                    _btnGoOfflineDojo = CreateButtonObject(_networkWarningPanel.transform, "Btn_GoOfflineDojo", "🥋 ENTER OFFLINE DOJO (UNRESTRICTED)", new Vector2(0f, -125f), new Vector2(400f, 45f), new Color(0.2f, 0.35f, 0.5f, 1f), font);
                    _btnCloseWarning = CreateButtonObject(_networkWarningPanel.transform, "Btn_CloseWarning", "RETURN TO LOBBY", new Vector2(0f, -180f), new Vector2(260f, 38f), new Color(0.3f, 0.3f, 0.35f, 1f), font);
                }
            }

            if (_warningTitleText == null && _networkWarningPanel != null)
                _warningTitleText = FindComponentRecursive<Text>("Title_GateWarning", "WarningTitleText");
            if (_warningMessageText == null && _networkWarningPanel != null)
                _warningMessageText = FindComponentRecursive<Text>("Message_GateWarning", "WarningMessageText");
            if (_warningMetricsText == null && _networkWarningPanel != null)
                _warningMetricsText = FindComponentRecursive<Text>("Metrics_GateWarning", "WarningMetricsText");
            if (_btnRetestConnection == null && _networkWarningPanel != null)
                _btnRetestConnection = FindComponentRecursive<Button>("Btn_RetestConnection");
            if (_btnGoOfflineDojo == null && _networkWarningPanel != null)
                _btnGoOfflineDojo = FindComponentRecursive<Button>("Btn_GoOfflineDojo");
            if (_btnCloseWarning == null && _networkWarningPanel != null)
                _btnCloseWarning = FindComponentRecursive<Button>("Btn_CloseWarning");

            // 9. Bind Click Listeners cleanly
            WireButton(_btnQuickMatch, OnClickQuickMatch);
            WireButton(_btnCreatePrivate, OnClickCreatePrivateRoom);
            WireButton(_btnJoinPrivate, OnClickJoinPrivateRoom);
            WireButton(_btnCopyRoomCode, OnClickCopyRoomCode);
            WireButton(_btnGenerateCode, OnClickGenerateCode);
            WireButton(_btnLoopbackMatch, OnClickLoopbackMatch);
            WireButton(_btnLocalVersus, OnClickLocalVersus);
            WireButton(_btnClose, OnClickClose);
            WireButton(_btnCancelSearch, OnClickCancelSearch);
            WireButton(_btnRetestConnection, OnClickRetestConnection);
            WireButton(_btnGoOfflineDojo, OnClickGoOfflineDojo);
            WireButton(_btnCloseWarning, OnClickCloseWarning);

            // Default visible state
            ShowPanel(_mainLobbyPanel);
        }

        private void WireButton(Button btn, UnityEngine.Events.UnityAction action)
        {
            if (btn != null)
            {
                btn.onClick.RemoveListener(action);
                btn.onClick.AddListener(action);
            }
        }

        private T FindComponentRecursive<T>(params string[] names) where T : Component
        {
            T[] all = GetComponentsInChildren<T>(true);
            foreach (var item in all)
            {
                foreach (var name in names)
                {
                    if (string.Equals(item.gameObject.name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        return item;
                    }
                }
            }
            return null;
        }

        private GameObject FindChildRecursive(GameObject root, params string[] names)
        {
            if (root == null) return null;
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in transforms)
            {
                foreach (var name in names)
                {
                    if (string.Equals(t.gameObject.name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        return t.gameObject;
                    }
                }
            }
            return null;
        }

        private GameObject CreatePanelObject(string name, Transform parent, Vector2 size, Color color)
        {
            var atlas = BloodlinesUIAtlas.Instance;
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
            Image img = obj.GetComponent<Image>();
            if (atlas != null) atlas.StylePanel(img, false);
            else img.color = color;
            return obj;
        }

        private Text CreateTextChild(Transform parent, string name, string content, Vector2 pos, Vector2 size, Color col, int fontSize, Font font)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            Text txt = obj.GetComponent<Text>();
            txt.font = font;
            txt.text = content;
            txt.fontSize = fontSize;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = col;
            return txt;
        }

        private Button CreateButtonObject(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color col, Font font)
        {
            var atlas = BloodlinesUIAtlas.Instance;
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = btnObj.GetComponent<Image>();
            Button btn = btnObj.GetComponent<Button>();
            btn.targetGraphic = img;

            bool isRed = name.Contains("Quick") || name.Contains("Local") || col == SumiStyleTokens.ToriiRed;
            if (atlas != null) atlas.StyleButton(btn, 1, isRed);
            else img.color = col;

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform trt = txtObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;
            Text txt = txtObj.GetComponent<Text>();
            txt.font = font;
            txt.text = label;
            txt.fontSize = 15;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            return btn;
        }

        private InputField CreateInputFieldObject(Transform parent, string name, string placeholder, Vector2 pos, Vector2 size, Font font)
        {
            var atlas = BloodlinesUIAtlas.Instance;
            GameObject inputObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InputField));
            inputObj.transform.SetParent(parent, false);
            RectTransform rt = inputObj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = inputObj.GetComponent<Image>();
            if (atlas != null) atlas.StyleInputField(img, false);
            else img.color = new Color(0.12f, 0.15f, 0.20f, 1f);

            InputField inputField = inputObj.GetComponent<InputField>();

            // Text
            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            txtObj.transform.SetParent(inputObj.transform, false);
            RectTransform trt = txtObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(10f, 5f);
            trt.offsetMax = new Vector2(-10f, -5f);
            Text txt = txtObj.GetComponent<Text>();
            txt.font = font;
            txt.fontSize = 15;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleLeft;

            // Placeholder
            GameObject phObj = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            phObj.transform.SetParent(inputObj.transform, false);
            RectTransform prt = phObj.GetComponent<RectTransform>();
            prt.anchorMin = Vector2.zero;
            prt.anchorMax = Vector2.one;
            prt.offsetMin = new Vector2(10f, 5f);
            prt.offsetMax = new Vector2(-10f, -5f);
            Text ph = phObj.GetComponent<Text>();
            ph.font = font;
            ph.text = placeholder;
            ph.fontSize = 14;
            ph.color = new Color(0.7f, 0.7f, 0.7f, 0.5f);
            ph.fontStyle = FontStyle.Italic;
            ph.alignment = TextAnchor.MiddleLeft;

            inputField.textComponent = txt;
            inputField.placeholder = ph;
            return inputField;
        }

        private void HandleStateChanged(QuantumPvPState state)
        {
            switch (state)
            {
                case QuantumPvPState.InLobby:
                    ShowPanel(_mainLobbyPanel);
                    break;
                case QuantumPvPState.SearchingMatch:
                    ShowPanel(_searchingPanel);
                    if (_statusText != null) _statusText.text = "Searching for rival in your rank tier (Photon Quantum Cloud)...";
                    break;
                case QuantumPvPState.MatchFound:
                    ShowPanel(_matchFoundPanel);
                    break;
                case QuantumPvPState.StartingSimulation:
                    ShowPanel(_matchFoundPanel);
                    if (_statusText != null) _statusText.text = "Starting Quantum Rollback Simulation Engine...";
                    break;
                case QuantumPvPState.InMatch:
                    gameObject.SetActive(false); // Hide lobby UI when fight starts
                    break;
                case QuantumPvPState.Error:
                    ShowPanel(_mainLobbyPanel);
                    break;
            }
        }

        private void HandleMatchFound(string opponentName, int slot)
        {
            ShowPanel(_matchFoundPanel);
            if (_opponentNameText != null) _opponentNameText.text = $"OPPONENT: {opponentName.ToUpper()} (SLOT P{slot + 1})";
            if (_pingText != null && QuantumPvPManager.Instance != null)
            {
                _pingText.text = $"PING: {QuantumPvPManager.Instance.CurrentPingMs}ms • ROLLBACK ACTIVE";
            }
        }

        private void HandleError(string message)
        {
            ShowPanel(_mainLobbyPanel);
            if (_statusText != null) _statusText.text = message;
        }

        public void ShowPanel(GameObject target)
        {
            if (_mainLobbyPanel != null) _mainLobbyPanel.SetActive(_mainLobbyPanel == target);
            if (_searchingPanel != null) _searchingPanel.SetActive(_searchingPanel == target);
            if (_matchFoundPanel != null) _matchFoundPanel.SetActive(_matchFoundPanel == target);
            if (_networkWarningPanel != null) _networkWarningPanel.SetActive(_networkWarningPanel == target);
        }

        public void ShowNetworkGateWarning(GateEvaluation eval)
        {
            ShowPanel(_networkWarningPanel);
            if (_warningTitleText != null)
            {
                _warningTitleText.text = eval.diagnosticTitle;
            }
            if (_warningMessageText != null)
            {
                _warningMessageText.text = eval.diagnosticMessage;
            }
            if (_warningMetricsText != null)
            {
                string connType = eval.reachability switch
                {
                    NetworkReachability.ReachableViaLocalAreaNetwork => "Wi-Fi (LAN)",
                    NetworkReachability.ReachableViaCarrierDataNetwork => eval.cellularGeneration switch
                    {
                        CellularGeneration.FiveG_NR => "5G NR Cellular",
                        CellularGeneration.FourG_LTE => "4G LTE Cellular",
                        CellularGeneration.ThreeG => "3G Cellular",
                        CellularGeneration.TwoG => "2G Cellular",
                        _ => "Cellular"
                    },
                    _ => "Disconnected"
                };

                if (eval.probeReport.totalProbes > 0)
                {
                    _warningMetricsText.text = $"NET: {connType} | PING: {eval.probeReport.meanPingMs:F0}ms | JITTER: {eval.probeReport.jitterMs:F1}ms | LOSS: {eval.probeReport.packetLossPercent:F0}%";
                }
                else
                {
                    _warningMetricsText.text = $"NET: {connType} | ROLLBACK STATUS: RESTRICTED";
                }
            }
        }

        public async void OnClickRetestConnection()
        {
            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();
            if (_warningMessageText != null)
            {
                _warningMessageText.text = "Testing network latency and packet jitter variance against edge endpoints...";
            }
            NetworkGatekeeper.EnsureInstance();
            if (NetworkGatekeeper.Instance != null)
            {
                var eval = await NetworkGatekeeper.Instance.EvaluateCompetitiveEligibilityAsync();
                if (eval.isEligible)
                {
                    ShowPanel(_mainLobbyPanel);
                    if (_statusText != null)
                    {
                        _statusText.text = $"Connection Verified ({eval.diagnosticTitle}). Ready for Rollback.";
                    }
                }
                else
                {
                    ShowNetworkGateWarning(eval);
                }
            }
        }

        public void OnClickGoOfflineDojo()
        {
            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();
            gameObject.SetActive(false);
            if (BloodTechUIManager.Instance != null)
            {
                BloodTechUIManager.Instance.LaunchDirectTraining1vCPU();
            }
        }

        public void OnClickCloseWarning()
        {
            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();
            ShowPanel(_mainLobbyPanel);
        }

        #region Button OnClick Hooks

        public void OnClickLocalVersus()
        {
            gameObject.SetActive(false);
            if (BloodTechUIManager.Instance != null)
            {
                BloodTechUIManager.Instance.TransitionToCharacterSelect("LocalVersus");
            }
        }

        public void OnClickClose()
        {
            gameObject.SetActive(false);
            if (BloodTechUIManager.Instance != null)
            {
                BloodTechUIManager.Instance.OnClickClosePvPModal();
            }
        }

        public async void OnClickQuickMatch()
        {
            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();

            // Competitive Network Gatekeeper Check
            NetworkGatekeeper.EnsureInstance();
            if (NetworkGatekeeper.Instance != null)
            {
                var eval = await NetworkGatekeeper.Instance.EvaluateCompetitiveEligibilityAsync();
                if (!eval.isEligible)
                {
                    ShowNetworkGateWarning(eval);
                    return;
                }
            }

            if (QuantumPvPManager.Instance != null)
            {
                if (Firebase.FirebaseManager.Instance != null)
                {
                    Firebase.FirebaseManager.Instance.EnsureActiveUser((user) =>
                    {
                        ShowPanel(_searchingPanel);
                        if (_statusText != null) _statusText.text = $"Authenticating as {user.DisplayName}... Connecting to Quantum Cloud...";
                        QuantumPvPManager.Instance.StartQuickMatch1v1();
                    });
                }
                else
                {
                    ShowPanel(_searchingPanel);
                    QuantumPvPManager.Instance.StartQuickMatch1v1();
                }
            }
            else
            {
                if (_statusText != null) _statusText.text = "Quantum PvP Manager not found in scene.";
            }
        }

        public async void OnClickCreatePrivateRoom()
        {
            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();

            // Competitive Network Gatekeeper Check
            NetworkGatekeeper.EnsureInstance();
            if (NetworkGatekeeper.Instance != null)
            {
                var eval = await NetworkGatekeeper.Instance.EvaluateCompetitiveEligibilityAsync();
                if (!eval.isEligible)
                {
                    ShowNetworkGateWarning(eval);
                    return;
                }
            }

            string code = _roomCodeInputField != null && !string.IsNullOrWhiteSpace(_roomCodeInputField.text)
                ? _roomCodeInputField.text.Trim().ToUpper()
                : "MDI" + UnityEngine.Random.Range(1000, 9999);

            ShowPanel(_searchingPanel);
            if (_statusText != null) _statusText.text = $"Creating Private Room: {code}... Waiting for challenger...";
            QuantumPvPManager.Instance?.CreatePrivateRoom(code);
        }

        public async void OnClickJoinPrivateRoom()
        {
            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();
            string code = _roomCodeInputField != null ? _roomCodeInputField.text.Trim().ToUpper() : string.Empty;
            if (string.IsNullOrEmpty(code))
            {
                if (_statusText != null) _statusText.text = "Please enter a valid room code (e.g. MDI1234).";
                return;
            }

            // Competitive Network Gatekeeper Check
            NetworkGatekeeper.EnsureInstance();
            if (NetworkGatekeeper.Instance != null)
            {
                var eval = await NetworkGatekeeper.Instance.EvaluateCompetitiveEligibilityAsync();
                if (!eval.isEligible)
                {
                    ShowNetworkGateWarning(eval);
                    return;
                }
            }

            ShowPanel(_searchingPanel);
            if (_statusText != null) _statusText.text = $"Joining Room {code}... Connecting to Quantum Cloud...";
            QuantumPvPManager.Instance?.JoinPrivateRoom(code);
        }

        public void OnClickCopyRoomCode()
        {
            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();
            string code = _roomCodeInputField != null ? _roomCodeInputField.text.Trim().ToUpper() : "";
            if (string.IsNullOrEmpty(code))
            {
                code = QuantumPvPManager.GenerateRoomCode();
                if (_roomCodeInputField != null) _roomCodeInputField.text = code;
            }
            GUIUtility.systemCopyBuffer = code;
            if (_statusText != null)
            {
                _statusText.text = $"ROOM CODE <b>{code}</b> COPIED TO CLIPBOARD!";
            }
        }

        public void OnClickGenerateCode()
        {
            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();
            string code = QuantumPvPManager.GenerateRoomCode();
            if (_roomCodeInputField != null)
            {
                _roomCodeInputField.text = code;
            }
            if (_statusText != null)
            {
                _statusText.text = $"Generated Code: <b>{code}</b>. Click 'CREATE PRIVATE ROOM' to host.";
            }
        }

        public void OnClickLoopbackMatch()
        {
            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();
            if (QuantumPvPManager.Instance != null)
            {
                ShowPanel(_searchingPanel);
                QuantumPvPManager.Instance.StartSimulatedLoopbackMatch("Shadow_Shinobi");
            }
        }

        public void OnClickCancelSearch()
        {
            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();
            QuantumPvPManager.Instance?.CancelSearch();
            ShowPanel(_mainLobbyPanel);
            if (_statusText != null)
            {
                _statusText.text = "Matchmaking canceled. Choose an online or local mode.";
            }
        }

        #endregion
    }
}
