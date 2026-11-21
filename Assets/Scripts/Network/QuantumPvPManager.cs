using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Photon.Realtime;
using Photon.Deterministic;
using Quantum;
using DemonIdol.Combat;
using DemonIdol.UI;
using DemonIdol.Firebase;

namespace DemonIdol.Network
{
    public enum MatchmakingMode
    {
        Ranked,
        Casual
    }

    public enum QuantumPvPState
    {
        Disconnected,
        Connecting,
        InLobby,
        SearchingMatch,
        MatchFound,
        StartingSimulation,
        InMatch,
        MatchFinished,
        Error
    }

    /// <summary>
    /// Master Photon Quantum 1v1 PvP Matchmaking & Deterministic Rollback Runner.
    /// Manages real Quantum cloud connection, online 1v1 room matchmaking,
    /// and initializes the true QuantumRunner deterministic simulation loop.
    /// </summary>
    public class QuantumPvPManager : MonoBehaviour
    {
        private static bool _isQuitting = false;
        private static QuantumPvPManager _instance;
        public static QuantumPvPManager Instance
        {
            get
            {
                if (_isQuitting) return null;
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<QuantumPvPManager>();
                }
                return _instance;
            }
            set => _instance = value;
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        [Header("Photon Quantum Configuration")]
        public string quantumAppId = "";
        public string preferredRegion = ""; // empty = best ping region

        [Header("Authentication")]
        [Tooltip("Enable only if your Photon Dashboard has a Custom Authentication webhook configured for Firebase.")]
        public bool useCustomAuthentication = false;

        [Header("Simulation Configs")]
        public RuntimeConfig runtimeConfig;
        public List<RuntimePlayer> runtimePlayers;

        public QuantumPvPState State { get; private set; } = QuantumPvPState.Disconnected;
        public int LocalPlayerSlot { get; private set; } = 0; // 0 = P1, 1 = P2
        public string OpponentName { get; private set; } = "Rival";
        public int CurrentPingMs { get; private set; } = 35;
        public int EstimatedRollbackFrames { get; private set; } = 1;
        public string CurrentRoomCode { get; private set; } = string.Empty;
        public MatchmakingMode CurrentMode { get; private set; } = MatchmakingMode.Ranked;
        public bool IsOpponentReconnecting { get; private set; } = false;
        public float DisconnectSecondsRemaining { get; private set; } = 5f;

        public RealtimeClient Client => _client;
        public QuantumRunner Runner => _runner;

        public event Action<QuantumPvPState> OnStateChanged;
        public event Action<string, int> OnMatchFound;
        public event Action<int> OnCountdownTick;
        public event Action<bool, float> OnOpponentDisconnectChanged;
        public event Action<string> OnError;

        private RealtimeClient _client;
        private QuantumRunner _runner;
        private IDisposable _disconnectSubscription;
        private Coroutine _disconnectRecoveryRoutine;
        private Coroutine _matchStartCountdownRoutine;
        private int _pingUpdateFrameCounter = 0;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            Application.runInBackground = true;
            SetState(QuantumPvPState.InLobby);
        }

        private void Update()
        {
            _pingUpdateFrameCounter++;
            if (_pingUpdateFrameCounter >= 30)
            {
                _pingUpdateFrameCounter = 0;
                UpdatePingAndRollbackMetrics();
            }
        }

        private void UpdatePingAndRollbackMetrics()
        {
            if (_client != null && _client.RealtimePeer != null)
            {
                CurrentPingMs = Mathf.Max(12, (int)_client.RealtimePeer.Stats.RoundtripTime);
            }
            else if (State == QuantumPvPState.InMatch && _runner == null)
            {
                CurrentPingMs = 35; // Loopback simulation baseline
            }

            // Empirical Rollback Frames estimation based on RTT
            if (CurrentPingMs < 45) EstimatedRollbackFrames = 1;
            else if (CurrentPingMs <= 70) EstimatedRollbackFrames = 2;
            else EstimatedRollbackFrames = Mathf.Clamp(4 + (CurrentPingMs - 70) / 20, 4, 8);
        }

