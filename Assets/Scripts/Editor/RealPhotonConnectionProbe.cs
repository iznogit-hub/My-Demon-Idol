#if UNITY_EDITOR
using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Photon.Realtime;
using Quantum;
using DemonIdol.Network;

namespace DemonIdol.Editor
{
    public static class RealPhotonConnectionProbe
    {
        [MenuItem("Demon Idol/🧪 Verification & Audit/Probe Real Photon Cloud Connection")]
        public static async void ProbeRealConnection()
        {
            AssetDatabase.ImportAsset("Assets/QuantumUser/Resources/PhotonServerSettings.asset", ImportAssetOptions.ForceUpdate);
            var settings = Resources.Load<PhotonServerSettings>("PhotonServerSettings");
            if (settings == null)
            {
                settings = AssetDatabase.LoadAssetAtPath<PhotonServerSettings>("Assets/QuantumUser/Resources/PhotonServerSettings.asset");
            }
            if (settings == null || settings.AppSettings == null || string.IsNullOrEmpty(settings.AppSettings.AppIdQuantum))
            {
                Debug.LogError("<color=#ff1744>[RealPhotonProbe] FAILED: AppIdQuantum is empty in PhotonServerSettings.asset! Cannot connect to Photon Cloud without an App ID.</color>");
                return;
            }

            string appId = settings.AppSettings.AppIdQuantum;
            Debug.Log($"<color=#00e676>[RealPhotonProbe] Attempting REAL connection to Photon Cloud with AppId: {appId.Substring(0, 8)}... (Region: best)</color>");

            var appSettings = new AppSettings(settings.AppSettings);
            appSettings.AppIdRealtime = string.Empty;
            appSettings.AppIdFusion = string.Empty;

            var chGo = new GameObject("ConnectionHandler");
            typeof(ConnectionHandler).GetField("go", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?.SetValue(null, chGo);

            string localUserId = "probe_" + Guid.NewGuid().ToString("N").Substring(0, 6);
            var connectionArguments = new MatchmakingArguments
            {
                PhotonSettings = appSettings,
                PluginName = "QuantumPlugin",
                MaxPlayers = 2,
                UserId = localUserId,
                AuthValues = new AuthenticationValues(localUserId),
                RoomName = "DemonIdol_Probe_" + Guid.NewGuid().ToString("N").Substring(0, 4),
                CanOnlyJoin = false,
                AsyncConfig = AsyncConfig.CreateUnityAsyncConfig()
            };

            try
            {
                var client = await MatchmakingExtensions.ConnectToRoomAsync(connectionArguments);
                if (client != null && client.CurrentRoom != null)
                {
                    int ping = (int)(client.RealtimePeer?.Stats.RoundtripTime ?? 0);
                    Debug.Log($"<color=#00e676>[RealPhotonProbe] REAL PHOTON CLOUD CONNECTED! Room: {client.CurrentRoom.Name}, PlayerCount: {client.CurrentRoom.PlayerCount}, Real Ping: {ping}ms, Server: {client.CurrentServerAddress}</color>");
                    client.Disconnect();
                }
                else
                {
                    Debug.LogWarning($"<color=#ff9100>[RealPhotonProbe] ConnectToRoomAsync returned client without room: IsConnected={client?.IsConnected}, State={client?.State}</color>");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"<color=#ff1744>[RealPhotonProbe] REAL PHOTON CLOUD ERROR: {ex.GetType().Name}: {ex.Message}\nStack: {ex.StackTrace}</color>");
            }
        }
    }
}
#endif
