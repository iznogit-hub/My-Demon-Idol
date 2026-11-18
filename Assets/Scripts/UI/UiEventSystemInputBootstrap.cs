using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM
using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace DemonIdol.UI
{
    /// <summary>
    /// Boot scenes still carry StandaloneInputModule axes named Horizontal/Vertical.
    /// This project's Input Manager defines UFE axes (P1KeyboardHorizontal, Mouse Wheel,
    /// Submit, Cancel) and does not define those default names, so EventSystem.Update
    /// throws. Rewire that existing EventSystem onto the Input System UI module and the
    /// project-wide UI action map (Navigate, Submit, Cancel, Point, Click, ScrollWheel).
    /// Does not add axes, flip the active input handler, or create another EventSystem.
    /// Modules already pointed at a defined axis, including UFE's Mouse Wheel module,
    /// are left alone. UFE gameplay input stays on its own controllers.
    /// </summary>
    public static class UiEventSystemInputBootstrap
    {
        const string MissingHorizontal = "Horizontal";
        const string MissingVertical = "Vertical";
        const string ProjectActionsPath = "Assets/InputSystem_Actions.inputactions";

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        static void EditorInitialize()
        {
            ReconcileLoadedEventSystems();
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += (scene, mode) => ReconcileLoadedEventSystems();
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void BeforeSceneLoad()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AfterSceneLoad()
        {
            ReconcileLoadedEventSystems();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ReconcileLoadedEventSystems();
        }

        public static void ReconcileLoadedEventSystems()
        {
            EventSystem[] systems = UnityEngine.Object.FindObjectsByType<EventSystem>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < systems.Length; i++)
                Reconcile(systems[i]);
        }

        public static bool UsesMissingDefaultAxes(string horizontalAxis, string verticalAxis)
        {
            return horizontalAxis == MissingHorizontal || verticalAxis == MissingVertical;
        }

        public static void Reconcile(EventSystem eventSystem)
        {
            if (eventSystem == null)
                return;

            StandaloneInputModule legacy = eventSystem.GetComponent<StandaloneInputModule>();
            if (legacy == null || !UsesMissingDefaultAxes(legacy.horizontalAxis, legacy.verticalAxis))
                return;

#if ENABLE_INPUT_SYSTEM
            InputSystemUIInputModule module = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (module != null && !legacy.enabled && module.move != null && module.move.action != null)
                return;

            if (module == null)
                module = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();

            if (!BindProjectUiActions(module))
            {
                if (module.actionsAsset == null || module.move == null || module.move.action == null)
                    module.AssignDefaultActions();
            }

            module.enabled = true;
            legacy.enabled = false;
#else
            // Input System package is not compiled in. Stop the module from polling
            // axis names this project does not define.
            legacy.enabled = false;
#endif
        }

#if ENABLE_INPUT_SYSTEM
        public static bool BindProjectUiActions(InputSystemUIInputModule module)
        {
            if (module == null)
                return false;

            InputActionAsset actions = InputSystem.actions;
#if UNITY_EDITOR
            if (actions == null)
                actions = UnityEditor.AssetDatabase.LoadAssetAtPath<InputActionAsset>(ProjectActionsPath);
#endif
            if (actions == null)
                return false;

            InputActionMap ui = actions.FindActionMap("UI", false);
            if (ui == null)
                return false;

            UiInputBindingOwner owner = module.GetComponent<UiInputBindingOwner>();
            if (owner == null)
                owner = module.gameObject.AddComponent<UiInputBindingOwner>();
            owner.ReleaseOwnedReferences();

            module.actionsAsset = actions;
            Bind(owner, ui, "Point", reference => module.point = reference);
            Bind(owner, ui, "Navigate", reference => module.move = reference);
            Bind(owner, ui, "Click", reference => module.leftClick = reference);
            Bind(owner, ui, "RightClick", reference => module.rightClick = reference);
            Bind(owner, ui, "MiddleClick", reference => module.middleClick = reference);
            Bind(owner, ui, "ScrollWheel", reference => module.scrollWheel = reference);
            Bind(owner, ui, "Submit", reference => module.submit = reference);
            Bind(owner, ui, "Cancel", reference => module.cancel = reference);
            Bind(owner, ui, "TrackedDevicePosition", reference => module.trackedDevicePosition = reference);
            Bind(owner, ui, "TrackedDeviceOrientation", reference => module.trackedDeviceOrientation = reference);

            return module.move != null && module.move.action != null
                && module.submit != null && module.submit.action != null
                && module.point != null && module.point.action != null;
        }

        static void Bind(UiInputBindingOwner owner, InputActionMap map, string actionName, Action<InputActionReference> assign)
        {
            InputAction action = map.FindAction(actionName, false);
            if (action == null)
                return;

            InputActionReference reference = InputActionReference.Create(action);
            owner.Own(reference);
            assign(reference);
            if (!action.enabled)
                action.Enable();
        }
#endif
    }

#if ENABLE_INPUT_SYSTEM
    /// <summary>
    /// Owns InputActionReference instances created for one EventSystem.
    /// Destroying the references does not disable the shared project-wide actions.
    /// </summary>
    public sealed class UiInputBindingOwner : MonoBehaviour
    {
        readonly System.Collections.Generic.List<InputActionReference> _owned = new System.Collections.Generic.List<InputActionReference>();

        public int OwnedCount => _owned.Count;

        public void Own(InputActionReference reference)
        {
            if (reference != null)
                _owned.Add(reference);
        }

        public void ReleaseOwnedReferences()
        {
            for (int i = 0; i < _owned.Count; i++)
            {
                if (_owned[i] == null)
                    continue;
                if (Application.isPlaying)
                    Destroy(_owned[i]);
                else
                    DestroyImmediate(_owned[i]);
            }
            _owned.Clear();
        }

        void OnDestroy()
        {
            ReleaseOwnedReferences();
        }
    }
#endif
}