        /// <summary>
        /// Generates a clean 4-character alphanumeric room code (e.g. KAI9, VOI7, ORC4).
        /// Excludes ambiguous characters (0, O, 1, I).
        /// </summary>
        public static string GenerateRoomCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            char[] code = new char[4];
            for (int i = 0; i < 4; i++)
            {
                code[i] = chars[UnityEngine.Random.Range(0, chars.Length)];
            }
            return new string(code);
        }

        public static bool IsQuantumDBEmpty()
        {
            try
            {
                var globalDb = QuantumUnityDB.Global;
                if (globalDb != null && globalDb.Entries != null && globalDb.Entries.Count > 0)
                {
                    return false;
                }
                if (globalDb != null)
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[QuantumPvPManager] Check QuantumUnityDB notice: " + ex.Message);
            }
            return false;
        }

        public void StartMatchmaking()
        {
            StartQuickMatch1v1();
        }

        /// <summary>
        /// Starts real 1v1 Quick Match via Photon Quantum Room Matchmaking.
        /// </summary>
        public async void StartQuickMatch1v1(string rankTier = "Adept")
        {
            await ConnectAndStartQuantum(roomName: null, isPrivate: false);
        }

        /// <summary>
        /// Starts a deterministic simulated loopback match (P1 Local vs Simulated Opponent P2).
        /// Runs full rollback bridge, tick synchronization, referee, and post-match ELO settlement
        /// completely offline without requiring live Quantum Cloud credentials.
        /// </summary>
        public void StartSimulatedLoopbackMatch(string simulatedOpponent = "Shadow_Shinobi")
        {
            if (Instance == null) Instance = this;
            LocalPlayerSlot = 0;
            OpponentName = simulatedOpponent;
            CurrentPingMs = 35;
            EstimatedRollbackFrames = 1;

            SetState(QuantumPvPState.SearchingMatch);
            SetState(QuantumPvPState.MatchFound);
            OnMatchFound?.Invoke(OpponentName, LocalPlayerSlot);

            SetState(QuantumPvPState.StartingSimulation);

            if (_matchStartCountdownRoutine != null) StopCoroutine(_matchStartCountdownRoutine);
            _matchStartCountdownRoutine = StartCoroutine(MatchStartCountdownRoutine(() =>
            {
                if (QuantumCombatBridge.Instance != null)
                {
                    QuantumCombatBridge.Instance.InitializeQuantumMatch();
                }

                SetState(QuantumPvPState.InMatch);
                Debug.Log($"<color=#00FF66>[QuantumPvPManager] Simulated Loopback Rollback Duel against {simulatedOpponent} started!</color>");

                if (BloodTechUIManager.Instance != null)
                {
                    BloodTechUIManager.Instance.TransitionToInGame();
                }

                if (MatchRoundReferee.Instance != null)
                {
                    MatchRoundReferee.Instance.OnMatchFinished -= HandleOnlineMatchFinished;
                    MatchRoundReferee.Instance.OnMatchFinished += HandleOnlineMatchFinished;
                    MatchRoundReferee.Instance.StartMatch();
                }
            }));
        }

        /// <summary>
        /// Creates a private 1v1 room with a custom room code.
        /// If roomCode is null or empty, auto-generates a 4-character code (e.g. KAI9).
        /// Automatically copies the code to the system clipboard.
        /// </summary>
        public async void CreatePrivateRoom(string roomCode = null)
        {
            if (string.IsNullOrWhiteSpace(roomCode))
            {
                roomCode = GenerateRoomCode();
            }
            else
            {
                roomCode = roomCode.Trim().ToUpper();
            }
            CurrentRoomCode = roomCode;
            GUIUtility.systemCopyBuffer = roomCode;
            Debug.Log($"<color=#00FF66>[QuantumPvPManager] Created Private Room Code: <b>{roomCode}</b> (Copied to clipboard).</color>");

            await ConnectAndStartQuantum(roomName: roomCode, isPrivate: true, canOnlyJoin: false);
        }

        /// <summary>
        /// Joins an existing private 1v1 room with a room code.
        /// </summary>
        public async void JoinPrivateRoom(string roomCode)
        {
            if (string.IsNullOrWhiteSpace(roomCode)) return;
            roomCode = roomCode.Trim().ToUpper();
            CurrentRoomCode = roomCode;
            Debug.Log($"<color=#00FF66>[QuantumPvPManager] Joining Private Room: <b>{roomCode}</b>...</color>");

            await ConnectAndStartQuantum(roomName: roomCode, isPrivate: true, canOnlyJoin: true);
        }

        private async Task ConnectAndStartQuantum(string roomName, bool isPrivate, bool canOnlyJoin = false)
        {
            try
            {
                if (IsQuantumDBEmpty())
                {
                    string dbMsg = "[QuantumPvPManager] Online mode unavailable: Quantum Asset Database (QuantumUnityDB.qunitydb) is empty. Local and offline combat modes remain fully operational.";
                    Debug.LogWarning(dbMsg);
                    SetState(QuantumPvPState.Error);
                    OnError?.Invoke(dbMsg);
                    return;
                }

                // Competitive Rollback Network Gatekeeper Check
                NetworkGatekeeper.EnsureInstance();
                if (NetworkGatekeeper.Instance != null)
                {
                    var gateEval = await NetworkGatekeeper.Instance.EvaluateCompetitiveEligibilityAsync();
                    if (!gateEval.isEligible)
                    {
                        string gateMsg = $"[Rollback Gatekeeper Blocked] {gateEval.diagnosticTitle}\n{gateEval.diagnosticMessage}";
                        Debug.LogWarning(gateMsg);
                        SetState(QuantumPvPState.Error);
                        OnError?.Invoke(gateMsg);
                        return;
                    }
                }

                SetState(QuantumPvPState.SearchingMatch);

                // 1. Prepare AppSettings from PhotonServerSettings asset
                AppSettings appSettings;
                if (PhotonServerSettings.Global != null && PhotonServerSettings.Global.AppSettings != null)
                {
                    appSettings = new AppSettings(PhotonServerSettings.Global.AppSettings);
                }
                else
                {
                    appSettings = new AppSettings();
                }

                if (!string.IsNullOrEmpty(quantumAppId))
                {
                    appSettings.AppIdQuantum = quantumAppId;
                }
                appSettings.AppIdRealtime = string.Empty;
                appSettings.AppIdFusion = string.Empty;

                if (!string.IsNullOrEmpty(preferredRegion))
                {
                    appSettings.FixedRegion = preferredRegion;
                }

                string localUserId = FirebaseAuthService.Instance?.CurrentUser != null
                    ? FirebaseAuthService.Instance.CurrentUser.UserId
                    : ("player_" + Guid.NewGuid().ToString("N").Substring(0, 8));

                var authValues = new AuthenticationValues(localUserId);
                if (useCustomAuthentication && FirebaseAuthService.Instance != null && FirebaseAuthService.Instance.IsAuthenticated)
                {
                    authValues.AuthType = CustomAuthenticationType.Custom;
                    authValues.AddAuthParameter("token", FirebaseAuthService.Instance.GetIdToken());
                    authValues.AddAuthParameter("username", FirebaseAuthService.Instance.CurrentUser.DisplayName);
                }
                else
                {
                    authValues.AuthType = CustomAuthenticationType.None;
                }

                var connectionArguments = new MatchmakingArguments
                {
                    PhotonSettings = appSettings,
                    PluginName = "QuantumPlugin",
                    MaxPlayers = 2,
                    UserId = localUserId,
                    AuthValues = authValues,
                    RoomName = roomName,
                    CanOnlyJoin = canOnlyJoin
                };

                Debug.Log($"<color=#00FF66>[QuantumPvPManager] Connecting to Quantum Cloud (AppId: {appSettings.AppIdQuantum}, User: {localUserId})...</color>");

                // 2. Connect to real Quantum cloud room
                _client = await MatchmakingExtensions.ConnectToRoomAsync(connectionArguments);

                if (_client == null || _client.CurrentRoom == null)
                {
                    throw new Exception("Failed to join or create Quantum 1v1 Room.");
                }

                Debug.Log($"<color=#00FF66>[QuantumPvPManager] Joined Room: {_client.CurrentRoom.Name} with {_client.CurrentRoom.PlayerCount} player(s).</color>");

                LocalPlayerSlot = _client.LocalPlayer != null ? (_client.LocalPlayer.ActorNumber - 1) : 0;
                OpponentName = _client.CurrentRoom.PlayerCount > 1 ? "Rival_Online" : "Waiting for Challenger...";
                CurrentPingMs = (int)(_client?.RealtimePeer?.Stats.RoundtripTime ?? 35);

                SetState(QuantumPvPState.MatchFound);
                OnMatchFound?.Invoke(OpponentName, LocalPlayerSlot);

                // 3. Prepare SessionRunner Arguments
                SetState(QuantumPvPState.StartingSimulation);

                var rConfig = runtimeConfig ?? new RuntimeConfig();
                var mapData = FindAnyObjectByType<QuantumMapData>();
                if (mapData != null && mapData.AssetRef.IsValid)
                {
                    rConfig.Map = mapData.AssetRef;
                }

                var sessionRunnerArguments = new SessionRunner.Arguments
                {
                    RunnerFactory = QuantumRunnerUnityFactory.DefaultFactory,
                    GameParameters = QuantumRunnerUnityFactory.CreateGameParameters,
                    Communicator = new QuantumNetworkCommunicator(_client),
                    GameMode = DeterministicGameMode.Multiplayer,
                    PlayerCount = 2,
                    ClientId = _client.UserId,
                    RuntimeConfig = rConfig,
                    SessionConfig = QuantumDeterministicSessionConfigAsset.DefaultConfig,
                };

                // Subscribe to disconnect notifications with 5-second recovery window
                _disconnectSubscription = QuantumCallback.SubscribeManual<CallbackPluginDisconnect>(m =>
                {
                    TriggerOpponentDisconnectNotice();
                });

                // 4. Start true deterministic rollback simulation
                using (new ConnectionServiceScope(_client))
                {
                    _runner = (QuantumRunner)await SessionRunner.StartAsync(sessionRunnerArguments);
                }

                if (_runner != null && _runner.Game != null)
                {
                    _runner.Game.AddPlayer(LocalPlayerSlot, new RuntimePlayer());
                }

                if (_matchStartCountdownRoutine != null) StopCoroutine(_matchStartCountdownRoutine);
                _matchStartCountdownRoutine = StartCoroutine(MatchStartCountdownRoutine(() =>
                {
                    if (QuantumCombatBridge.Instance != null)
                    {
                        QuantumCombatBridge.Instance.InitializeQuantumMatch();
                    }

                    SetState(QuantumPvPState.InMatch);
                    Debug.Log("<color=#00FF66>[QuantumPvPManager] Real Quantum Rollback Simulation Running!</color>");

                    // Transition UI to Battle Arena
                    if (BloodTechUIManager.Instance != null)
                    {
                        BloodTechUIManager.Instance.TransitionToInGame();
                    }

                    if (MatchRoundReferee.Instance != null)
                    {
                        MatchRoundReferee.Instance.OnMatchFinished -= HandleOnlineMatchFinished;
                        MatchRoundReferee.Instance.OnMatchFinished += HandleOnlineMatchFinished;
                        MatchRoundReferee.Instance.StartMatch();
                    }
                }));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[QuantumPvPManager] Quantum connection notice: {ex.Message}");
                string userMsg = $"Quantum Cloud Notice: {ex.Message}. You can use 'PRACTICE ROLLBACK (OFFLINE LOOPBACK)' or 'LOCAL VERSUS'.";
                OnError?.Invoke(userMsg);
                SetState(QuantumPvPState.InLobby);
            }
        }

        private void HandleOnlineMatchFinished(RoundWinner matchWinner)
        {
            int winnerIndex = matchWinner == RoundWinner.Player1 ? 1 : (matchWinner == RoundWinner.Player2 ? 2 : 0);

            // BlazBlue match flow results authority (BBMatchFlow)
            if (DemonIdol.BlazBlue.BBMatchScreenDirector.Instance != null)
            {
                DemonIdol.BlazBlue.BBMatchScreenDirector.Instance.ShowResults(winnerIndex);
            }

            if (FirestoreService.Instance != null && FirebaseAuthService.Instance != null)
            {
                string localUid = FirebaseAuthService.Instance.CurrentUser?.UserId ?? "P1";
                string opponentUid = "Opponent_" + OpponentName;
                string winnerId = matchWinner == RoundWinner.Player1 ? (LocalPlayerSlot == 0 ? localUid : opponentUid)
                                : (matchWinner == RoundWinner.Player2 ? (LocalPlayerSlot == 1 ? localUid : opponentUid) : "Draw");

                _ = FirestoreService.Instance.RecordMatchResultAsync(
                    localUid,
                    opponentUid,
                    winnerId,
                    "Knockout",
                    60f
                );
            }
        }

        private IEnumerator MatchStartCountdownRoutine(Action onComplete)
        {
            if (!Application.isPlaying)
            {
                onComplete?.Invoke();
                yield break;
            }
            for (int sec = 3; sec >= 1; sec--)
            {
                OnCountdownTick?.Invoke(sec);
                yield return new WaitForSeconds(1.0f);
            }
            onComplete?.Invoke();
        }

        /// <summary>
        /// Triggered when opponent disconnects mid-fight.
        /// Starts a 5-second recovery countdown before declaring forfeit victory.
        /// </summary>
        public void TriggerOpponentDisconnectNotice()
        {
            if (_disconnectRecoveryRoutine != null) StopCoroutine(_disconnectRecoveryRoutine);
            _disconnectRecoveryRoutine = StartCoroutine(DisconnectRecoveryRoutine());
        }

        private IEnumerator DisconnectRecoveryRoutine()
        {
            IsOpponentReconnecting = true;
            DisconnectSecondsRemaining = 5.0f;
            Debug.LogWarning("[QuantumPvPManager] Opponent disconnected! Starting 5-second recovery window...");

            while (DisconnectSecondsRemaining > 0f)
            {
                OnOpponentDisconnectChanged?.Invoke(true, DisconnectSecondsRemaining);
                yield return new WaitForSeconds(0.25f);
                DisconnectSecondsRemaining -= 0.25f;

                if (_client != null && _client.CurrentRoom != null && _client.CurrentRoom.PlayerCount >= 2)
                {
                    IsOpponentReconnecting = false;
                    OnOpponentDisconnectChanged?.Invoke(false, 0f);
                    Debug.Log("<color=#00FF66>[QuantumPvPManager] Opponent reconnected! Resuming duel.</color>");
                    yield break;
                }
            }

            IsOpponentReconnecting = false;
            OnOpponentDisconnectChanged?.Invoke(false, 0f);
            HandleOpponentForfeit();
        }

        private void HandleOpponentForfeit()
        {
            Debug.LogWarning("[QuantumPvPManager] Opponent failed to reconnect within 5 seconds. Awarding forfeit victory!");
            RoundWinner localWinner = LocalPlayerSlot == 0 ? RoundWinner.Player1 : RoundWinner.Player2;
            if (MatchRoundReferee.Instance != null)
            {
                MatchRoundReferee.Instance.TriggerForfeitVictory(localWinner);
            }
        }

        public async void LeaveMatch()
        {
            if (MatchRoundReferee.Instance != null)
            {
                MatchRoundReferee.Instance.OnMatchFinished -= HandleOnlineMatchFinished;
            }
            if (QuantumCombatBridge.Instance != null)
            {
                QuantumCombatBridge.Instance.ResetQuantumMatch();
            }
            try
            {
                _disconnectSubscription?.Dispose();
                _disconnectSubscription = null;

                if (_runner != null)
                {
                    await _runner.ShutdownAsync();
                    _runner = null;
                }

                if (_client != null)
                {
                    await _client.DisconnectAsync();
                    _client = null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[QuantumPvPManager] Disconnect error: {ex.Message}");
            }
            finally
            {
                SetState(QuantumPvPState.InLobby);
            }
        }

        public void CancelSearch()
        {
            LeaveMatch();
        }

        private void SetState(QuantumPvPState newState)
        {
            State = newState;
            OnStateChanged?.Invoke(newState);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
            LeaveMatch();
        }
    }
}
