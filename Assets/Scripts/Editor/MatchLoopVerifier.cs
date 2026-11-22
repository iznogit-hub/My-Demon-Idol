#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Text;
using DemonIdol.BlazBlue;
using DemonIdol.Combat;
using DemonIdol.UI;
using DemonIdol.Launch;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace DemonIdol.Editor
{
    /// <summary>
    /// Automated Match Loop Verifier for Demon Idol Rebuild Step 6.
    /// Flow:
    /// a) MainMenu > click Arcade > CharacterSelect loads. (Captures 01_main_menu.png)
    /// b) Move cursor to C07, pick. CPU picks EX01. Press Confirm. (Captures 02_character_select.png)
    /// c) BattleArena loads. Verify: tick > 0, C07 and EX01 spawned, P1 moves on walk, jump, attack A.
    ///    (Captures 03_battle_arena_round_start.png and 04_battle_arena_mid_combo.png)
    /// d) Pause > CharacterSelect. (Captures 05_battle_arena_pause_menu.png and 06_battle_arena_results_screen.png)
    /// e) Pick SK02 vs C12, Versus mode, Confirm.
    /// f) BattleArena loads. Verify: SK02 vs C12 spawned and tick > 0.
    /// </summary>
    public static class MatchLoopVerifier
    {
        public const string RunKey = "MDI_MatchLoop_Run";
        public const string PhaseKey = "MDI_MatchLoop_Phase";
        public const string TRIGGER_FILE = "run_match_loop.trigger";
        const string MenuScenePath = "Assets/Scenes/MainMenu.unity";

        static MatchLoopVerifier()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            if (File.Exists(TRIGGER_FILE))
            {
                try { File.Delete(TRIGGER_FILE); } catch { }
                RunVerification();
            }
        }

        public static void RunVerification()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[MatchLoop] Exit Play Mode before running Demon Idol > Verify > Match Loop.");
                return;
            }

            EditorPrefs.SetBool(RunKey, true);
            EditorPrefs.SetString(PhaseKey, "arcade");
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScenePath);
            Debug.Log("<color=#00e676>[MatchLoop] Starting Automated Match Loop Verification: Arcade (C07 vs EX01), then Versus (SK02 vs C12)...</color>");
            EditorApplication.isPlaying = true;
        }

        [InitializeOnLoadMethod]
        static void ClearStartSceneWhenEditModeReturns()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                    EditorSceneManager.playModeStartScene = null;
            };
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AfterSceneLoad()
        {
            if (!EditorPrefs.GetBool(RunKey, false))
                return;
            if (UnityEngine.Object.FindAnyObjectByType<MatchLoopRunner>() != null)
                return;
            var go = new GameObject("MatchLoopRunner");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<MatchLoopRunner>();
        }
    }

    public sealed class MatchLoopRunner : MonoBehaviour
    {
        string _handled;
        bool _driving;
        bool _sawWalk;
        bool _sawJump;
        bool _sawAttack;
        Key[] _held = System.Array.Empty<Key>();
        readonly StringBuilder _log = new StringBuilder();

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            HandleScene(SceneManager.GetActiveScene().name);
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            HandleScene(scene.name);
        }

        void Update()
        {
            if (!_driving)
                return;
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                keyboard = InputSystem.AddDevice<Keyboard>();
            if (keyboard == null)
                return;
            KeyboardState state = _held != null && _held.Length > 0 ? new KeyboardState(_held) : new KeyboardState();
            InputSystem.QueueStateEvent(keyboard, state);
        }

        void HandleScene(string sceneName)
        {
            string phase = EditorPrefs.GetString(MatchLoopVerifier.PhaseKey, "");
            string key = phase + ":" + sceneName;
            if (_handled == key)
                return;
            _handled = key;

            if (sceneName == "MainMenu")
                StartCoroutine(LaunchFromMenu(phase));
            else if (sceneName == "CharacterSelect")
                StartCoroutine(PickFromCharacterSelect(phase));
            else if (sceneName == "BattleArena")
                StartCoroutine(ProbeMatch(phase));
        }

        IEnumerator LaunchFromMenu(string phase)
        {
            yield return new WaitForSecondsRealtime(0.5f);
            _log.AppendLine($"[MatchLoop] MainMenu loaded for phase '{phase}'");

            if (phase == "arcade")
            {
                _log.AppendLine("[MatchLoop] Capturing 01_main_menu.png...");
                yield return Capture("01_main_menu");
            }

            DemonIdolMainMenuController menu = UnityEngine.Object.FindAnyObjectByType<DemonIdolMainMenuController>();
            if (menu != null)
            {
                if (phase == "versus")
                {
                    _log.AppendLine("[MatchLoop] MainMenu: Clicking Versus...");
                    menu.OnVersusClicked();
                }
                else
                {
                    _log.AppendLine("[MatchLoop] MainMenu: Clicking Arcade...");
                    menu.OnArcadeClicked();
                }
            }
            else
            {
                string mode = phase == "versus" ? "LocalVersus" : "Arcade";
                LaunchServices.Session.TrySelectMode(mode, out _);
                PlayerPrefs.SetString("MDI_GameMode", mode);
                PlayerPrefs.Save();
                SceneManager.LoadScene("CharacterSelect");
            }
        }

        IEnumerator PickFromCharacterSelect(string phase)
        {
            yield return new WaitForSecondsRealtime(0.6f);
            _log.AppendLine($"[MatchLoop] CharacterSelect loaded for phase '{phase}'");

            string p1Id = phase == "versus" ? "SK02" : "C07";
            string p2Id = phase == "versus" ? "C12" : "EX01";

            DemonIdolCharacterSelectManager charSelect = UnityEngine.Object.FindAnyObjectByType<DemonIdolCharacterSelectManager>();
            if (charSelect != null)
            {
                charSelect.isVersus = (phase == "versus");
                int p1Idx = Array.IndexOf(DemonIdolCharacterSelectManager.RosterIds, p1Id);
                int p2Idx = Array.IndexOf(DemonIdolCharacterSelectManager.RosterIds, p2Id);
                if (p1Idx >= 0) charSelect.p1Index = p1Idx;
                if (p2Idx >= 0) charSelect.p2Index = p2Idx;
                charSelect.UpdateHighlights();

                if (phase == "arcade")
                {
                    yield return new WaitForSecondsRealtime(0.5f);
                    _log.AppendLine("[MatchLoop] Capturing 02_character_select.png (with C07 highlighted and 3D preview)...");
                    yield return Capture("02_character_select");
                }

                _log.AppendLine($"[MatchLoop] Selecting {p1Id} vs {p2Id} on grid");
                yield return new WaitForSecondsRealtime(0.2f);
                charSelect.SelectSlot(p1Idx >= 0 ? p1Idx : 0, true);
                if (phase == "versus")
                {
                    yield return new WaitForSecondsRealtime(0.2f);
                    charSelect.SelectSlot(p2Idx >= 0 ? p2Idx : 1, false);
                }
            }
            else
            {
                if (phase == "arcade")
                {
                    yield return Capture("02_character_select");
                }
                PlayerPrefs.SetString("MDI_P1_CharacterId", p1Id);
                PlayerPrefs.SetString("MDI_P2_CharacterId", p2Id);
                PlayerPrefs.SetString("MDI_StageId", "ST07");
                PlayerPrefs.Save();
                LaunchServices.Session.BeginMatch(p1Id, p2Id, "ST07");
                SceneManager.LoadScene("BattleArena");
            }
        }

        IEnumerator ProbeMatch(string phase)
        {
            string expectP1 = phase == "versus" ? "SK02" : "C07";
            string expectP2 = phase == "versus" ? "C12" : "EX01";
            _sawWalk = false;
            _sawJump = false;
            _sawAttack = false;
            _log.AppendLine($"=== {phase} expected {expectP1} vs {expectP2} ===");

            float deadline = Time.realtimeSinceStartup + 15f;
            while (Time.realtimeSinceStartup < deadline)
            {
                HomegrownCombatDirector director = HomegrownCombatDirector.Instance;
                BBMatchSim sim = BBMatchSim.Instance;
                if (director != null && director.isActiveAndEnabled
                    && director.ActiveP1Id == expectP1 && director.ActiveP2Id == expectP2
                    && sim != null && sim.p1Brain != null && sim.p2Brain != null
                    && CombatTickEngine.Instance != null && CombatTickEngine.Instance.isActiveAndEnabled
                    && CombatTickEngine.Instance.CurrentTick > 0)
                    break;
                yield return null;
            }

            HomegrownCombatDirector live = HomegrownCombatDirector.Instance;
            BBMatchSim liveSim = BBMatchSim.Instance;
            int tick = CombatTickEngine.Instance != null ? CombatTickEngine.Instance.CurrentTick : -1;

            _log.AppendLine($"director={(live != null && live.isActiveAndEnabled)} p1={(live != null ? live.ActiveP1Id : "none")} p2={(live != null ? live.ActiveP2Id : "none")} tickEngine={(CombatTickEngine.Instance != null && CombatTickEngine.Instance.isActiveAndEnabled)} tick={tick} sim={(liveSim != null)}");

            if (live == null || live.ActiveP1Id != expectP1 || live.ActiveP2Id != expectP2 || liveSim == null || liveSim.p1Brain == null || tick <= 0)
            {
                _log.AppendLine($"FAIL: Expected {expectP1} vs {expectP2} with tick > 0, got p1={(live != null ? live.ActiveP1Id : "null")} p2={(live != null ? live.ActiveP2Id : "null")} tick={tick}");
                WriteLog();
                Finish("FAIL match did not start with expected parameters");
                yield break;
            }

            LogFighter("P1", liveSim.p1Brain);
            LogFighter("P2", liveSim.p2Brain);

            if (phase == "arcade")
            {
                // Capture round start screenshot
                yield return new WaitForSecondsRealtime(0.5f);
                _log.AppendLine("[MatchLoop] Capturing 03_battle_arena_round_start.png (C07 vs EX01, round call visible)...");
                yield return Capture("03_battle_arena_round_start");

                _driving = true;

                // Configure test-only lower maxHealth on opponent so real presses achieve natural KO
                var ex01Def = CharacterRegistry.GetCharacterById("EX01");
                var ex01Profile = ex01Def != null ? ex01Def.blazBlueProfile as BBCharacterProfile : null;
                float origEx01MaxHealth = ex01Profile != null ? ex01Profile.maxHealth : 10000f;
                if (ex01Profile != null)
                {
                    ex01Profile.maxHealth = 400f;
                }

                // 1. Walk forward
                yield return Hold("walk", new[] { Key.D }, 20, liveSim);
                yield return Hold("release", System.Array.Empty<Key>(), 8, liveSim);

                // 2. Crouch
                yield return Hold("crouch", new[] { Key.S }, 15, liveSim);
                yield return Hold("release", System.Array.Empty<Key>(), 8, liveSim);

                // 3. Jump
                yield return Hold("jump", new[] { Key.W }, 35, liveSim);
                yield return Hold("release", System.Array.Empty<Key>(), 8, liveSim);

                // Wait until grounded and recovered
                float landTimeout = Time.realtimeSinceStartup + 3f;
                while (liveSim.p1Brain != null && (!liveSim.p1Brain.isGrounded || liveSim.p1Brain.bbState == BBState.Hitstun || liveSim.p1Brain.bbState == BBState.Blockstun) && Time.realtimeSinceStartup < landTimeout)
                {
                    yield return null;
                }
                yield return new WaitForSecondsRealtime(0.05f);

                // Walk into close attack range
                float walkTimeout = Time.realtimeSinceStartup + 4f;
                while (liveSim.p1Brain != null && liveSim.p2Brain != null && Time.realtimeSinceStartup < walkTimeout)
                {
                    float dist = Mathf.Abs(liveSim.p1Brain.transform.position.x - liveSim.p2Brain.transform.position.x);
                    if (dist <= 1.25f) break;
                    _held = new[] { Key.D };
                    _driving = true;
                    yield return null;
                }
                _held = System.Array.Empty<Key>();
                yield return new WaitForSecondsRealtime(0.05f);

                // 4. Attack A (5A) with real key press
                yield return Hold("A", new[] { Key.J }, 15, liveSim);
                yield return Hold("release", System.Array.Empty<Key>(), 8, liveSim);

                // Wait 2 frames for impact and contact presentation
                yield return null;
                yield return null;

                _log.AppendLine("[MatchLoop] Capturing 04_battle_arena_mid_combo.png (hit spark, combo counter, HP drop)...");
                yield return Capture("04_battle_arena_mid_combo");

                yield return Hold("release", System.Array.Empty<Key>(), 10, liveSim);
                _held = System.Array.Empty<Key>();
                _driving = false;

                int finalTick = CombatTickEngine.Instance != null ? CombatTickEngine.Instance.CurrentTick : -1;
                if (finalTick <= 0 || !_sawWalk || !_sawJump || !_sawAttack)
                {
                    if (ex01Profile != null) ex01Profile.maxHealth = origEx01MaxHealth;
                    _log.AppendLine($"FAIL tick={finalTick} walk={_sawWalk} jump={_sawJump} attack={_sawAttack}");
                    WriteLog();
                    Finish("FAIL input did not trigger required state transitions");
                    yield break;
                }

                _log.AppendLine($"PASS arcade: tick={finalTick} walk={_sawWalk} jump={_sawJump} attack={_sawAttack}");

                // 5. In-Game Pause Modal
                if (BattleHUDController.Instance != null)
                {
                    BattleHUDController.Instance.ToggleInGamePause();
                    yield return new WaitForSecondsRealtime(0.4f);
                    _log.AppendLine("[MatchLoop] Capturing 05_battle_arena_pause_menu.png...");
                    yield return Capture("05_battle_arena_pause_menu");
                    yield return new WaitForSecondsRealtime(0.2f);
                    BattleHUDController.Instance.ToggleInGamePause();
                    yield return new WaitForSecondsRealtime(0.2f);
                }

                // 6. Reach natural KO on P2 by real presses
                _log.AppendLine("[MatchLoop] Driving real attacks to achieve natural KO on P2...");
                float koTimeout = Time.realtimeSinceStartup + 6f;
                int koPresses = 0;
                while (liveSim.p2Brain != null && liveSim.p2Brain.health > 0f && Time.realtimeSinceStartup < koTimeout && koPresses < 8)
                {
                    koPresses++;
                    yield return Hold("A", new[] { Key.J }, 12, liveSim);
                    yield return Hold("release", System.Array.Empty<Key>(), 10, liveSim);
                }

                // Wait for referee to trigger round/match end
                float refWait = Time.realtimeSinceStartup + 3f;
                while (MatchRoundReferee.Instance != null && MatchRoundReferee.Instance.State != MatchState.MatchOver && Time.realtimeSinceStartup < refWait)
                {
                    yield return null;
                }

                yield return new WaitForSecondsRealtime(0.8f);
                _log.AppendLine("[MatchLoop] Capturing 06_battle_arena_results_screen.png...");
                yield return Capture("06_battle_arena_results_screen");

                // Restore profile maxHealth
                if (ex01Profile != null)
                {
                    ex01Profile.maxHealth = origEx01MaxHealth;
                }

                // Route to CharacterSelect for Versus
                _log.AppendLine("[MatchLoop] Transitioning from Results / Pause to CharacterSelect for Versus phase...");
                EditorPrefs.SetString(MatchLoopVerifier.PhaseKey, "versus");
                PlayerPrefs.SetString("MDI_GameMode", "LocalVersus");
                PlayerPrefs.Save();
                LaunchServices.Session.TrySelectMode("LocalVersus", out _);

                if (BattleHUDController.Instance != null)
                {
                    BattleHUDController.Instance.OnClickCharacterSelect();
                }
                else
                {
                    SceneManager.LoadScene("CharacterSelect");
                }
            }
            else
            {
                // Versus phase
                yield return new WaitForSecondsRealtime(0.5f);
                _log.AppendLine($"PASS versus: tick={tick} p1={expectP1} p2={expectP2}");
                WriteLog();
                Finish("ALL PHASES PASSED SUCCESSFULLY");
            }
        }

        IEnumerator Hold(string label, Key[] keys, int frames, BBMatchSim sim)
        {
            _held = keys ?? System.Array.Empty<Key>();
            _driving = true;
            for (int i = 0; i < frames; i++)
            {
                BBFighterBrain p1 = sim != null ? sim.p1Brain : null;
                if (p1 != null)
                {
                    if (p1.bbState == BBState.WalkForward || p1.bbState == BBState.WalkBackward)
                        _sawWalk = true;
                    if (p1.bbState == BBState.JumpStartup || p1.bbState == BBState.Airborne)
                        _sawJump = true;
                    if (p1.bbState == BBState.AttackStartup || p1.bbState == BBState.AttackActive || p1.bbState == BBState.AttackRecovery)
                        _sawAttack = true;
                }
                yield return null;
            }

            BBFighterBrain endP1 = sim != null ? sim.p1Brain : null;
            BBFighterBrain endP2 = sim != null ? sim.p2Brain : null;
            _log.AppendLine($"{label} tick={(CombatTickEngine.Instance != null ? CombatTickEngine.Instance.CurrentTick : -1)} p1={Describe(endP1)} p2={Describe(endP2)}");
        }

        static string Describe(BBFighterBrain brain)
        {
            if (brain == null) return "missing";
            string id = brain.fighter != null && brain.fighter.appliedDefinition != null
                ? brain.fighter.appliedDefinition.id
                : "?";
            int slot = brain.sampler != null ? brain.sampler.playerIndex : -1;
            bool human = brain.sampler != null && brain.sampler.isPlayerControlled;
            string clip = brain.animDriver != null ? brain.animDriver.CurrentClipLabel() : "no-driver";
            return $"{id} slot={slot} human={human} state={brain.bbState} clip={clip}";
        }

        void LogFighter(string label, BBFighterBrain brain)
        {
            _log.AppendLine(label + " " + Describe(brain));
        }

        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            string dir = Path.Combine(Directory.GetCurrentDirectory(), "Screenshots", "rebuild");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, name + ".png");
            Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture();
            if (tex != null)
            {
                File.WriteAllBytes(path, tex.EncodeToPNG());
                UnityEngine.Object.Destroy(tex);
                _log.AppendLine("screenshot " + path);
                Debug.Log($"<color=#00e676>[MatchLoop] Screenshot captured: {name}.png ({tex.width}x{tex.height})</color>");
            }
            else
            {
                _log.AppendLine("screenshot FAILED " + path);
                Debug.LogError($"[MatchLoop] Screenshot failed: {name}.png");
            }
        }

        void WriteLog()
        {
            string dir = Path.Combine(Directory.GetCurrentDirectory(), "Screenshots", "rebuild");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "match_loop_log.txt");
            File.WriteAllText(path, _log.ToString());
            Debug.Log("<color=#00e676>[MatchLoop] Wrote log to " + path + "</color>");
        }

        void Finish(string reason)
        {
            _log.AppendLine(reason);
            WriteLog();
            _driving = false;
            _held = System.Array.Empty<Key>();
            EditorPrefs.SetString(MatchLoopVerifier.PhaseKey, "done");
            EditorPrefs.SetBool(MatchLoopVerifier.RunKey, false);
            EditorApplication.isPlaying = false;
            Debug.Log($"<color=#00e676>[MatchLoop] Finished: {reason}</color>");
        }
    }
}
#endif
