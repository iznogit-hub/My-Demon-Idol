using System;
using UnityEngine;
using DemonIdol.UI;

namespace DemonIdol.Network
{
    /// <summary>
    /// Manages the full lifecycle of an online 1v1 PvP match:
    /// Round wins, match settlements, MMR calculation, rematch votes, and return to lobby.
    /// </summary>
    public class PvPMatchController : MonoBehaviour
    {
        public static PvPMatchController Instance { get; private set; }

        private const string MMR_SAVE_KEY = "DemonIdol_Player_MMR";

        [Header("Ranked Settings")]
        public int currentMMR { get; private set; } = 1200; // Starting MMR
        public int lastMMRDelta { get; private set; } = 0;

        public bool LocalPlayerWon { get; private set; } = false;
        public bool IsRematchRequested { get; private set; } = false;

        public event Action<bool, int> OnMatchSettled; // isWinner, mmrDelta
        public event Action OnRematchAccepted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            currentMMR = PlayerPrefs.GetInt(MMR_SAVE_KEY, 1200);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Called when the match finishes (someone won 2 rounds in best of 3).
        /// </summary>
        public void HandleMatchFinished(int winnerIndex)
        {
            int localSlot = QuantumPvPManager.Instance != null ? QuantumPvPManager.Instance.LocalPlayerSlot : 0;
            int localPlayerIndex = localSlot + 1; // 1 or 2

            LocalPlayerWon = (winnerIndex == localPlayerIndex);

            // Calculate MMR Delta
            if (LocalPlayerWon)
            {
                lastMMRDelta = +25;
            }
            else
            {
                lastMMRDelta = -18;
            }

            currentMMR = Mathf.Max(0, currentMMR + lastMMRDelta);
            PlayerPrefs.SetInt(MMR_SAVE_KEY, currentMMR);
            PlayerPrefs.Save();

            OnMatchSettled?.Invoke(LocalPlayerWon, lastMMRDelta);
        }

        public void RequestRematch()
        {
            IsRematchRequested = true;
            OnRematchAccepted?.Invoke();
        }

        public void LeaveToPvPLobby()
        {
            if (QuantumPvPManager.Instance != null)
            {
                QuantumPvPManager.Instance.LeaveMatch();
            }

            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }
}
