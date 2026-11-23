#if UNITY_EDITOR
using System.Collections;
using Chilseok.CameraSystem;
using Chilseok.Player;
using Chilseok.Puzzle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace Chilseok.Tests.PlayMode
{
    public sealed class ViewRotatorTests : InputTestFixture
    {
        private GameObject _playerObject;
        private GameObject _pivot;
        private GameObject _indicator;
        private TextAsset _layout;
        private MovementSettings _settings;
        private Keyboard _keyboard;
        private PlayerController2D _player;
        private ViewRotator _rotator;
        private PlayerInputReader _input;
        private float _previousCaptureDeltaTime;

        public override void Setup()
        {
            base.Setup();
            _previousCaptureDeltaTime = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _layout = new TextAsset(@"name: rotation test
teaches: rotation
intent: turn at start and reject near the wall
size: 5 2 2
rotations: 0-0
start-view: 0
y=0
z0: ###.#
z1: ..###
y=1
z0: ....#
z1: ...SM");
            _settings = ScriptableObject.CreateInstance<MovementSettings>();
            _pivot = new GameObject("Test View Pivot");
            _indicator = new GameObject("Test Rejection Indicator");
            _indicator.transform.SetParent(_pivot.transform);
            _playerObject = new GameObject(nameof(ViewRotatorTests));
            _playerObject.SetActive(false);

            _player = _playerObject.AddComponent<PlayerController2D>();
            var playerData = new SerializedObject(_player);
            playerData.FindProperty("_zoneData").objectReferenceValue = _layout;
            playerData.FindProperty("_settings").objectReferenceValue = _settings;
            playerData.ApplyModifiedPropertiesWithoutUndo();

            _rotator = _playerObject.AddComponent<ViewRotator>();
            var rotatorData = new SerializedObject(_rotator);
            rotatorData.FindProperty("_viewPivot").objectReferenceValue = _pivot.transform;
            rotatorData.FindProperty("_rejectedIndicator").objectReferenceValue = _indicator;
            rotatorData.ApplyModifiedPropertiesWithoutUndo();
            _input = _playerObject.GetComponent<PlayerInputReader>();
            _playerObject.SetActive(true);
        }

        public override void TearDown()
        {
            try
            {
                Object.DestroyImmediate(_playerObject);
                Object.DestroyImmediate(_pivot);
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
        public IEnumerator Rotate_Grounded_LocksInputUntilQuarterTurnEnds()
        {
            var before = _player.transform.localPosition;
            Keys(Key.E);
            yield return null;
            Assert.That(_rotator.IsRotating, Is.True);
            Assert.That(_input.IsInputLocked, Is.True);

            Keys(Key.A, Key.Q, Key.Space, Key.R);
            for (var frame = 0; frame < 8; frame++)
                yield return null;
            Assert.That(_player.transform.localPosition, Is.EqualTo(before));
            Assert.That(_player.Direction, Is.EqualTo(ViewDirection.Degrees0));
            Assert.That(_rotator.IsRotating, Is.True);
            Assert.That(Quaternion.Angle(Quaternion.identity, _pivot.transform.localRotation), Is.GreaterThan(0f));

            Keys();
            for (var frame = 0; frame < 60 && _rotator.IsRotating; frame++)
                yield return null;
            Assert.That(_input.IsInputLocked, Is.False);
            Assert.That(_player.Direction, Is.EqualTo(ViewDirection.Degrees90));
            Assert.That(Quaternion.Angle(Quaternion.Euler(0f, -90f, 0f), _pivot.transform.localRotation),
                Is.LessThan(0.1f));
        }

        [UnityTest]
        public IEnumerator Rotate_BlockedView_ShowsRejectionAndDoesNotMovePlayer()
        {
            Keys(Key.A);
            for (var frame = 0; frame < 30 && _player.Feet.x == 3; frame++)
                yield return null;
            Assert.That(_player.Feet, Is.EqualTo(new Vector3Int(2, 1, 0)));
            Keys();
            yield return null;
            var before = _player.transform.localPosition;

            Keys(Key.E);
            yield return null;

            Assert.That(_indicator.activeSelf, Is.True);
            Assert.That(_rotator.IsRotating, Is.False);
            Assert.That(_input.IsInputLocked, Is.False);
            Assert.That(_player.Direction, Is.EqualTo(ViewDirection.Degrees0));
            Assert.That(_player.transform.localPosition, Is.EqualTo(before));
        }

        [UnityTest]
        public IEnumerator Rotate_PressedJustBeforeLanding_StartsWhenGrounded()
        {
            Keys(Key.Space);
            yield return null;
            Keys();
            for (var frame = 0; frame < 30; frame++)
                yield return null;
            Assert.That(_player.IsGrounded, Is.False);

            Keys(Key.Q);
            yield return null;
            Assert.That(_rotator.IsRotating, Is.False);
            Keys();
            for (var frame = 0; frame < 60 && _player.Direction == ViewDirection.Degrees0; frame++)
                yield return null;

            Assert.That(_player.Direction, Is.EqualTo(ViewDirection.Degrees270));
        }

        [UnityTest]
        public IEnumerator Rotate_PressedAtTakeoff_ExpiresBeforeLanding()
        {
            Keys(Key.Space, Key.E);
            yield return null;
            Assert.That(_player.IsGrounded, Is.False);
            Keys();
            for (var frame = 0; frame < 60; frame++)
                yield return null;

            Assert.That(_player.IsGrounded, Is.True);
            Assert.That(_player.Direction, Is.EqualTo(ViewDirection.Degrees0));
            Assert.That(_rotator.IsRotating, Is.False);
        }

        private void Keys(params Key[] keys) =>
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys));
    }
}
#endif
