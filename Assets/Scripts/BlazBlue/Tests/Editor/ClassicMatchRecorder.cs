#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using DemonIdol.Combat;

namespace DemonIdol.BlazBlue.Tests
{
    [Serializable]
    public class ClassicTickRecord
    {
        public int tick;
        public float p1MoveX;
        public float p1MoveY;
        public int p1Button;
        public float p1BufMoveX;
        public float p1BufMoveY;

        public float p2MoveX;
        public float p2MoveY;
        public int p2Button;
        public float p2BufMoveX;
        public float p2BufMoveY;

        public float p1PosX;
        public float p1PosY;
        public float p1Health;
        public float p1Super;
        public string p1State;
        public int p1Facing;

        public float p2PosX;
        public float p2PosY;
        public float p2Health;
        public float p2Super;
        public string p2State;
        public int p2Facing;

        public uint stateHash;
    }

    [Serializable]
    public class ClassicMatchLog
    {
        public string scenarioName;
        public string p1Character;
        public string p2Character;
        public Vector3 startP1Pos;
        public Vector3 startP2Pos;
        public float startP1Super;
        public float startP2Super;
        public int totalTicks;
        public List<ClassicTickRecord> ticks = new List<ClassicTickRecord>();
    }

    [InitializeOnLoad]
    public static class ClassicMatchRecorder
    {
        // Auto-run test suites on compilation
        public const string GOLDEN_DIR = "Assets/Scripts/BlazBlue/Tests/Golden";

        static ClassicMatchRecorder()
        {
            CheckAndRecord();
        }

        private static void CheckAndRecord()
        {
            try
            {
                BBMotionMatcherTests.RunAllTestsMenu();
                BBCollisionTests.RunAllTestsMenu();
                BBSubsystemTests.RunAllTestsMenu();
                BBAkioTests.RunAllTestsMenu();
                ClassicRegressionTests.RunAllTestsMenu();

                if (!Directory.Exists(GOLDEN_DIR) || Directory.GetFiles(GOLDEN_DIR, "*.classicrec").Length < 6)
                {
                    RecordAllGoldenMatches();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ClassicMatchRecorder] Failed during recording: {ex}");
            }
        }

        private static readonly MethodInfo HandleCombatTickMethod =
            typeof(FighterController).GetMethod("HandleCombatTick", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly MethodInfo AwakeMethod =
            typeof(FighterController).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly FieldInfo CharControllerField =
            typeof(FighterController).GetField("_charController", BindingFlags.NonPublic | BindingFlags.Instance);

        public static uint ComputeStateHash(FighterController p1, FighterController p2, int tick)
        {
            unchecked
            {
                uint hash = 2166136261;
                hash = (hash ^ (uint)tick) * 16777619;

                // P1
                Vector3 pos1 = p1.transform.position;
                hash = (hash ^ (uint)Mathf.RoundToInt(pos1.x * 1000f)) * 16777619;
                hash = (hash ^ (uint)Mathf.RoundToInt(pos1.y * 1000f)) * 16777619;
                hash = (hash ^ (uint)Mathf.RoundToInt(p1.currentHealth * 10f)) * 16777619;
                hash = (hash ^ (uint)Mathf.RoundToInt(p1.currentSuperMeter * 10f)) * 16777619;
                hash = (hash ^ (uint)p1.State.GetHashCode()) * 16777619;
                hash = (hash ^ (uint)p1.FacingDirection) * 16777619;

                // P2
                Vector3 pos2 = p2.transform.position;
                hash = (hash ^ (uint)Mathf.RoundToInt(pos2.x * 1000f)) * 16777619;
                hash = (hash ^ (uint)Mathf.RoundToInt(pos2.y * 1000f)) * 16777619;
                hash = (hash ^ (uint)Mathf.RoundToInt(p2.currentHealth * 10f)) * 16777619;
                hash = (hash ^ (uint)Mathf.RoundToInt(p2.currentSuperMeter * 10f)) * 16777619;
                hash = (hash ^ (uint)p2.State.GetHashCode()) * 16777619;
                hash = (hash ^ (uint)p2.FacingDirection) * 16777619;

                return hash;
            }
        }

        public static void SetFighterPosition(FighterController fc, CharacterController cc, Vector3 pos)
        {
            if (cc != null) cc.enabled = false;
            fc.transform.position = pos;
            Physics.SyncTransforms();
            if (cc != null) cc.enabled = true;
        }

        public class SimScope : IDisposable
        {
            private readonly List<Collider> _disabledColliders = new List<Collider>();
            public GameObject groundGo;
            public GameObject p1Go;
            public FighterController p1;
            public GameObject p2Go;
            public FighterController p2;

            public SimScope(string p1Char, string p2Char)
            {
                // Disable any scene colliders during simulation to guarantee hermetic environment
                var allCols = UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None);
                foreach (var col in allCols)
                {
                    if (col != null && col.enabled)
                    {
                        col.enabled = false;
                        _disabledColliders.Add(col);
                    }
                }

                SetupFighters(p1Char, p2Char, out groundGo, out p1Go, out p1, out p2Go, out p2);
            }

            public void Dispose()
            {
                if (p1Go != null) UnityEngine.Object.DestroyImmediate(p1Go);
                if (p2Go != null) UnityEngine.Object.DestroyImmediate(p2Go);
                if (groundGo != null) UnityEngine.Object.DestroyImmediate(groundGo);

                foreach (var col in _disabledColliders)
                {
                    if (col != null) col.enabled = true;
                }
                _disabledColliders.Clear();
                Physics.SyncTransforms();
            }
        }

