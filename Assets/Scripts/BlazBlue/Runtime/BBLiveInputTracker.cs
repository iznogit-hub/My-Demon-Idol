using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// BlazBlue Training-Mode Live Input Tracker HUD.
    /// Toggle ON/OFF with F2 (Default: OFF).
    /// Toggle CSV session logging to Logs/input_session_<date>.csv with F3.
    /// </summary>
    public class BBLiveInputTracker : MonoBehaviour
    {
        public static BBLiveInputTracker Instance { get; private set; }

        public bool isVisible = false;
        public bool isLogging = false;

        private string _logFilePath = "";
        private StreamWriter _logWriter = null;

        public struct HistoryEntry
        {
            public sbyte numpad;
            public byte buttons;
            public int durationFrames;
            public string commandName;

            public override string ToString()
            {
                string dir = DirectionArrow(numpad);
                string btns = ButtonString(buttons);
                string str = $"[{dir}]";
                if (!string.IsNullOrEmpty(btns)) str += $" + {btns}";
                str += $" ({durationFrames}f)";
                if (!string.IsNullOrEmpty(commandName)) str += $" -> {commandName}";
                return str;
            }
        }

        public class PlayerTracker
        {
            public int playerIndex; // 0 = P1, 1 = P2
            public BBFighterBrain brain;
            public sbyte lastNumpad = 5;
            public byte lastButtons = 0;
            public int currentHoldDuration = 0;
            public string lastCommandName = "";
            public string lastExecutedMoveId = "";
            public List<HistoryEntry> history = new List<HistoryEntry>(20);

            public void UpdateInput(int tick)
            {
                if (brain == null) return;

                int facing = brain.fighter != null ? brain.fighter.FacingDirection : (playerIndex == 0 ? 1 : -1);
                var latest = brain.History.LatestFrame;
                sbyte facingNumpad = BBInputFrame.WorldToFacingNumpad(latest.numpad, facing);
                byte held = latest.held;

                string activeCmd = brain.CurrentCommand.IsValid ? brain.CurrentCommand.commandType.ToString() : "";

                if (facingNumpad == lastNumpad && held == lastButtons)
                {
                    currentHoldDuration++;
                }
                else
                {
                    if (currentHoldDuration > 0)
                    {
                        history.Insert(0, new HistoryEntry
                        {
                            numpad = lastNumpad,
                            buttons = lastButtons,
                            durationFrames = currentHoldDuration,
                            commandName = lastCommandName
                        });
                        if (history.Count > 20) history.RemoveAt(history.Count - 1);
                    }

                    lastNumpad = facingNumpad;
                    lastButtons = held;
                    currentHoldDuration = 1;
                    lastCommandName = activeCmd;
                }

                if (!string.IsNullOrEmpty(activeCmd))
                {
                    lastCommandName = activeCmd;
                }
            }
        }

        private PlayerTracker _p1 = new PlayerTracker { playerIndex = 0 };
        private PlayerTracker _p2 = new PlayerTracker { playerIndex = 1 };

        private GUIStyle _panelStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _textStyle;
        private GUIStyle _phaseStyle;
        private GUIStyle _historyStyle;
        private bool _stylesInitialized = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null)
            {
                var go = new GameObject("BBLiveInputTracker");
                Instance = go.AddComponent<BBLiveInputTracker>();
                DontDestroyOnLoad(go);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.f2Key.wasPressedThisFrame)
                {
                    isVisible = !isVisible;
                    Debug.Log($"[BBLiveInputTracker] F2 pressed. Display is now {(isVisible ? "ON" : "OFF")}.");
                }

                if (kb.f3Key.wasPressedThisFrame)
                {
                    ToggleLogging();
                }
            }

            ResolveBrains();

            int tick = CombatTickEngine.Instance != null ? CombatTickEngine.Instance.currentTick : Time.frameCount;
            _p1.UpdateInput(tick);
            _p2.UpdateInput(tick);

            if (isLogging)
            {
                CheckLogMoveExecution(tick, _p1);
                CheckLogMoveExecution(tick, _p2);
            }
        }

        private void ResolveBrains()
        {
            if (BBMatchSim.Instance != null)
            {
                _p1.brain = BBMatchSim.Instance.p1Brain;
                _p2.brain = BBMatchSim.Instance.p2Brain;
            }
            else
            {
                var brains = FindObjectsByType<BBFighterBrain>(FindObjectsSortMode.None);
                if (brains != null && brains.Length > 0)
                {
                    if (_p1.brain == null && brains.Length >= 1) _p1.brain = brains[0];
                    if (_p2.brain == null && brains.Length >= 2) _p2.brain = brains[1];
                }
            }
        }

        private void ToggleLogging()
        {
            isLogging = !isLogging;
            if (isLogging)
            {
                string dir = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                string dateStr = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                _logFilePath = Path.Combine(dir, $"input_session_{dateStr}.csv");
                _logWriter = new StreamWriter(_logFilePath, false, Encoding.UTF8);
                _logWriter.WriteLine("tick,player,fighter,input,command,moveId,clip,result");
                _logWriter.Flush();
                Debug.Log($"<color=#00e676>[BBLiveInputTracker] F3 Logging STARTED -> {_logFilePath}</color>");
            }
            else
            {
                if (_logWriter != null)
                {
                    _logWriter.Flush();
                    _logWriter.Close();
                    _logWriter = null;
                }
                Debug.Log($"<color=#ff9800>[BBLiveInputTracker] F3 Logging STOPPED.</color>");
            }
        }

        private void CheckLogMoveExecution(int tick, PlayerTracker pt)
        {
            if (_logWriter == null || pt.brain == null) return;

            string currentMove = pt.brain.currentAttack != null ? pt.brain.currentAttack.moveId : "";
            if (!string.IsNullOrEmpty(currentMove) && currentMove != pt.lastExecutedMoveId)
            {
                pt.lastExecutedMoveId = currentMove;
                string fighter = pt.brain.fighter != null ? pt.brain.fighter.fighterName : "Unknown";
                string inputStr = DirectionArrow(pt.lastNumpad) + " " + ButtonString(pt.lastButtons);
                string cmd = pt.brain.CurrentCommand.IsValid ? pt.brain.CurrentCommand.commandType.ToString() : currentMove;
                string clip = pt.brain.currentAttack != null && pt.brain.currentAttack.clip != null ? pt.brain.currentAttack.clip.name : "";
                string result = pt.brain.attackHitMask.Count > 0 ? "Hit" : "Active";

                _logWriter.WriteLine($"{tick},P{pt.playerIndex + 1},{fighter},{inputStr},{cmd},{currentMove},{clip},{result}");
                _logWriter.Flush();
            }
            else if (string.IsNullOrEmpty(currentMove))
            {
                pt.lastExecutedMoveId = "";
            }
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            _panelStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 11,
                padding = new RectOffset(10, 10, 8, 8)
            };

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.yellow }
            };

            _textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = Color.white }
            };

            _phaseStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };

            _historyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
            };

            _stylesInitialized = true;
        }

        private void OnGUI()
        {
            if (!isVisible) return;
            InitStyles();

            float panelW = 320f;
            float panelH = 460f;
            float margin = 16f;

            // P1 Panel (Left)
            DrawPlayerPanel(new Rect(margin, 90f, panelW, panelH), _p1, "PLAYER 1");

            // P2 Panel (Right)
            DrawPlayerPanel(new Rect(Screen.width - panelW - margin, 90f, panelW, panelH), _p2, "PLAYER 2");

            // Central Header Pill
            string status = $"[F2] LIVE INPUT TRACKER  |  [F3] LOGGING: {(isLogging ? "<color=#00e676>ACTIVE</color>" : "<color=#888888>OFF</color>")}";
            GUI.Box(new Rect((Screen.width - 380f) * 0.5f, 60f, 380f, 26f), status);
        }

        private void DrawPlayerPanel(Rect rect, PlayerTracker pt, string label)
        {
            GUI.Box(rect, GUIContent.none);
            GUILayout.BeginArea(new Rect(rect.x + 8, rect.y + 6, rect.width - 16, rect.height - 12));

            string fighterName = pt.brain != null && pt.brain.fighter != null ? pt.brain.fighter.fighterName : "None";
            GUILayout.Label($"{label}: {fighterName}", _headerStyle);

            if (pt.brain != null)
            {
                // Raw buttons and numpad
                sbyte facingNumpad = pt.lastNumpad;
                string dirArrow = DirectionArrow(facingNumpad);
                string buttons = ButtonString(pt.lastButtons);
                GUILayout.Label($"<b>Direction:</b> {dirArrow} ({facingNumpad})   <b>Buttons:</b> {(string.IsNullOrEmpty(buttons) ? "None" : buttons)}", _textStyle);

                // State & Phase
                Color phaseColor = Color.gray;
                string phaseName = "Neutral";
                switch (pt.brain.bbState)
                {
                    case BBState.AttackStartup:
                        phaseName = $"STARTUP (f{pt.brain.attackTick})";
                        phaseColor = new Color(0.29f, 0.85f, 0.39f); // Green
                        break;
                    case BBState.AttackActive:
                        phaseName = $"ACTIVE (f{pt.brain.attackTick})";
                        phaseColor = new Color(0.95f, 0.26f, 0.21f); // Red
                        break;
                    case BBState.AttackRecovery:
                        phaseName = $"RECOVERY (f{pt.brain.attackTick})";
                        phaseColor = new Color(0.13f, 0.59f, 0.95f); // Blue
                        break;
                    case BBState.Hitstun:
                    case BBState.Blockstun:
                    case BBState.BarrierBlockstun:
                        phaseName = $"{pt.brain.bbState}";
                        phaseColor = new Color(1.0f, 0.6f, 0.0f); // Orange
                        break;
                    default:
                        phaseName = pt.brain.bbState.ToString();
                        phaseColor = Color.gray;
                        break;
                }

                _phaseStyle.normal.textColor = phaseColor;
                GUILayout.Label($"<b>State:</b> {pt.brain.bbState}   <b>Phase:</b> {phaseName}", _phaseStyle);

                // Command & Move
                string cmdName = pt.brain.CurrentCommand.IsValid ? pt.brain.CurrentCommand.commandType.ToString() : "None";
                string moveId = pt.brain.currentAttack != null ? pt.brain.currentAttack.moveId : "(none)";
                string clip = pt.brain.currentAttack != null && pt.brain.currentAttack.clip != null ? pt.brain.currentAttack.clip.name : "(none)";
                GUILayout.Label($"<b>Command:</b> {cmdName}   <b>MoveId:</b> {moveId}", _textStyle);
                GUILayout.Label($"<b>Clip:</b> {clip}", _textStyle);

                GUILayout.Space(4);
                GUILayout.Label("<b>Input History (Last 20):</b>", _headerStyle);

                int showCount = Mathf.Min(pt.history.Count, 12);
                for (int i = 0; i < showCount; i++)
                {
                    GUILayout.Label($" {i + 1:D2}. {pt.history[i]}", _historyStyle);
                }
            }
            else
            {
                GUILayout.Label("Waiting for fighter spawn...", _textStyle);
            }

            GUILayout.EndArea();
        }

        private static string DirectionArrow(sbyte numpad)
        {
            switch (numpad)
            {
                case 1: return "↙ 1";
                case 2: return "↓ 2";
                case 3: return "↘ 3";
                case 4: return "← 4";
                case 5: return "• 5";
                case 6: return "→ 6";
                case 7: return "↖ 7";
                case 8: return "↑ 8";
                case 9: return "↗ 9";
                default: return "• 5";
            }
        }

        private static string ButtonString(byte mask)
        {
            var sb = new StringBuilder();
            if ((mask & (byte)BBButton.A) != 0) sb.Append("A ");
            if ((mask & (byte)BBButton.B) != 0) sb.Append("B ");
            if ((mask & (byte)BBButton.C) != 0) sb.Append("C ");
            if ((mask & (byte)BBButton.D) != 0) sb.Append("D ");
            if ((mask & (byte)BBButton.Taunt) != 0) sb.Append("Taunt ");
            return sb.ToString().TrimEnd();
        }

        private void OnDestroy()
        {
            if (_logWriter != null)
            {
                _logWriter.Flush();
                _logWriter.Close();
                _logWriter = null;
            }
        }
    }
}
