#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Text;
using DemonIdol.BlazBlue;
using DemonIdol.Combat;
using DemonIdol.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace DemonIdol.Editor
{
    /// <summary>
    /// One button: Demon Idol &gt; Verify &gt; Match Loop.
    /// Enters Play Mode from MainMenu, launches Arcade C07 vs EX01 and Versus SK02 vs C12,
    /// and drives BBInputSampler through the Input System keyboard (not a handwritten state log).
    /// Does not save or dirty shipped scenes.
    /// </summary>
    public static class MatchLoopVerifier
    {
        public const string RunKey = "MDI_MatchLoop_Run";
        public const string PhaseKey = "MDI_MatchLoop_Phase";
        const string MenuScenePath = "Assets/Scenes/MainMenu.unity";

        [MenuItem("Demon Idol/Verify/Match Loop")]
        public static void RunFromMenu()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[MatchLoop] Exit Play Mode, then run Demon Idol > Verify > Match Loop again.");
                return;
            }

            EditorPrefs.SetBool(RunKey, true);
            EditorPrefs.SetString(PhaseKey, "arcade");
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScenePath);
            Debug.Log("[MatchLoop] Entering Play Mode from MainMenu. Arcade C07 vs EX01, then Versus SK02 vs C12.");
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
            if (UnityEngine.Object.FindFirstObjectByType<MatchLoopRunner>() != null)
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
            else if (sceneName == "ArcadeArena")
                StartCoroutine(ProbeMatch(phase));
        }

        IEnumerator LaunchFromMenu(string phase)
        {
            yield return null;
            MainMenuController menu = UnityEngine.Object.FindFirstObjectByType<MainMenuController>();
            if (menu == null)
            {
                Finish("FAIL MainMenuController was not in the MainMenu scene.");
                yield break;
            }

            menu.SelectedStageId = "ST07";
            if (phase == "versus")
            {
                menu.ActiveMode = "LocalVersus";
                menu.SelectedP1Id = "SK02";
                menu.SelectedP2Id = "C12";
            }
            else
            {
                menu.ActiveMode = "Arcade";
                menu.SelectedP1Id = "C07";
                menu.SelectedP2Id = "EX01";
            }

            Debug.Log("[MatchLoop] MainMenu launch " + menu.ActiveMode + " " + menu.SelectedP1Id + " vs " + menu.SelectedP2Id);
            menu.OnLaunchBattle();
            float deadline = Time.realtimeSinceStartup + 8f;
            while (SceneManager.GetActiveScene().name == "MainMenu" && Time.realtimeSinceStartup < deadline)
                yield return null;
            if (SceneManager.GetActiveScene().name == "MainMenu")
            {
                _log.AppendLine("VS overlay did not load ArcadeArena; loading the arena with the session already begun");
                WriteLog();
                SceneManager.LoadScene("ArcadeArena");
            }
        }

        IEnumerator ProbeMatch(string phase)
        {
            string expectP1 = phase == "versus" ? "SK02" : "C07";
            string expectP2 = phase == "versus" ? "C12" : "EX01";
            _log.AppendLine("=== " + phase + " expected " + expectP1 + " vs " + expectP2 + " ===");

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
            _log.AppendLine("director=" + (live != null && live.isActiveAndEnabled)
                + " p1=" + (live != null ? live.ActiveP1Id : "none")
                + " p2=" + (live != null ? live.ActiveP2Id : "none")
                + " tickEngine=" + (CombatTickEngine.Instance != null && CombatTickEngine.Instance.isActiveAndEnabled)
                + " tick=" + (CombatTickEngine.Instance != null ? CombatTickEngine.Instance.CurrentTick : -1)
                + " sim=" + (liveSim != null));

            if (live == null || live.ActiveP1Id != expectP1 || live.ActiveP2Id != expectP2 || liveSim == null || liveSim.p1Brain == null)
            {
                _log.AppendLine("FAIL roster or simulation did not come up");
                WriteLog();
                if (phase != "versus")
                {
                    EditorPrefs.SetString(MatchLoopVerifier.PhaseKey, "versus");
                    SceneManager.LoadScene("MainMenu");
                }
                else
                    Finish("FAIL match did not start");
                yield break;
            }

            LogFighter("P1", liveSim.p1Brain);
            LogFighter("P2", liveSim.p2Brain);
            _driving = true;
            BBTouchLayout touch = UnityEngine.Object.FindFirstObjectByType<BBTouchLayout>();
            string touchParent = touch != null && touch.transform.parent != null ? touch.transform.parent.name : "none";
            _log.AppendLine("touchLayout=" + (touch != null) + " parent=" + touchParent
                + " shouldShow=" + BBTouchLayout.ShouldShowTouchControls()
                + " keyboard=" + (Keyboard.current != null));

            yield return Hold("walk", new[] { Key.D }, 30, liveSim, true, phase);
            yield return Hold("release", System.Array.Empty<Key>(), 8, liveSim, false, phase);
            yield return Hold("crouch", new[] { Key.S }, 20, liveSim, false, phase);
            yield return Hold("release", System.Array.Empty<Key>(), 8, liveSim, false, phase);
            yield return Hold("jump", new[] { Key.W }, 20, liveSim, false, phase);
            yield return Hold("release", System.Array.Empty<Key>(), 10, liveSim, false, phase);
            yield return Hold("dash-a", new[] { Key.D }, 4, liveSim, false, phase);
            yield return Hold("dash-gap", System.Array.Empty<Key>(), 4, liveSim, false, phase);
            yield return Hold("dash-b", new[] { Key.D }, 12, liveSim, false, phase);
            yield return Hold("release", System.Array.Empty<Key>(), 8, liveSim, false, phase);
            yield return Hold("A", new[] { Key.J }, 18, liveSim, true, phase);
            yield return Hold("release", System.Array.Empty<Key>(), 15, liveSim, false, phase);
            yield return Hold("B", new[] { Key.K }, 12, liveSim, false, phase);
            yield return Hold("release", System.Array.Empty<Key>(), 12, liveSim, false, phase);
            yield return Hold("C", new[] { Key.L }, 12, liveSim, false, phase);
            yield return Hold("release", System.Array.Empty<Key>(), 12, liveSim, false, phase);
            yield return Hold("D", new[] { Key.Space }, 12, liveSim, false, phase);
            yield return Hold("release", System.Array.Empty<Key>(), 8, liveSim, false, phase);
            if (phase == "versus")
            {
                yield return Hold("p2-back", new[] { Key.LeftArrow }, 20, liveSim, false, phase);
                yield return Hold("p2-A", new[] { Key.Numpad1 }, 12, liveSim, false, phase);
            }

            _held = System.Array.Empty<Key>();
            _driving = false;
            WriteLog();

            if (phase != "versus")
            {
                EditorPrefs.SetString(MatchLoopVerifier.PhaseKey, "versus");
                SceneManager.LoadScene("MainMenu");
            }
            else
                Finish("done");
        }

        IEnumerator Hold(string label, Key[] keys, int frames, BBMatchSim sim, bool shot, string phase)
        {
            _held = keys ?? System.Array.Empty<Key>();
            for (int i = 0; i < frames; i++)
                yield return null;
            BBFighterBrain p1 = sim != null ? sim.p1Brain : null;
            BBFighterBrain p2 = sim != null ? sim.p2Brain : null;
            _log.AppendLine(label
                + " tick=" + (CombatTickEngine.Instance != null ? CombatTickEngine.Instance.CurrentTick : -1)
                + " p1=" + Describe(p1)
                + " p2=" + Describe(p2));
            if (shot)
                yield return Capture(phase + "_" + label);
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
            return id + " slot=" + slot + " human=" + human + " state=" + brain.bbState + " clip=" + clip;
        }

        void LogFighter(string label, BBFighterBrain brain)
        {
            _log.AppendLine(label + " " + Describe(brain));
        }

        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            string dir = Path.Combine(Directory.GetCurrentDirectory(), "Screenshots", "hotfix_loop");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, name + ".png");
            Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture();
            if (tex != null)
            {
                File.WriteAllBytes(path, tex.EncodeToPNG());
                UnityEngine.Object.Destroy(tex);
                _log.AppendLine("screenshot " + path);
            }
            else
            {
                _log.AppendLine("screenshot FAILED " + path);
            }
        }

        void WriteLog()
        {
            string dir = Path.Combine(Directory.GetCurrentDirectory(), "Screenshots", "hotfix_loop");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "match_loop_log.txt");
            File.WriteAllText(path, _log.ToString());
            Debug.Log("[MatchLoop] Wrote " + path);
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
        }
    }
}
#endif
