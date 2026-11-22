using System.Collections.Generic;
using Chilseok.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

namespace Chilseok.Tests.PlayMode
{
    public sealed class PlayerInputReaderTests : InputTestFixture
    {
        public enum Command { Jump, RotateLeft, RotateRight, Reset }

        private readonly List<Command> _received = new();
        private GameObject _player;
        private PlayerInputReader _reader;
        private Keyboard _keyboard;
        private Gamepad _pad;

        public override void Setup()
        {
            base.Setup();
            _received.Clear();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _pad = InputSystem.AddDevice<Gamepad>();
            _player = new GameObject(nameof(PlayerInputReaderTests));
            _reader = _player.AddComponent<PlayerInputReader>();
            _reader.JumpPressed += () => _received.Add(Command.Jump);
            _reader.RotateLeftPressed += () => _received.Add(Command.RotateLeft);
            _reader.RotateRightPressed += () => _received.Add(Command.RotateRight);
            _reader.ResetPressed += () => _received.Add(Command.Reset);
        }

        public override void TearDown()
        {
            try
            {
                Object.DestroyImmediate(_player);
            }
            finally
            {
                base.TearDown();
            }
        }

        [TestCase(Key.A, -1f)]
        [TestCase(Key.D, 1f)]
        public void HorizontalInput_KeyboardKeyPressed_ReadsDirection(Key key, float expected)
        {
            Press(_keyboard[key]);
            Assert.That(_reader.HorizontalInput, Is.EqualTo(expected));

            Release(_keyboard[key]);
            Assert.That(_reader.HorizontalInput, Is.Zero);
        }

        [TestCase(-1f, 0f, -1f)]
        [TestCase(1f, 0f, 1f)]
        [TestCase(0f, 1f, 0f)]
        public void HorizontalInput_LeftStickMoved_ReadsOnlyHorizontalAxis(float x, float y, float expected)
        {
            Set(_pad.leftStick, new Vector2(x, y));
            Assert.That(_reader.HorizontalInput, Is.EqualTo(expected));
        }

        [TestCase(Command.Jump, Key.Space, GamepadButton.South)]
        [TestCase(Command.RotateLeft, Key.Q, GamepadButton.LeftShoulder)]
        [TestCase(Command.RotateRight, Key.E, GamepadButton.RightShoulder)]
        [TestCase(Command.Reset, Key.R, GamepadButton.Select)]
        public void ButtonPressed_KeyboardAndPad_NotifyConsumerOncePerPress(
            Command expected, Key key, GamepadButton button)
        {
            ButtonControl keyboardButton = _keyboard[key];
            Press(keyboardButton);
            Release(keyboardButton);
            Assert.That(_received, Is.EqualTo(new[] { expected }));

            _received.Clear();
            Press(_pad[button]);
            Release(_pad[button]);
            Assert.That(_received, Is.EqualTo(new[] { expected }));
        }

        [Test]
        public void SetInputLocked_HeldMovement_IsZeroUntilUnlocked()
        {
            Press(_keyboard.dKey);

            _reader.SetInputLocked(true);
            Assert.That(_reader.IsInputLocked, Is.True);
            Assert.That(_reader.HorizontalInput, Is.Zero);

            _reader.SetInputLocked(false);
            Assert.That(_reader.IsInputLocked, Is.False);
            Assert.That(_reader.HorizontalInput, Is.EqualTo(1f));
        }

        [Test]
        public void SetInputLocked_ButtonHeldThroughUnlock_DropsPressUntilPressedAgain()
        {
            _reader.SetInputLocked(true);
            Press(_keyboard.spaceKey);
            Press(_pad.leftShoulder);
            Press(_keyboard.eKey);
            Press(_pad.selectButton);
            Assert.That(_received, Is.Empty);

            _reader.SetInputLocked(false);
            InputSystem.Update();
            Assert.That(_received, Is.Empty);

            Release(_pad.leftShoulder);
            Press(_pad.leftShoulder);
            Assert.That(_received, Is.EqualTo(new[] { Command.RotateLeft }));
        }

        [Test]
        public void OnEnable_AfterDisable_ResumesDeliveryWithoutDuplicateEvents()
        {
            _reader.enabled = false;
            Press(_keyboard.dKey);
            Press(_keyboard.rKey);
            Release(_keyboard.rKey);
            Assert.That(_reader.HorizontalInput, Is.Zero);
            Assert.That(_received, Is.Empty);

            _reader.enabled = true;
            Press(_keyboard.rKey);
            Assert.That(_received, Is.EqualTo(new[] { Command.Reset }));
        }
    }
}