        public static void SetupFighters(string p1Char, string p2Char, out GameObject groundGo, out GameObject p1Go, out FighterController p1, out GameObject p2Go, out FighterController p2)
        {
            groundGo = new GameObject("Sim_Ground");
            var groundCol = groundGo.AddComponent<BoxCollider>();
            groundCol.center = new Vector3(0f, -0.5f, 0f);
            groundCol.size = new Vector3(100f, 1f, 100f);

            p1Go = new GameObject("Sim_P1");
            var cc1 = p1Go.AddComponent<CharacterController>();
            cc1.height = 1.8f;
            cc1.center = new Vector3(0f, 0.9f, 0f);
            cc1.radius = 0.35f;
            cc1.skinWidth = 0.0001f;
            cc1.minMoveDistance = 0f;
            p1 = p1Go.AddComponent<FighterController>();

            p2Go = new GameObject("Sim_P2");
            var cc2 = p2Go.AddComponent<CharacterController>();
            cc2.height = 1.8f;
            cc2.center = new Vector3(0f, 0.9f, 0f);
            cc2.radius = 0.35f;
            cc2.skinWidth = 0.0001f;
            cc2.minMoveDistance = 0f;
            p2 = p2Go.AddComponent<FighterController>();

            // Ensure Awake runs and CharacterController is cached
            AwakeMethod?.Invoke(p1, null);
            AwakeMethod?.Invoke(p2, null);
            CharControllerField?.SetValue(p1, cc1);
            CharControllerField?.SetValue(p2, cc2);

            p1.opponent = p2;
            p2.opponent = p1;

            SetFighterPosition(p1, cc1, new Vector3(-2.5f, 0f, 0f));
            SetFighterPosition(p2, cc2, new Vector3(2.5f, 0f, 0f));

            p1.SetFacing(1);
            p2.SetFacing(-1);

            // Apply sample preset movesets
            p1.ApplyMoveSet(MoveSet.CreateSamplePreset(p1Char));
            p2.ApplyMoveSet(MoveSet.CreateSamplePreset(p2Char));
        }

        public static void TickFighters(FighterController p1, FighterController p2, int tick)
        {
            HandleCombatTickMethod.Invoke(p1, new object[] { tick });
            HandleCombatTickMethod.Invoke(p2, new object[] { tick });
        }

