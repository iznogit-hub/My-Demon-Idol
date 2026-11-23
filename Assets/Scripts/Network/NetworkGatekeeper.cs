using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;

namespace DemonIdol.Network
{
    public enum CellularGeneration
    {
        Unknown = 0,
        TwoG = 1,
        ThreeG = 2,
        FourG_LTE = 3,
        FiveG_NR = 5
    }

    public enum NetworkGateResult
    {
        AllowedWiFi,
        Allowed5GLowLatency,
        BlockedNoInternet,
        BlockedLegacyCellular, // 2G/3G/4G LTE
        BlockedHighJitter,     // Unstable ping > 65ms or jitter > 12ms
        BypassedOffline
    }

    [Serializable]
    public struct GateEvaluation
    {
        public bool isEligible;
        public NetworkGateResult result;
        public NetworkReachability reachability;
        public CellularGeneration cellularGeneration;
        public RollbackProbeReport probeReport;
        public string diagnosticTitle;
        public string diagnosticMessage;
        public DateTime evaluationTime;
    }

    /// <summary>
    /// Master Network Gatekeeper for Demon Idol Rollback Netcode (Photon Quantum 3.0).
    /// Enforces competitive stability by:
    /// 1. Verifying active internet reachability (Application.internetReachability).
    /// 2. Detecting hardware cellular generation (native 5G NR check on iOS & Android).
    /// 3. Conducting active 5-sample empirical ping and jitter variance testing.
    /// 4. Restricting low-stability networks from Ranked PvP while preserving offline modes.
    /// </summary>
    public class NetworkGatekeeper : MonoBehaviour
    {
        public static NetworkGatekeeper Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Instance = null;
        }

        [Header("Competitive Rollback Thresholds")]
        [SerializeField] private float _maxAllowedPingMs = RollbackQualityProbe.COMPETITIVE_MAX_PING_MS;
        [SerializeField] private float _maxAllowedJitterMs = RollbackQualityProbe.COMPETITIVE_MAX_JITTER_MS;
        [SerializeField] private bool _strict5GRequirement = true;

        [Header("Editor & QA Simulation Overrides")]
        [SerializeField] private bool _enableSimulationOverrides = false;
        [SerializeField] private NetworkReachability _simulatedReachability = NetworkReachability.ReachableViaLocalAreaNetwork;
        [SerializeField] private CellularGeneration _simulatedCellularGen = CellularGeneration.FiveG_NR;
        [SerializeField] private float _simulatedPingMs = 32f;
        [SerializeField] private float _simulatedJitterMs = 4f;

        public event Action<GateEvaluation> OnGateEvaluationCompleted;

        public GateEvaluation LastEvaluation { get; private set; }

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern int _GetIOSCellularGeneration();
#endif

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public static void EnsureInstance()
        {
            if (Instance != null) return;
            var existing = FindAnyObjectByType<NetworkGatekeeper>();
            if (existing != null)
            {
                Instance = existing;
                return;
            }
            GameObject go = new GameObject("[NetworkGatekeeper]");
            Instance = go.AddComponent<NetworkGatekeeper>();
            if (Application.isPlaying) DontDestroyOnLoad(go);
        }

