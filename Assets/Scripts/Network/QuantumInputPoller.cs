using UnityEngine;
using DemonIdol.Combat;
using Quantum;
using Photon.Deterministic;

namespace DemonIdol.Network
{
    /// <summary>
    /// Samples local mobile touch & keyboard inputs each frame
    /// and injects them into Photon Quantum's deterministic rollback stream.
    /// </summary>
    public class QuantumInputPoller : MonoBehaviour
    {
        private static QuantumInputPoller _instance;
        public static QuantumInputPoller Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<QuantumInputPoller>();
                    if (_instance == null)
                    {
                        var go = new GameObject("QuantumInputPoller");
                        _instance = go.AddComponent<QuantumInputPoller>();
                        if (Application.isPlaying) DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [SerializeField] private MobileFighterInput _mobileInput;

        private Vector2 _currentDirection = Vector2.zero;
        private InputButton _activeButtons = InputButton.None;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (_mobileInput == null) _mobileInput = FindAnyObjectByType<MobileFighterInput>();
        }

        private void OnEnable()
        {
            QuantumCallback.Subscribe(this, (CallbackPollInput callback) => OnCallbackPollInput(callback));
        }

        private void OnDisable()
        {
            QuantumCallback.UnsubscribeListener(this);
        }

        private void OnCallbackPollInput(CallbackPollInput callback)
        {
            GatherInput();
            Quantum.Input i = new Quantum.Input();
            callback.SetInput(i, DeterministicInputFlags.Repeatable);
        }

        private void Update()
        {
            GatherInput();
        }

        private void GatherInput()
        {
            _activeButtons = InputButton.None;
            _currentDirection = Vector2.zero;

            try
            {
                // Directional Input
                if (UnityEngine.Input.GetKey(KeyCode.A) || UnityEngine.Input.GetKey(KeyCode.LeftArrow)) _currentDirection.x -= 1f;
                if (UnityEngine.Input.GetKey(KeyCode.D) || UnityEngine.Input.GetKey(KeyCode.RightArrow)) _currentDirection.x += 1f;
                if (UnityEngine.Input.GetKey(KeyCode.S) || UnityEngine.Input.GetKey(KeyCode.DownArrow)) _currentDirection.y -= 1f;
                if (UnityEngine.Input.GetKey(KeyCode.W) || UnityEngine.Input.GetKey(KeyCode.UpArrow)) _currentDirection.y += 1f;

                // Action Buttons
                if (UnityEngine.Input.GetKey(KeyCode.J)) _activeButtons |= InputButton.Light;
                if (UnityEngine.Input.GetKey(KeyCode.K)) _activeButtons |= InputButton.Medium;
                if (UnityEngine.Input.GetKey(KeyCode.L)) _activeButtons |= InputButton.Heavy;
                if (UnityEngine.Input.GetKey(KeyCode.U)) _activeButtons |= InputButton.DriveSkill;
                if (UnityEngine.Input.GetKey(KeyCode.I)) _activeButtons |= InputButton.JumpAttack;
                if (UnityEngine.Input.GetKey(KeyCode.F)) _activeButtons |= InputButton.Parry;
                if (UnityEngine.Input.GetKey(KeyCode.Space) || UnityEngine.Input.GetKey(KeyCode.Q)) _activeButtons |= InputButton.Backdash;
                if (UnityEngine.Input.GetKey(KeyCode.O)) _activeButtons |= InputButton.DomainSuper;
            }
            catch (System.InvalidOperationException)
            {
#if ENABLE_INPUT_SYSTEM
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null)
                {
                    if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) _currentDirection.x -= 1f;
                    if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) _currentDirection.x += 1f;
                    if (kb.sKey.isPressed || kb.downArrowKey.isPressed) _currentDirection.y -= 1f;
                    if (kb.wKey.isPressed || kb.upArrowKey.isPressed) _currentDirection.y += 1f;

                    if (kb.jKey.isPressed) _activeButtons |= InputButton.Light;
                    if (kb.kKey.isPressed) _activeButtons |= InputButton.Medium;
                    if (kb.lKey.isPressed) _activeButtons |= InputButton.Heavy;
                    if (kb.uKey.isPressed) _activeButtons |= InputButton.DriveSkill;
                    if (kb.iKey.isPressed) _activeButtons |= InputButton.JumpAttack;
                    if (kb.fKey.isPressed) _activeButtons |= InputButton.Parry;
                    if (kb.spaceKey.isPressed || kb.qKey.isPressed) _activeButtons |= InputButton.Backdash;
                    if (kb.oKey.isPressed) _activeButtons |= InputButton.DomainSuper;
                }
#endif
            }
        }

        /// <summary>
        /// Called by Quantum's CallbackPollInput to obtain the deterministic input for the current tick.
        /// </summary>
        public QuantumFighterInput PollLocalInput()
        {
            QuantumFighterInput input = new QuantumFighterInput();
            input.Pack(_currentDirection, _activeButtons);
            return input;
        }

        public void SetVirtualTouchInput(Vector2 direction, InputButton buttons)
        {
            _currentDirection = direction;
            _activeButtons = buttons;
        }
    }
}