        [MenuItem("Demon Idol/BlazBlue/Record 6 Classic Golden Matches")]
        public static void RecordAllGoldenMatches()
        {
            if (!Directory.Exists(GOLDEN_DIR))
            {
                Directory.CreateDirectory(GOLDEN_DIR);
            }

            RecordScenario1_AkioNeutralMobility();
            RecordScenario2_AkioNormalsChain();
            RecordScenario3_AkioSpecialsSuper();
            RecordScenario4_AkioVsAikaSkirmish();
            RecordScenario5_AikaVsUnityChanCombat();
            RecordScenario6_PicoChanThrowsAndBurst();

            bool allGreen = true;
            foreach (string file in Directory.GetFiles(GOLDEN_DIR, "*.classicrec"))
            {
                string json = File.ReadAllText(file);
                var log = JsonUtility.FromJson<ClassicMatchLog>(json);
                if (!ReplayMatch(log, out string err))
                {
                    Debug.LogError($"[ClassicMatchRecorder] Golden Replay Verification FAILED: {err}");
                    allGreen = false;
                }
            }

            if (allGreen)
            {
                BBMotionMatcherTests.RunAllTestsMenu();
                BBCollisionTests.RunAllTestsMenu();
                BBSubsystemTests.RunAllTestsMenu();
                BBAkioTests.RunAllTestsMenu();
                ClassicRegressionTests.RunAllTestsMenu();
                Debug.Log("<color=#00e676>[ClassicMatchRecorder] VERIFIED: All 6 Golden Replays replayed with 100% hash parity!</color>");
            }

            AssetDatabase.Refresh();
            Debug.Log("<color=#00e676>[ClassicMatchRecorder] Successfully recorded all 6 golden matches!</color>");
        }

        public static void RecordScenario1_AkioNeutralMobility()
        {
            using (var sim = new SimScope("C01_Akio", "Dummy"))
            {
                var p1 = sim.p1;
                var p2 = sim.p2;

                SetFighterPosition(p1, sim.p1Go.GetComponent<CharacterController>(), new Vector3(-2.5f, 0, 0));
                SetFighterPosition(p2, sim.p2Go.GetComponent<CharacterController>(), new Vector3(5.0f, 0, 0));

                var log = new ClassicMatchLog
                {
                    scenarioName = "match1_akio_neutral_mobility",
                    p1Character = "C01_Akio",
                    p2Character = "Dummy",
                    startP1Pos = p1.transform.position,
                    startP2Pos = p2.transform.position,
                    startP1Super = p1.currentSuperMeter,
                    startP2Super = p2.currentSuperMeter,
                    totalTicks = 240
                };

                for (int t = 1; t <= 240; t++)
                {
                    Vector2 move1 = Vector2.zero;
                    Vector2 bufMove1 = Vector2.zero;
                    InputButton btn1 = InputButton.None;

                    // Ticks 1-40: walk forward
                    if (t >= 1 && t <= 40) move1 = new Vector2(1f, 0f);
                    // Ticks 50-70: forward dash
                    else if (t == 50)
                    {
                        btn1 = InputButton.Light;
                        bufMove1 = Vector2.right;
                        p1.InputBuf.Enqueue(btn1, t, bufMove1);
                    }
                    // Ticks 90-130: jump forward
                    else if (t == 90) move1 = new Vector2(1f, 1f);
                    // Ticks 150-180: crouch
                    else if (t >= 150 && t <= 180) move1 = new Vector2(0f, -1f);
                    // Ticks 190-230: back walk
                    else if (t >= 190 && t <= 230) move1 = new Vector2(-1f, 0f);

                    p1.SetMoveInput(move1);
                    p2.SetMoveInput(Vector2.zero);

                    TickFighters(p1, p2, t);

                    log.ticks.Add(CreateTickRecord(t, move1, (int)btn1, bufMove1, Vector2.zero, 0, Vector2.zero, p1, p2));
                }

                SaveLog(log);
            }
        }

