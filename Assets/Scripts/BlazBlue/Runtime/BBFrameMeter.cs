using System;
using UnityEngine;
using DemonIdol.Combat;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// SF6-style Frame Meter bar for training mode.
    /// Visualizes the last 60 ticks of simulation history:
    /// Startup (green), Active (red), Recovery (blue), Stun (yellow), Invulnerable (white).
    /// Displays frame advantage dynamically based on sim authority.
    /// </summary>
    public class BBFrameMeter : MonoBehaviour
    {
        public static bool IsMeterVisible = false;

        public enum FrameType : byte
        {
            Neutral,
            Startup,
            Active,
            Recovery,
            Hitstun,
            Blockstun,
            Invulnerable
        }

        private const int HISTORY_LENGTH = 60;
        private readonly FrameType[] _p1History = new FrameType[HISTORY_LENGTH];
        private readonly FrameType[] _p2History = new FrameType[HISTORY_LENGTH];
        private int _historyIndex = 0;

        [Header("Fighters")]
        public BBFighterBrain p1Brain;
        public BBFighterBrain p2Brain;

        public int p1Advantage = 0;
        public int p2Advantage = 0;

        private GUIStyle _boxStyle;
        private GUIStyle _textStyle;

        private void Start()
        {
            FindBrainsIfMissing();
            if (BBMatchSim.Instance != null)
            {
                BBMatchSim.Instance.OnSimTickCompleted -= HandleSimTickCompleted;
                BBMatchSim.Instance.OnSimTickCompleted += HandleSimTickCompleted;
            }
            else if (CombatTickEngine.Instance != null)
            {
                CombatTickEngine.Instance.OnTick += OnCombatTick;
            }
        }

        private void OnDestroy()
        {
            if (BBMatchSim.Instance != null)
            {
                BBMatchSim.Instance.OnSimTickCompleted -= HandleSimTickCompleted;
            }
            if (CombatTickEngine.Instance != null)
            {
                CombatTickEngine.Instance.OnTick -= OnCombatTick;
            }
        }

        private void HandleSimTickCompleted(BBFighterSnapshot snap1, BBFighterSnapshot snap2)
        {
            OnCombatTick(snap1.tick);
        }

        private void FindBrainsIfMissing()
        {
            if (p1Brain == null || p2Brain == null)
            {
                var brains = FindObjectsByType<BBFighterBrain>(FindObjectsSortMode.None);
                if (brains != null)
                {
                    if (brains.Length > 0 && p1Brain == null) p1Brain = brains[0];
                    if (brains.Length > 1 && p2Brain == null) p2Brain = brains[1];
                }
            }
        }

        private void OnCombatTick(int tick)
        {
            FindBrainsIfMissing();

            _p1History[_historyIndex] = SampleFighterFrame(p1Brain);
            _p2History[_historyIndex] = SampleFighterFrame(p2Brain);
            _historyIndex = (_historyIndex + 1) % HISTORY_LENGTH;

            // Compute frame advantage
            if (p1Brain != null && p2Brain != null)
            {
                p1Advantage = p2Brain.hitstunRemaining + p2Brain.blockstunRemaining - (p1Brain.attackTick > 0 ? (p1Brain.currentAttack != null ? p1Brain.currentAttack.TotalTicks - p1Brain.attackTick : 0) : 0);
            }
        }

        private FrameType SampleFighterFrame(BBFighterBrain brain)
        {
            if (brain == null) return FrameType.Neutral;

            if (brain.currentInvuln != BBInvuln.None) return FrameType.Invulnerable;

            switch (brain.bbState)
            {
                case BBState.AttackStartup:
                    return FrameType.Startup;
                case BBState.AttackActive:
                    return FrameType.Active;
                case BBState.AttackRecovery:
                    return FrameType.Recovery;
                case BBState.Hitstun:
                case BBState.Crumple:
                    return FrameType.Hitstun;
                case BBState.Blockstun:
                case BBState.BarrierBlockstun:
                    return FrameType.Blockstun;
                default:
                    return FrameType.Neutral;
            }
        }

        private void OnGUI()
        {
            if (!IsMeterVisible) return;

            InitStyles();

            float barWidth = 10f;
            float barHeight = 24f;
            float totalWidth = HISTORY_LENGTH * barWidth;
            float startX = (Screen.width - totalWidth) * 0.5f;
            float startY = Screen.height - 100f;

            // Draw Background Box
            GUI.Box(new Rect(startX - 10f, startY - 30f, totalWidth + 20f, 90f), GUIContent.none);

            // Header Labels
            string advText = p1Advantage >= 0 ? $"+{p1Advantage}" : $"{p1Advantage}";
            GUI.Label(new Rect(startX, startY - 26f, 200f, 24f), $"<color=#00e676>P1</color>  Advantage: <b>{advText}</b>", _textStyle);

            // P1 Bar
            for (int i = 0; i < HISTORY_LENGTH; i++)
            {
                int idx = (_historyIndex + i) % HISTORY_LENGTH;
                Color col = GetFrameColor(_p1History[idx]);
                DrawColoredRect(new Rect(startX + (i * barWidth), startY, barWidth - 1f, barHeight), col);
            }

            // P2 Bar
            for (int i = 0; i < HISTORY_LENGTH; i++)
            {
                int idx = (_historyIndex + i) % HISTORY_LENGTH;
                Color col = GetFrameColor(_p2History[idx]);
                DrawColoredRect(new Rect(startX + (i * barWidth), startY + barHeight + 4f, barWidth - 1f, barHeight), col);
            }
        }

        private void InitStyles()
        {
            if (_textStyle == null)
            {
                _textStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold
                };
                _textStyle.normal.textColor = Color.white;
            }
        }

        private Color GetFrameColor(FrameType type)
        {
            switch (type)
            {
                case FrameType.Startup:
                    return new Color(0.1f, 0.9f, 0.2f); // Green
                case FrameType.Active:
                    return new Color(1.0f, 0.15f, 0.15f); // Red
                case FrameType.Recovery:
                    return new Color(0.2f, 0.5f, 1.0f); // Blue
                case FrameType.Hitstun:
                case FrameType.Blockstun:
                    return new Color(1.0f, 0.85f, 0.1f); // Yellow
                case FrameType.Invulnerable:
                    return Color.white;
                default:
                    return new Color(0.15f, 0.15f, 0.15f, 0.8f); // Dark Grey
            }
        }

        private void DrawColoredRect(Rect rect, Color color)
        {
            Color oldColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = oldColor;
        }
    }
}
