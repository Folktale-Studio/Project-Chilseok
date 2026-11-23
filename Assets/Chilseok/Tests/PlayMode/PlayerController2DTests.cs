#if UNITY_EDITOR
using System.Collections;
using Chilseok.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace Chilseok.Tests.PlayMode
{
    public sealed class PlayerController2DTests : InputTestFixture
    {
        private GameObject _player;
        private TextAsset _layout;
        private MovementSettings _settings;
        private Keyboard _keyboard;
        private float _previousCaptureDeltaTime;

        public override void Setup()
        {
            base.Setup();
            _previousCaptureDeltaTime = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;
            _keyboard = InputSystem.AddDevice<Keyboard>();
        }

        public override void TearDown()
        {
            try
            {
                Object.DestroyImmediate(_player);
                Object.DestroyImmediate(_layout);
                Object.DestroyImmediate(_settings);
                Time.captureDeltaTime = _previousCaptureDeltaTime;
            }
            finally
            {
                base.TearDown();
            }
        }

        [UnityTest]
        public IEnumerator Jump_OneCellStep_LandsOnStep()
        {
            return JumpAndCheck("######", ".S####", ".....M", "......", new Vector3Int(2, 2, 0), false);
        }

        [UnityTest]
        public IEnumerator Jump_TwoCellStep_StaysOnStartingSide()
        {
            return JumpAndCheck("######", ".S####", "..####", ".....M", new Vector3Int(1, 1, 0), true);
        }

        [UnityTest]
        public IEnumerator Jump_OneCellGap_LandsOnNextPlatform()
        {
            return JumpAndCheck("##.###", ".S...M", "......", "......", new Vector3Int(3, 1, 0), false);
        }

        [UnityTest]
        public IEnumerator Jump_TwoCellGap_StaysOnStartingSide()
        {
            return JumpAndCheck("##..##", ".S...M", "......", "......", new Vector3Int(1, 1, 0), true);
        }

        [UnityTest]
        public IEnumerator Jump_WhileWalkingRight_StaysInDirection()
        {
            return JumpFromPartialWalk(Key.D, 1);
        }

        [UnityTest]
        public IEnumerator Jump_WhileWalkingLeft_StaysInDirection()
        {
            return JumpFromPartialWalk(Key.A, -1);
        }

        [UnityTest]
        public IEnumerator ResetPressed_AfterWalking_ReturnsToStartingCell()
        {
            var controller = CreatePlayer("######", ".S...M", "......", "......", false);
            Press(_keyboard.dKey);
            for (var frame = 0; frame < 30 && controller.Feet.x == 1; frame++)
                yield return null;

            Assert.That(controller.Feet.x, Is.GreaterThan(1));
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.R));
            yield return null;

            Assert.That(controller.Feet, Is.EqualTo(new Vector3Int(1, 1, 0)));
            Assert.That(controller.transform.localPosition, Is.EqualTo(new Vector3(1f, 1f, 0f)));
        }

        private IEnumerator JumpAndCheck(string floor, string first, string second, string third,
            Vector3Int expected, bool upperBounds)
        {
            var controller = CreatePlayer(floor, first, second, third, upperBounds);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.D, Key.Space));
            yield return null;
            Assert.That(controller.IsGrounded, Is.False);
            InputSystem.QueueStateEvent(_keyboard, default(KeyboardState));

            for (var frame = 0; frame < 120 && !controller.IsGrounded; frame++)
                yield return null;

            Assert.That(controller.IsGrounded, Is.True);
            Assert.That(controller.Feet, Is.EqualTo(expected));
        }

        private IEnumerator JumpFromPartialWalk(Key key, int direction)
        {
            var controller = CreatePlayer("######", "..S..M", "......", "......", false);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(key));
            for (var frame = 0; frame < 6; frame++)
                yield return null;
            Assert.That(controller.Feet.x, Is.EqualTo(2));
            var takeoff = controller.transform.localPosition.x;

            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(key, Key.Space));
            yield return null;
            Assert.That(controller.IsGrounded, Is.False);
            Assert.That((controller.transform.localPosition.x - takeoff) * direction, Is.GreaterThan(0f));
            InputSystem.QueueStateEvent(_keyboard, default(KeyboardState));

            for (var frame = 0; frame < 120 && !controller.IsGrounded; frame++)
            {
                var before = controller.transform.localPosition.x;
                yield return null;
                Assert.That((controller.transform.localPosition.x - before) * direction,
                    Is.GreaterThanOrEqualTo(-0.0001f));
            }

            Assert.That(controller.IsGrounded, Is.True);
            Assert.That(controller.Feet.x, Is.EqualTo(2 + direction));
        }

        private PlayerController2D CreatePlayer(string floor, string first, string second, string third,
            bool upperBounds)
        {
            _layout = new TextAsset($@"name: movement test
teaches: movement
intent: fixed input exercises jump and landing
size: 6 4 1
rotations: 0-0
start-view: 0
y=0
z0: {floor}
y=1
z0: {first}
y=2
z0: {second}
y=3
z0: {third}");
            _settings = ScriptableObject.CreateInstance<MovementSettings>();
            var tuning = new SerializedObject(_settings);
            tuning.FindProperty("_moveSpeed").floatValue = upperBounds ? 5f : 3f;
            tuning.FindProperty("_jumpHeight").floatValue = upperBounds ? 1.4f : 1.1f;
            tuning.FindProperty("_jumpDistance").floatValue = upperBounds ? 2.4f : 1.8f;
            tuning.FindProperty("_gravity").floatValue = upperBounds ? 25f : 40f;
            tuning.FindProperty("_coyoteSeconds").floatValue = upperBounds ? 0.12f : 0.05f;
            tuning.ApplyModifiedPropertiesWithoutUndo();
            _player = new GameObject(nameof(PlayerController2DTests));
            _player.SetActive(false);
            var controller = _player.AddComponent<PlayerController2D>();
            var data = new SerializedObject(controller);
            data.FindProperty("_zoneData").objectReferenceValue = _layout;
            data.FindProperty("_settings").objectReferenceValue = _settings;
            data.ApplyModifiedPropertiesWithoutUndo();
            _player.SetActive(true);
            return controller;
        }
    }
}
#endif