        public static void RecordScenario2_AkioNormalsChain()
        {
            using (var sim = new SimScope("C01_Akio", "Dummy"))
            {
                var p1 = sim.p1;
                var p2 = sim.p2;

                SetFighterPosition(p1, sim.p1Go.GetComponent<CharacterController>(), new Vector3(-0.8f, 0, 0));
                SetFighterPosition(p2, sim.p2Go.GetComponent<CharacterController>(), new Vector3(0.8f, 0, 0));

                var log = new ClassicMatchLog
                {
                    scenarioName = "match2_akio_normals_chain",
                    p1Character = "C01_Akio",
                    p2Character = "Dummy",
                    startP1Pos = p1.transform.position,
                    startP2Pos = p2.transform.position,
                    startP1Super = p1.currentSuperMeter,
                    startP2Super = p2.currentSuperMeter,
                    totalTicks = 240
                };

                for (int t = 1; t <= 240; t++)
                {
                    Vector2 m1 = Vector2.zero;
                    Vector2 bm1 = Vector2.zero;
                    InputButton btn1 = InputButton.None;
                    if (t == 10) btn1 = InputButton.Light;
                    else if (t == 45) btn1 = InputButton.Medium;
                    else if (t == 90) btn1 = InputButton.Heavy;
                    else if (t == 150)
                    {
                        m1 = new Vector2(0, -1);
                        bm1 = m1;
                        btn1 = InputButton.Light;
                    }

                    p1.SetMoveInput(m1);
                    p2.SetMoveInput(Vector2.zero);
                    if (btn1 != InputButton.None)
                    {
                        p1.InputBuf.Enqueue(btn1, t, bm1);
                    }

                    TickFighters(p1, p2, t);

                    log.ticks.Add(CreateTickRecord(t, m1, (int)btn1, bm1, Vector2.zero, 0, Vector2.zero, p1, p2));
                }

                SaveLog(log);
            }
        }

        public static void RecordScenario3_AkioSpecialsSuper()
        {
            using (var sim = new SimScope("C01_Akio", "Dummy"))
            {
                var p1 = sim.p1;
                var p2 = sim.p2;

                p1.AddSuperMeter(100f);

                var log = new ClassicMatchLog
                {
                    scenarioName = "match3_akio_specials_super",
                    p1Character = "C01_Akio",
                    p2Character = "Dummy",
                    startP1Pos = p1.transform.position,
                    startP2Pos = p2.transform.position,
                    startP1Super = p1.currentSuperMeter,
                    startP2Super = p2.currentSuperMeter,
                    totalTicks = 240
                };

                for (int t = 1; t <= 240; t++)
                {
                    InputButton btn1 = InputButton.None;
                    if (t == 20) btn1 = InputButton.DriveSkill;
                    else if (t == 100) btn1 = InputButton.DomainSuper;

                    p1.SetMoveInput(Vector2.zero);
                    p2.SetMoveInput(Vector2.zero);

                    if (btn1 != InputButton.None)
                    {
                        p1.InputBuf.Enqueue(btn1, t, Vector2.zero);
                    }

                    TickFighters(p1, p2, t);

                    log.ticks.Add(CreateTickRecord(t, Vector2.zero, (int)btn1, Vector2.zero, Vector2.zero, 0, Vector2.zero, p1, p2));
                }

                SaveLog(log);
            }
        }

        public static void RecordScenario4_AkioVsAikaSkirmish()
        {
            using (var sim = new SimScope("C01_Akio", "C02_Aika"))
            {
                var p1 = sim.p1;
                var p2 = sim.p2;

                SetFighterPosition(p1, sim.p1Go.GetComponent<CharacterController>(), new Vector3(-1.2f, 0, 0));
                SetFighterPosition(p2, sim.p2Go.GetComponent<CharacterController>(), new Vector3(1.2f, 0, 0));

                var log = new ClassicMatchLog
                {
                    scenarioName = "match4_akio_vs_aika_skirmish",
                    p1Character = "C01_Akio",
                    p2Character = "C02_Aika",
                    startP1Pos = p1.transform.position,
                    startP2Pos = p2.transform.position,
                    startP1Super = p1.currentSuperMeter,
                    startP2Super = p2.currentSuperMeter,
                    totalTicks = 240
                };

                for (int t = 1; t <= 240; t++)
                {
                    Vector2 m1 = Vector2.zero;
                    Vector2 m2 = Vector2.zero;
                    InputButton b1 = InputButton.None;
                    InputButton b2 = InputButton.None;

                    // P1 attacks at tick 15, P2 holds back to block
                    if (t >= 10 && t <= 60) m2 = new Vector2(-1f, 0f);
                    if (t == 15) b1 = InputButton.Heavy;

                    // Counter attack by P2 at tick 80
                    if (t == 80) b2 = InputButton.Medium;

                    if (b1 != InputButton.None) p1.InputBuf.Enqueue(b1, t, m1);
                    if (b2 != InputButton.None) p2.InputBuf.Enqueue(b2, t, m2);

                    p1.SetMoveInput(m1);
                    p2.SetMoveInput(m2);

                    TickFighters(p1, p2, t);

                    log.ticks.Add(CreateTickRecord(t, m1, (int)b1, m1, m2, (int)b2, m2, p1, p2));
                }

                SaveLog(log);
            }
        }

