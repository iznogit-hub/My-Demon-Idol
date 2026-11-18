using System;
using System.Collections.Generic;
using UnityEngine;

namespace DemonIdol.Combat
{
    [Serializable]
    public class MetaverseAvatarState
    {
        public string peerId;
        public string displayName;
        public string characterId = "C01";
        public Vector3 position;
        public float rotationY;
        public string currentEmote = "Idle"; // Idle, Wave, Bow, ReadyStance, FistPump
        public int duelChallengerSlot = -1; // -1 = None, >= 0 = Challenged peer
    }

    [Serializable]
    public class ArcadeCabinetStation
    {
        public string cabinetId;
        public string stageId = "ST01";
        public string player1PeerId = string.Empty;
        public string player2PeerId = string.Empty;
        public bool IsOccupied => !string.IsNullOrEmpty(player1PeerId) && !string.IsNullOrEmpty(player2PeerId);
        public bool HasFreeSlot => string.IsNullOrEmpty(player1PeerId) || string.IsNullOrEmpty(player2PeerId);
    }

    /// <summary>
    /// WebGL & Metaverse Virtual Battle Hub Director.
    /// Emulates the Street Fighter 6 Battle Hub / Metaverse arcade floor:
    /// - 3D social space where 3D chibi/full avatars walk around.
    /// - Physical virtual Arcade Cabinets where players sit down face-to-face to initiate 1v1 duels.
    /// - Real-time emote sync, avatar custom looks, and spectator viewing.
    /// - Seamlessly transitions into full 60Hz deterministic combat when two avatars sit at a cabinet.
    /// </summary>
    public class MetaverseBattleHubManager : MonoBehaviour
    {
        public static MetaverseBattleHubManager Instance { get; private set; }

        [Header("Local Player Avatar State")]
        public MetaverseAvatarState localAvatar = new MetaverseAvatarState();

        [Header("Virtual Battle Hub World")]
        public string hubRegion = "Cyberpunk_Tokyo_Dojo";
        public int maxHubCapacity = 32;

        public readonly List<ArcadeCabinetStation> arcadeCabinets = new List<ArcadeCabinetStation>
        {
            new ArcadeCabinetStation { cabinetId = "CAB_01", stageId = "ST01" },
            new ArcadeCabinetStation { cabinetId = "CAB_02", stageId = "ST02" },
            new ArcadeCabinetStation { cabinetId = "CAB_03", stageId = "ST03" },
            new ArcadeCabinetStation { cabinetId = "CAB_04", stageId = "ST04" }
        };

        public readonly Dictionary<string, MetaverseAvatarState> remotePeers = new Dictionary<string, MetaverseAvatarState>();

        public event Action<string, string> OnDuelChallenged; // challengerId, targetId
        public event Action<ArcadeCabinetStation> OnCabinetMatchStarted;
        public event Action<string, string> OnAvatarEmoteTriggered; // peerId, emoteName

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                if (Application.isPlaying) Destroy(gameObject);
                else DestroyImmediate(gameObject);
                return;
            }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);

            InitializeLocalAvatar();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void InitializeLocalAvatar()
        {
            localAvatar.peerId = "peer_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            localAvatar.displayName = "Operative_" + UnityEngine.Random.Range(100, 999);
            localAvatar.characterId = "C01";
            localAvatar.position = Vector3.zero;
            localAvatar.rotationY = 0f;
            localAvatar.currentEmote = "Idle";
        }

        public bool TrySitAtCabinet(string cabinetId, out string failureReason)
        {
            var cabinet = arcadeCabinets.Find(c => c.cabinetId == cabinetId);
            if (cabinet == null)
            {
                failureReason = "Cabinet not found.";
                return false;
            }

            if (cabinet.IsOccupied)
            {
                failureReason = "Cabinet is currently in a 1v1 battle.";
                return false;
            }

            if (string.IsNullOrEmpty(cabinet.player1PeerId))
            {
                cabinet.player1PeerId = localAvatar.peerId;
                failureReason = null;
                Debug.Log($"<color=#69F0AE>[BattleHub] Sat down at {cabinetId} as Challenger 1. Waiting for opponent...</color>");
                return true;
            }
            else if (string.IsNullOrEmpty(cabinet.player2PeerId))
            {
                cabinet.player2PeerId = localAvatar.peerId;
                failureReason = null;
                Debug.Log($"<color=#69F0AE>[BattleHub] Sat down at {cabinetId} as Challenger 2! Duel initiating!</color>");
                OnCabinetMatchStarted?.Invoke(cabinet);
                return true;
            }

            failureReason = "Cabinet is full.";
            return false;
        }

        public void LeaveCabinet(string cabinetId)
        {
            var cabinet = arcadeCabinets.Find(c => c.cabinetId == cabinetId);
            if (cabinet == null) return;

            if (cabinet.player1PeerId == localAvatar.peerId) cabinet.player1PeerId = string.Empty;
            if (cabinet.player2PeerId == localAvatar.peerId) cabinet.player2PeerId = string.Empty;
        }

        public void TriggerEmote(string emoteName)
        {
            localAvatar.currentEmote = emoteName;
            OnAvatarEmoteTriggered?.Invoke(localAvatar.peerId, emoteName);
            Debug.Log($"[BattleHub] Triggered avatar emote: {emoteName}");
        }

        public void ChallengePeer(string targetPeerId)
        {
            OnDuelChallenged?.Invoke(localAvatar.peerId, targetPeerId);
            Debug.Log($"[BattleHub] Challenged peer {targetPeerId} to an impromptu duel!");
        }
    }
}