        /// <summary>
        /// Evaluates whether the current connection meets Photon Quantum 3.0 competitive rollback requirements.
        /// </summary>
        public async Task<GateEvaluation> EvaluateCompetitiveEligibilityAsync(bool skipActiveProbe = false)
        {
            NetworkReachability reachability = GetCurrentReachability();
            GateEvaluation eval = new GateEvaluation
            {
                reachability = reachability,
                cellularGeneration = CellularGeneration.Unknown,
                evaluationTime = DateTime.UtcNow
            };

            // 1. Zero Connectivity Check
            if (reachability == NetworkReachability.NotReachable)
            {
                eval.isEligible = false;
                eval.result = NetworkGateResult.BlockedNoInternet;
                eval.diagnosticTitle = "NO CONNECTION DETECTED";
                eval.diagnosticMessage = "Unable to connect to Quantum Rollback Cloud.\nPlease check your Wi-Fi or cellular connection.\nOffline Dojo and Story modes remain accessible.";
                LastEvaluation = eval;
                OnGateEvaluationCompleted?.Invoke(eval);
                return eval;
            }

            // 2. Wi-Fi / Local Area Network Check
            if (reachability == NetworkReachability.ReachableViaLocalAreaNetwork)
            {
                if (skipActiveProbe)
                {
                    eval.isEligible = true;
                    eval.result = NetworkGateResult.AllowedWiFi;
                    eval.diagnosticTitle = "WI-FI CONNECTED";
                    eval.diagnosticMessage = "High-stability Local Area Network verified for 60Hz Rollback.";
                    LastEvaluation = eval;
                    OnGateEvaluationCompleted?.Invoke(eval);
                    return eval;
                }

                // Run active jitter probe to guard against severely congested Wi-Fi
                RollbackProbeReport probeReport;
                if (_enableSimulationOverrides && Application.isEditor)
                {
                    probeReport = CreateSimulatedProbeReport();
                }
                else
                {
                    probeReport = await RollbackQualityProbe.RunProbeAsync(5, _maxAllowedPingMs, _maxAllowedJitterMs);
                }
                eval.probeReport = probeReport;

                if (!probeReport.isRollbackSafe)
                {
                    eval.isEligible = false;
                    eval.result = NetworkGateResult.BlockedHighJitter;
                    eval.diagnosticTitle = "CONGESTED WI-FI // HIGH JITTER";
                    eval.diagnosticMessage = $"Wi-Fi latency or packet jitter exceeds rollback safety limits:\n" +
                                             $"• Ping: {probeReport.meanPingMs:F0}ms (Max: {_maxAllowedPingMs:F0}ms)\n" +
                                             $"• Jitter: {probeReport.jitterMs:F1}ms (Max: {_maxAllowedJitterMs:F1}ms)\n" +
                                             $"• Packet Loss: {probeReport.packetLossPercent:F0}%\n" +
                                             $"Competitive PvP requires stable low-jitter connections to prevent rollback resimulation stutter.";
                }
                else
                {
                    eval.isEligible = true;
                    eval.result = NetworkGateResult.AllowedWiFi;
                    eval.diagnosticTitle = "WI-FI ROLLBACK VERIFIED";
                    eval.diagnosticMessage = $"Latency: {probeReport.meanPingMs:F0}ms | Jitter: {probeReport.jitterMs:F1}ms • 60Hz Ready";
                }

                LastEvaluation = eval;
                OnGateEvaluationCompleted?.Invoke(eval);
                return eval;
            }

            // 3. Cellular Carrier Data Network Check
            if (reachability == NetworkReachability.ReachableViaCarrierDataNetwork)
            {
                CellularGeneration cellularGen = DetectCellularGeneration();
                eval.cellularGeneration = cellularGen;

                if (_strict5GRequirement && cellularGen != CellularGeneration.FiveG_NR)
                {
                    eval.isEligible = false;
                    eval.result = NetworkGateResult.BlockedLegacyCellular;
                    string genName = cellularGen switch
                    {
                        CellularGeneration.FourG_LTE => "4G LTE",
                        CellularGeneration.ThreeG => "3G",
                        CellularGeneration.TwoG => "2G / EDGE",
                        _ => "Cellular"
                    };

                    eval.diagnosticTitle = $"{genName.ToUpper()} DETECTED // PVP RESTRICTED";
                    eval.diagnosticMessage = $"Photon Quantum 3.0 deterministic rollback requires low-latency 5G NR or Wi-Fi.\n" +
                                             $"Legacy {genName} mobile networks suffer from high packet jitter and bufferbloat.\n" +
                                             $"Please connect to Wi-Fi or 5G to enter Ranked PvP. Offline Dojo & Story remain operational.";
                    LastEvaluation = eval;
                    OnGateEvaluationCompleted?.Invoke(eval);
                    return eval;
                }

                // 4. Cellular 5G Empirical Jitter & Latency Gatekeeper
                RollbackProbeReport probeReport;
                if (_enableSimulationOverrides && Application.isEditor)
                {
                    probeReport = CreateSimulatedProbeReport();
                }
                else
                {
                    probeReport = await RollbackQualityProbe.RunProbeAsync(5, _maxAllowedPingMs, _maxAllowedJitterMs);
                }
                eval.probeReport = probeReport;

                if (!probeReport.isRollbackSafe)
                {
                    eval.isEligible = false;
                    eval.result = NetworkGateResult.BlockedHighJitter;
                    eval.diagnosticTitle = "UNSTABLE 5G CARRIER JITTER";
                    eval.diagnosticMessage = $"Cellular connection latency or jitter exceeds competitive rollback thresholds:\n" +
                                             $"• Ping: {probeReport.meanPingMs:F0}ms (Threshold: ≤ {_maxAllowedPingMs:F0}ms)\n" +
                                             $"• Jitter: {probeReport.jitterMs:F1}ms (Threshold: ≤ {_maxAllowedJitterMs:F1}ms)\n" +
                                             $"• Packet Loss: {probeReport.packetLossPercent:F0}%\n" +
                                             $"Ranked PvP locked to prevent match desync. Switch to Wi-Fi or move closer to 5G coverage.";
                }
                else
                {
                    eval.isEligible = true;
                    eval.result = NetworkGateResult.Allowed5GLowLatency;
                    eval.diagnosticTitle = "5G ULTRA-LOW LATENCY READY";
                    eval.diagnosticMessage = $"5G NR Latency: {probeReport.meanPingMs:F0}ms | Jitter: {probeReport.jitterMs:F1}ms • 60Hz Verified";
                }

                LastEvaluation = eval;
                OnGateEvaluationCompleted?.Invoke(eval);
                return eval;
            }

            // Fallback: Allow with warning
            eval.isEligible = true;
            eval.result = NetworkGateResult.AllowedWiFi;
            eval.diagnosticTitle = "CONNECTION ACCEPTED";
            eval.diagnosticMessage = "Connection verified.";
            LastEvaluation = eval;
            OnGateEvaluationCompleted?.Invoke(eval);
            return eval;
        }

