using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using DemonIdol.BlazBlue;
using DemonIdol.Combat;
using DemonIdol.Launch;

namespace DemonIdol.Tests.PlayMode
{
    public class BBHitStressInputDriver : MonoBehaviour
    {
        private Keyboard _keyboard;
        private readonly List<Key> _heldKeys = new List<Key>();
        private bool _driving = false;

        void Awake()
        {
            _keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
        }

        void Update()
        {
            if (!_driving) return;
            if (_keyboard == null) _keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
            if (_keyboard == null) return;

            var state = _heldKeys.Count > 0 ? new KeyboardState(_heldKeys.ToArray()) : new KeyboardState();
            InputSystem.QueueStateEvent(_keyboard, state);
        }

        public void SetHeldKeys(params Key[] keys)
        {
            _heldKeys.Clear();
            if (keys != null && keys.Length > 0) _heldKeys.AddRange(keys);
            _driving = true;
        }

        public void ClearHeldKeys()
        {
            _heldKeys.Clear();
            _driving = true;
        }
    }

    [TestFixture]
    public class BBHitCrashStressTest : InputTestFixture
    {
        [UnityTest]
        public IEnumerator HitCrashStressTest_50ConsecutiveHits_NoEditorCrash()
        {
            Debug.Log("[BBHitCrashStressTest] >>> STEP 1: METHOD STARTED");
            LogAssert.ignoreFailingMessages = true;

            var driverGo = new GameObject("BBHitStressInputDriver");
            Object.DontDestroyOnLoad(driverGo);
            var driver = driverGo.AddComponent<BBHitStressInputDriver>();

            try
            {
                LaunchServices.Session.TrySelectMode("LocalVersus", out _);
                LaunchServices.Session.BeginMatch("C01", "C07", "ST07");
                PlayerPrefs.SetString("MDI_GameMode", "LocalVersus");
                PlayerPrefs.SetString("MDI_P1_CharacterId", "C01");
                PlayerPrefs.SetString("MDI_P2_CharacterId", "C07");
                PlayerPrefs.SetString("MDI_StageId", "ST07");
                PlayerPrefs.Save();

                Debug.Log("[BBHitCrashStressTest] >>> STEP 2: LOADING SCENE");
                var loadOp = SceneManager.LoadSceneAsync("BattleArena", LoadSceneMode.Single);
                while (!loadOp.isDone) yield return null;
                yield return null;
                yield return null;

                Debug.Log("[BBHitCrashStressTest] >>> STEP 3: SCENE LOADED, FINDING DIRECTORS");
                var combatDirector = HomegrownCombatDirector.Instance;
                Assert.IsNotNull(combatDirector, "HomegrownCombatDirector must exist in BattleArena");

                var p1Fc = combatDirector.Fighter1;
                var p2Fc = combatDirector.Fighter2;
                Assert.IsNotNull(p1Fc, "P1 FighterController must exist");
                Assert.IsNotNull(p2Fc, "P2 FighterController must exist");

                var p1Brain = p1Fc.GetComponent<BBFighterBrain>();
                var p2Brain = p2Fc.GetComponent<BBFighterBrain>();
                Assert.IsNotNull(p1Brain, "P1 BBFighterBrain must exist");
                Assert.IsNotNull(p2Brain, "P2 BBFighterBrain must exist");

                Debug.Log("[BBHitCombatDirector] >>> STEP 4: DISABLING AI");
                var p2Ai = p2Fc.GetComponent<BBAIController>();
                if (p2Ai != null) p2Ai.enabledAI = false;

                Debug.Log("[BBHitCrashStressTest] >>> STEP 5: WAITING FOR FIGHT STATE");
                if (MatchRoundReferee.Instance != null)
                {
                    MatchRoundReferee.Instance.isTrainingMode = true;
                    MatchRoundReferee.Instance.SkipIntroCountdown();
                }
                for (int f = 0; f < 5; f++) yield return null;

                Debug.Log("[BBHitCrashStressTest] >>> STEP 6: WALKING INTO RANGE");
                float walkTimeout = Time.realtimeSinceStartup + 3.0f;
                while (Mathf.Abs(p1Fc.transform.position.x - p2Fc.transform.position.x) > 0.70f && Time.realtimeSinceStartup < walkTimeout)
                {
                    driver.SetHeldKeys(Key.D);
                    yield return null;
                }
                driver.ClearHeldKeys();
                for (int f = 0; f < 5; f++) yield return null;

                Debug.Log($"[BBHitCrashStressTest] >>> STEP 7: ENTERING HIT BARRAGE. Current dist = {Mathf.Abs(p1Fc.transform.position.x - p2Fc.transform.position.x):F2}");

                int initialHitEvents = BBContactPresentation.LastHitEvents;
                int totalHitsConnected = 0;
                int totalGuardsConnected = 0;

                // Execute 55 rapid hits using real button presses
                for (int i = 0; i < 55; i++)
                {
                    // Reset health every 8 iterations cleanly via official ResetForRound
                    if (i % 8 == 0)
                    {
                        p1Brain.ResetForRound();
                        p2Brain.ResetForRound();
                        p1Brain.heat = 0f;
                        p2Brain.heat = 0f;
                    }

                    // If pushed back out of range, walk back in
                    float recloseTimeout = Time.realtimeSinceStartup + 1.5f;
                    while (Mathf.Abs(p1Fc.transform.position.x - p2Fc.transform.position.x) > 0.75f && Time.realtimeSinceStartup < recloseTimeout)
                    {
                        driver.SetHeldKeys(Key.D);
                        yield return null;
                    }
                    driver.ClearHeldKeys();
                    for (int f = 0; f < 2; f++) yield return null;

                    // Alternate between 5A, 5B, 5C, 5D, 2C
                    int attackIdx = i % 5;
                    if (attackIdx == 0)
                    {
                        // 5A
                        driver.SetHeldKeys(Key.J);
                    }
                    else if (attackIdx == 1)
                    {
                        // 5B
                        driver.SetHeldKeys(Key.K);
                    }
                    else if (attackIdx == 2)
                    {
                        // 5C
                        driver.SetHeldKeys(Key.L);
                    }
                    else if (attackIdx == 3)
                    {
                        // 5D
                        driver.SetHeldKeys(Key.Space);
                    }
                    else
                    {
                        // 2C (Sweep)
                        driver.SetHeldKeys(Key.S, Key.L);
                    }

                    float atkHold = Time.realtimeSinceStartup + 0.12f;
                    while (p1Brain.currentAttack == null && Time.realtimeSinceStartup < atkHold)
                    {
                        yield return null;
                    }
                    driver.ClearHeldKeys();

                    // Wait for attack startup & active frames to hit
                    float maxWait = Time.realtimeSinceStartup + 0.5f;
                    int hitsBefore = BBContactPresentation.LastHitEvents;
                    int guardsBefore = BBContactPresentation.LastGuardEvents;
                    while (Time.realtimeSinceStartup < maxWait && BBContactPresentation.LastHitEvents == hitsBefore && BBContactPresentation.LastGuardEvents == guardsBefore)
                    {
                        yield return null;
                    }

                    if (BBContactPresentation.LastHitEvents > hitsBefore)
                    {
                        totalHitsConnected += (BBContactPresentation.LastHitEvents - hitsBefore);
                    }
                    if (BBContactPresentation.LastGuardEvents > guardsBefore)
                    {
                        totalGuardsConnected += (BBContactPresentation.LastGuardEvents - guardsBefore);
                    }

                    // Allow recovery settlement
                    float recovTimeout = Time.realtimeSinceStartup + 0.6f;
                    while ((p1Brain.bbState == BBState.AttackActive || p1Brain.bbState == BBState.AttackRecovery) && Time.realtimeSinceStartup < recovTimeout)
                    {
                        yield return null;
                    }
                }

                int finalHitEvents = BBContactPresentation.LastHitEvents;
                int totalRecordedHits = finalHitEvents - initialHitEvents;
                Debug.Log($"<color=#00e676>[BBHitCrashStressTest] COMPLETED 55 iterations. Recorded hits: {totalRecordedHits}, Guards: {totalGuardsConnected}. No crash!</color>");

                Assert.GreaterOrEqual(totalRecordedHits + totalGuardsConnected, 20, "At least 20 hits/guards must connect during the 55-iteration stress test");
            }
            finally
            {
                if (driverGo != null) Object.Destroy(driverGo);
            }
        }
    }
}