        public static void RecordScenario5_AikaVsUnityChanCombat()
        {
            using (var sim = new SimScope("C02_Aika", "C04_UnityChan"))
            {
                var p1 = sim.p1;
                var p2 = sim.p2;

                var log = new ClassicMatchLog
                {
                    scenarioName = "match5_aika_vs_unitychan_combat",
                    p1Character = "C02_Aika",
                    p2Character = "C04_UnityChan",
                    startP1Pos = p1.transform.position,
                    startP2Pos = p2.transform.position,
                    startP1Super = p1.currentSuperMeter,
                    startP2Super = p2.currentSuperMeter,
                    totalTicks = 240
                };

                for (int t = 1; t <= 240; t++)
                {
                    Vector2 m1 = (t <= 50) ? new Vector2(1f, 0f) : Vector2.zero;
                    InputButton b1 = (t == 60) ? InputButton.Heavy : InputButton.None;

                    if (b1 != InputButton.None) p1.InputBuf.Enqueue(b1, t, m1);
                    p1.SetMoveInput(m1);
                    p2.SetMoveInput(Vector2.zero);

                    TickFighters(p1, p2, t);

                    log.ticks.Add(CreateTickRecord(t, m1, (int)b1, m1, Vector2.zero, 0, Vector2.zero, p1, p2));
                }

                SaveLog(log);
            }
        }

        public static void RecordScenario6_PicoChanThrowsAndBurst()
        {
            using (var sim = new SimScope("C09_PicoChan", "Dummy"))
            {
                var p1 = sim.p1;
                var p2 = sim.p2;

                SetFighterPosition(p1, sim.p1Go.GetComponent<CharacterController>(), new Vector3(-0.5f, 0, 0));
                SetFighterPosition(p2, sim.p2Go.GetComponent<CharacterController>(), new Vector3(0.5f, 0, 0));

                var log = new ClassicMatchLog
                {
                    scenarioName = "match6_picochan_throws_and_burst",
                    p1Character = "C09_PicoChan",
                    p2Character = "Dummy",
                    startP1Pos = p1.transform.position,
                    startP2Pos = p2.transform.position,
                    startP1Super = p1.currentSuperMeter,
                    startP2Super = p2.currentSuperMeter,
                    totalTicks = 240
                };

                for (int t = 1; t <= 240; t++)
                {
                    InputButton b1 = InputButton.None;
                    InputButton b2 = InputButton.None;

                    if (t == 15) b1 = InputButton.Throw;
                    if (t == 22) b2 = InputButton.Burst; // Tech break attempt

                    p1.SetMoveInput(Vector2.zero);
                    p2.SetMoveInput(Vector2.zero);

                    if (b1 != InputButton.None) p1.InputBuf.Enqueue(b1, t, Vector2.zero);
                    if (b2 != InputButton.None) p2.InputBuf.Enqueue(b2, t, Vector2.zero);

                    TickFighters(p1, p2, t);

                    log.ticks.Add(CreateTickRecord(t, Vector2.zero, (int)b1, Vector2.zero, Vector2.zero, (int)b2, Vector2.zero, p1, p2));
                }

                SaveLog(log);
            }
        }