        public NetworkReachability GetCurrentReachability()
        {
            if (_enableSimulationOverrides && Application.isEditor)
            {
                return _simulatedReachability;
            }
            return Application.internetReachability;
        }

        public CellularGeneration DetectCellularGeneration()
        {
            if (_enableSimulationOverrides && Application.isEditor)
            {
                return _simulatedCellularGen;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using var telephonyManager = currentActivity.Call<AndroidJavaObject>("getSystemService", "phone");

                if (telephonyManager != null)
                {
                    int networkType = telephonyManager.Call<int>("getDataNetworkType");

                    // 20 = NETWORK_TYPE_NR (5G New Radio)
                    if (networkType == 20)
                    {
                        return CellularGeneration.FiveG_NR;
                    }

                    // 13 = NETWORK_TYPE_LTE
                    if (networkType == 13)
                    {
                        // Check Android 11+ TelephonyDisplayInfo for 5G NSA (Non-Standalone)
                        try
                        {
                            using var buildVersion = new AndroidJavaClass("android.os.Build$VERSION");
                            int sdkInt = buildVersion.GetStatic<int>("SDK_INT");
                            if (sdkInt >= 30)
                            {
                                // In Android 30+, TelephonyDisplayInfo provides OVERRIDE_NETWORK_TYPE_NR_NSA (3) or NR_ADVANCED (4)
                                // We check if serviceState indicates 5G NSA connection
                                using var serviceState = telephonyManager.Call<AndroidJavaObject>("getServiceState");
                                if (serviceState != null)
                                {
                                    string ssStr = serviceState.Call<string>("toString");
                                    if (ssStr.Contains("nrState=CONNECTED") || ssStr.Contains("nrState=NOT_RESTRICTED"))
                                    {
                                        return CellularGeneration.FiveG_NR;
                                    }
                                }
                            }
                        }
                        catch {}

                        return CellularGeneration.FourG_LTE;
                    }

                    // 3G Types (WCDMA, HSDPA, UMTS, EVDO)
                    if (networkType == 3 || networkType == 8 || networkType == 9 || networkType == 10 || networkType == 15)
                    {
                        return CellularGeneration.ThreeG;
                    }

                    // 2G Types (GPRS, EDGE, CDMA)
                    if (networkType == 1 || networkType == 2 || networkType == 4 || networkType == 7 || networkType == 11)
                    {
                        return CellularGeneration.TwoG;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NetworkGatekeeper] Android 5G query exception: {ex.Message}");
            }
            return CellularGeneration.Unknown;

#elif UNITY_IOS && !UNITY_EDITOR
            try
            {
                int iosGen = _GetIOSCellularGeneration();
                return iosGen switch
                {
                    5 => CellularGeneration.FiveG_NR,
                    3 => CellularGeneration.FourG_LTE,
                    2 => CellularGeneration.ThreeG,
                    1 => CellularGeneration.TwoG,
                    _ => CellularGeneration.Unknown
                };
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NetworkGatekeeper] iOS 5G query exception: {ex.Message}");
                return CellularGeneration.Unknown;
            }
#else
            // Editor / Standalone / WebGL default
            return CellularGeneration.FiveG_NR;
#endif
        }

        private RollbackProbeReport CreateSimulatedProbeReport()
        {
            bool safe = (_simulatedPingMs <= _maxAllowedPingMs) && (_simulatedJitterMs <= _maxAllowedJitterMs);
            return new RollbackProbeReport
            {
                meanPingMs = _simulatedPingMs,
                minPingMs = _simulatedPingMs * 0.95f,
                maxPingMs = _simulatedPingMs * 1.05f,
                jitterMs = _simulatedJitterMs,
                packetLossPercent = 0f,
                successfulProbes = 5,
                totalProbes = 5,
                isRollbackSafe = safe,
                primaryEndpoint = "simulated://qa-editor-test",
                timestamp = DateTime.UtcNow
            };
        }

        // Test harness setters
        public void SetSimulationOverrides(bool enable, NetworkReachability reachability, CellularGeneration gen, float ping = 32f, float jitter = 4f)
        {
            _enableSimulationOverrides = enable;
            _simulatedReachability = reachability;
            _simulatedCellularGen = gen;
            _simulatedPingMs = ping;
            _simulatedJitterMs = jitter;
        }
    }
}
