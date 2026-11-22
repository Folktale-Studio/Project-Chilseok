using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Chilseok.Player
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        private InputActionMap _actions;
        private InputAction _move;
        private InputAction _jump;
        private InputAction _rotateLeft;
        private InputAction _rotateRight;
        private InputAction _reset;

        public event Action JumpPressed;
        public event Action RotateLeftPressed;
        public event Action RotateRightPressed;
        public event Action ResetPressed;

        public bool IsInputLocked { get; private set; }
        public float HorizontalInput => IsInputLocked || _move == null ? 0f : _move.ReadValue<float>();

        public void SetInputLocked(bool locked)
        {
            // 액션을 켜 둬야 잠금 중 눌린 버튼이 해제 순간에 다시 발동하지 않는다.
            IsInputLocked = locked;
        }

        private void Awake()
        {
            _actions = new InputActionMap("ChilseokPlayer");
            _move = _actions.AddAction("Move", InputActionType.Value, expectedControlLayout: "Axis");
            _move.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/a")
                .With("Positive", "<Keyboard>/d");
            _move.AddBinding("<Gamepad>/leftStick/x", processors: "axisDeadzone");

            _jump = AddButton("Jump", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            _rotateLeft = AddButton("RotateLeft", "<Keyboard>/q", "<Gamepad>/leftShoulder");
            _rotateRight = AddButton("RotateRight", "<Keyboard>/e", "<Gamepad>/rightShoulder");
            _reset = AddButton("Reset", "<Keyboard>/r", "<Gamepad>/select");
        }

        private void OnEnable()
        {
            _jump.performed += OnJump;
            _rotateLeft.performed += OnRotateLeft;
            _rotateRight.performed += OnRotateRight;
            _reset.performed += OnReset;
            _actions.Enable();
        }

        private void OnDisable()
        {
            _actions.Disable();
            _jump.performed -= OnJump;
            _rotateLeft.performed -= OnRotateLeft;
            _rotateRight.performed -= OnRotateRight;
            _reset.performed -= OnReset;
        }

        private void OnDestroy()
        {
            _actions?.Dispose();
        }

        private InputAction AddButton(string name, string keyboardPath, string gamepadPath)
        {
            var action = _actions.AddAction(name, InputActionType.Button);
            action.AddBinding(keyboardPath);
            action.AddBinding(gamepadPath);
            return action;
        }

        private void OnJump(InputAction.CallbackContext context) => RaiseIfUnlocked(JumpPressed);
        private void OnRotateLeft(InputAction.CallbackContext context) => RaiseIfUnlocked(RotateLeftPressed);
        private void OnRotateRight(InputAction.CallbackContext context) => RaiseIfUnlocked(RotateRightPressed);
        private void OnReset(InputAction.CallbackContext context) => RaiseIfUnlocked(ResetPressed);

        private void RaiseIfUnlocked(Action pressed)
        {
            if (!IsInputLocked)
                pressed?.Invoke();
        }
    }
}