        private static ClassicTickRecord CreateTickRecord(
            int tick, Vector2 m1, int b1, Vector2 bm1, Vector2 m2, int b2, Vector2 bm2, FighterController p1, FighterController p2)
        {
            return new ClassicTickRecord
            {
                tick = tick,
                p1MoveX = m1.x,
                p1MoveY = m1.y,
                p1Button = b1,
                p1BufMoveX = bm1.x,
                p1BufMoveY = bm1.y,
                p2MoveX = m2.x,
                p2MoveY = m2.y,
                p2Button = b2,
                p2BufMoveX = bm2.x,
                p2BufMoveY = bm2.y,
                p1PosX = p1.transform.position.x,
                p1PosY = p1.transform.position.y,
                p1Health = p1.currentHealth,
                p1Super = p1.currentSuperMeter,
                p1State = p1.State.ToString(),
                p1Facing = p1.FacingDirection,
                p2PosX = p2.transform.position.x,
                p2PosY = p2.transform.position.y,
                p2Health = p2.currentHealth,
                p2Super = p2.currentSuperMeter,
                p2State = p2.State.ToString(),
                p2Facing = p2.FacingDirection,
                stateHash = ComputeStateHash(p1, p2, tick)
            };
        }

        private static void SaveLog(ClassicMatchLog log)
        {
            string path = Path.Combine(GOLDEN_DIR, $"{log.scenarioName}.classicrec");
            string json = JsonUtility.ToJson(log, prettyPrint: true);
            File.WriteAllText(path, json);
        }

        public static bool ReplayMatch(ClassicMatchLog log, out string mismatchDetail)
        {
            mismatchDetail = "";
            using (var sim = new SimScope(log.p1Character, log.p2Character))
            {
                var p1 = sim.p1;
                var p2 = sim.p2;

                SetFighterPosition(p1, sim.p1Go.GetComponent<CharacterController>(), log.startP1Pos);
                SetFighterPosition(p2, sim.p2Go.GetComponent<CharacterController>(), log.startP2Pos);
                p1.SetFacing(1);
                p2.SetFacing(-1);

                if (log.startP1Super > 0) p1.AddSuperMeter(log.startP1Super);
                if (log.startP2Super > 0) p2.AddSuperMeter(log.startP2Super);

                foreach (var rec in log.ticks)
                {
                    Vector2 m1 = new Vector2(rec.p1MoveX, rec.p1MoveY);
                    Vector2 m2 = new Vector2(rec.p2MoveX, rec.p2MoveY);
                    Vector2 bm1 = new Vector2(rec.p1BufMoveX, rec.p1BufMoveY);
                    Vector2 bm2 = new Vector2(rec.p2BufMoveX, rec.p2BufMoveY);

                    p1.SetMoveInput(m1);
                    p2.SetMoveInput(m2);

                    if (rec.p1Button != 0)
                        p1.InputBuf.Enqueue((InputButton)rec.p1Button, rec.tick, bm1);
                    if (rec.p2Button != 0)
                        p2.InputBuf.Enqueue((InputButton)rec.p2Button, rec.tick, bm2);

                    TickFighters(p1, p2, rec.tick);

                    uint currentHash = ComputeStateHash(p1, p2, rec.tick);
                    if (currentHash != rec.stateHash)
                    {
                        mismatchDetail = $"Tick {rec.tick} mismatch in {log.scenarioName}:\n" +
                            $"Expected Hash: {rec.stateHash}, Got: {currentHash}\n" +
                            $"P1 Pos: ({p1.transform.position.x:F4}, {p1.transform.position.y:F4}) vs ({rec.p1PosX:F4}, {rec.p1PosY:F4})\n" +
                            $"P1 HP: {p1.currentHealth} vs {rec.p1Health}, Super: {p1.currentSuperMeter} vs {rec.p1Super}\n" +
                            $"P1 State: {p1.State} vs {rec.p1State}, Facing: {p1.FacingDirection} vs {rec.p1Facing}\n" +
                            $"P2 Pos: ({p2.transform.position.x:F4}, {p2.transform.position.y:F4}) vs ({rec.p2PosX:F4}, {rec.p2PosY:F4})\n" +
                            $"P2 HP: {p2.currentHealth} vs {rec.p2Health}, Super: {p2.currentSuperMeter} vs {rec.p2Super}\n" +
                            $"P2 State: {p2.State} vs {rec.p2State}, Facing: {p2.FacingDirection} vs {rec.p2Facing}";
                        return false;
                    }
                }
                return true;
            }
        }
    }
}
#endif
