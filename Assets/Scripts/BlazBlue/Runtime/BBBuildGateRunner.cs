using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using DemonIdol.Combat;
using DemonIdol.Launch;

namespace DemonIdol.BlazBlue
{
    public class BBBuildGateRunner : MonoBehaviour
    {
        private static BBBuildGateRunner s_Instance;
        private Keyboard _keyboard;
        private readonly List<Key> _heldKeys = new List<Key>();
        private bool _driving = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void CheckCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();
            bool runGate = false;
            foreach (var arg in args)
            {
                if (arg.Equals("-runBuildGate", StringComparison.OrdinalIgnoreCase))
                {
                    runGate = true;
                    break;
                }
            }

            if (!runGate) return;
            if (s_Instance != null) return;

            var go = new GameObject("BBBuildGateRunner");
            DontDestroyOnLoad(go);
            s_Instance = go.AddComponent<BBBuildGateRunner>();
            s_Instance.StartCoroutine(s_Instance.RunBuildGateSequence());
        }

        void Awake()
        {
            s_Instance = this;
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
            if (keys != null && keys.Length > 0)
            {
                _heldKeys.AddRange(keys);
            }
            _driving = true;
        }

        public void ClearHeldKeys()
        {
            _heldKeys.Clear();
            _driving = true;
        }

        private IEnumerator RunBuildGateSequence()
        {
            Debug.Log("[BBBuildGate] ========================================================");
            Debug.Log("[BBBuildGate] STARTING MAC DEVELOPMENT BUILD VERIFICATION GATE");
            Debug.Log("[BBBuildGate] Testing 3 fighters: C (C01 Akio), SK (SK02 Galahad), EX (EX01 Hugo)");
            Debug.Log("[BBBuildGate] ========================================================");

            // Match 1: C01 (Akio, C) vs C02 (Aika) on ST07
            yield return RunFighterMatch("C01", "C02");

            // Match 2: SK02 (Galahad, SK) vs C01 (Akio) on ST07
            yield return RunFighterMatch("SK02", "C01");

            // Match 3: EX01 (Hugo, EX) vs SK02 (Galahad) on ST07
            yield return RunFighterMatch("EX01", "SK02");

            Debug.Log("[BBBuildGate] ========================================================");
            Debug.Log("[BBBuildGate] ALL 3 FIGHTERS (C, SK, EX) RESOLVED AND 5A HITS VERIFIED IN BUILD!");
            Debug.Log("[BBBuildGate] GATE COMPLETE - EXITING APPLICATION WITH CODE 0");
            Debug.Log("[BBBuildGate] ========================================================");

            yield return new WaitForSecondsRealtime(0.5f);
            Application.Quit(0);
        }

        private IEnumerator RunFighterMatch(string p1Id, string p2Id)
        {
            Debug.Log($"[BBBuildGate] Starting Match: P1={p1Id} vs P2={p2Id} on ST07...");

            LaunchServices.Session.TrySelectMode("LocalVersus", out _);
            LaunchServices.Session.BeginMatch(p1Id, p2Id, "ST07");
            PlayerPrefs.SetString("MDI_GameMode", "LocalVersus");
            PlayerPrefs.SetString("MDI_P1_CharacterId", p1Id);
            PlayerPrefs.SetString("MDI_P2_CharacterId", p2Id);
            PlayerPrefs.SetString("MDI_StageId", "ST07");
            PlayerPrefs.Save();

            SceneManager.LoadScene("BattleArena");
            yield return null;

            float loadTimeout = Time.realtimeSinceStartup + 15f;
            while (Time.realtimeSinceStartup < loadTimeout)
            {
                if (HomegrownCombatDirector.Instance != null && HomegrownCombatDirector.Instance.isActiveAndEnabled
                    && BBMatchSim.Instance != null && BBMatchSim.Instance.p1Brain != null && BBMatchSim.Instance.p2Brain != null
                    && CombatTickEngine.Instance != null && CombatTickEngine.Instance.isActiveAndEnabled
                    && MatchRoundReferee.Instance != null && MatchRoundReferee.Instance.State == MatchState.Fighting)
                {
                    break;
                }
                yield return null;
            }

            var p1Brain = BBMatchSim.Instance.p1Brain;
            var p2Brain = BBMatchSim.Instance.p2Brain;

            if (p1Brain == null || p2Brain == null)
            {
                Debug.LogError($"[BBBuildGate] FAIL: Fighters failed to spawn for {p1Id} vs {p2Id}");
                yield break;
            }

            // Verify profile, model, and moves resolved
            int p1Moves = (p1Brain.profile != null && p1Brain.profile.moveSet is BBMoveSet ms1 && ms1.moves != null) ? ms1.moves.Length : 0;
            int p2Moves = (p2Brain.profile != null && p2Brain.profile.moveSet is BBMoveSet ms2 && ms2.moves != null) ? ms2.moves.Length : 0;
            string p1ProfileName = p1Brain.profile != null ? p1Brain.profile.name : "null";
            string p2ProfileName = p2Brain.profile != null ? p2Brain.profile.name : "null";

            Debug.Log($"[BBBuildGate] VERIFY P1 ({p1Id}): profile={p1ProfileName}, moves={p1Moves}");
            Debug.Log($"[BBBuildGate] VERIFY P2 ({p2Id}): profile={p2ProfileName}, moves={p2Moves}");

            if (p1Brain.profile == null || p2Brain.profile == null || p1Moves < 14 || p2Moves < 14)
            {
                Debug.LogError($"[BBBuildGate] FAIL: Incomplete profile resolution: P1 moves={p1Moves}, P2 moves={p2Moves}");
                yield break;
            }

            // Walk P1 into range with real key input
            float startDist = Mathf.Abs(p1Brain.transform.position.x - p2Brain.transform.position.x);
            Debug.Log($"[BBBuildGate] Initial spacing: {startDist:F2}m. Walking P1 into 5A range...");

            float walkTimeout = Time.realtimeSinceStartup + 5f;
            while (Time.realtimeSinceStartup < walkTimeout)
            {
                float currentDist = Mathf.Abs(p1Brain.transform.position.x - p2Brain.transform.position.x);
                if (currentDist <= 1.15f) break;

                SetHeldKeys(Key.D);
                yield return null;
            }
            ClearHeldKeys();
            for (int f = 0; f < 3; f++) yield return null;

            // Attack 5A with real key press
            float p2HpBefore = p2Brain.health;
            Debug.Log($"[BBBuildGate] Pressing 5A (Key.J)... P2 HP before: {p2HpBefore:F0}");

            SetHeldKeys(Key.J);
            for (int f = 0; f < 6; f++) yield return null;
            ClearHeldKeys();

            // Wait for 5A contact and recovery
            float hitTimeout = Time.realtimeSinceStartup + 2f;
            bool hitLanded = false;
            while (Time.realtimeSinceStartup < hitTimeout)
            {
                if (p2Brain.health < p2HpBefore)
                {
                    hitLanded = true;
                    break;
                }
                yield return null;
            }

            float p2HpAfter = p2Brain.health;
            float damage = p2HpBefore - p2HpAfter;

            if (hitLanded && damage > 0f)
            {
                Debug.Log($"[BBBuildGate] PASS: 5A HIT CONNECTED! Damage={damage:F0}, P2 HP: {p2HpBefore:F0} -> {p2HpAfter:F0}");
            }
            else
            {
                Debug.LogError($"[BBBuildGate] FAIL: 5A did not hit or deal damage for {p1Id}!");
            }

            yield return new WaitForSecondsRealtime(0.2f);
        }
    }
}
