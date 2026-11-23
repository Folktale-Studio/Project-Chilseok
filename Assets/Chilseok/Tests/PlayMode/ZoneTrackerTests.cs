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
    public sealed class ZoneTrackerTests : InputTestFixture
    {
        private GameObject _playerObject;
        private GameObject _pivot;
        private TextAsset _firstZone;
        private TextAsset _connector;
        private TextAsset _secondZone;
        private MovementSettings _settings;
        private Keyboard _keyboard;
        private PlayerController2D _player;
        private ZoneTracker _tracker;
        private ViewRotator _rotator;
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
                Object.DestroyImmediate(_playerObject);
                Object.DestroyImmediate(_pivot);
                Object.DestroyImmediate(_firstZone);
                Object.DestroyImmediate(_connector);
                Object.DestroyImmediate(_secondZone);
                Object.DestroyImmediate(_settings);
                Time.captureDeltaTime = _previousCaptureDeltaTime;
            }
            finally
            {
                base.TearDown();
            }
        }

        [UnityTest]
        public IEnumerator Walk_ThroughBothSharedCells_KeepsPositionContinuous()
        {
            CreateRoute();
            Assert.That(_tracker.ActiveZone, Is.SameAs(_firstZone));

            Keys(Key.D);
            for (var frame = 0; frame < 150 && _tracker.ActiveSegmentIndex < 2; frame++)
            {
                var previous = _player.transform.localPosition;
                yield return null;
                var current = _player.transform.localPosition;
                Assert.That(current.x - previous.x, Is.InRange(-0.001f, 0.07f));
                Assert.That(current.y, Is.EqualTo(previous.y));
                Assert.That(current.z, Is.EqualTo(previous.z));
                if (_tracker.ActiveSegmentIndex == 1)
                    Assert.That(_tracker.ActiveZone, Is.Null);
            }

            Assert.That(_tracker.ActiveZone, Is.SameAs(_secondZone));
            Assert.That(_player.transform.localPosition.x, Is.EqualTo(6f));
        }

        [UnityTest]
        public IEnumerator Rotate_HeldFromConnector_IsDiscardedUntilNewPress()
        {
            CreateRoute();
            Keys(Key.D);
            yield return WaitForSegment(1);
            Keys(Key.D, Key.E);
            yield return null;

            Assert.That(_tracker.CanRotate, Is.False);
            Assert.That(_rotator.IsRotating, Is.False);
            yield return WaitForSegment(2);
            Keys(Key.E);
            for (var frame = 0; frame < 26; frame++)
                yield return null;

            Assert.That(_tracker.CanRotate, Is.True);
            Assert.That(_player.Direction, Is.EqualTo(ViewDirection.Degrees0));
            Assert.That(_rotator.IsRotating, Is.False);

            Keys();
            yield return null;
            Keys(Key.E);
            yield return null;
            Assert.That(_rotator.IsRotating, Is.True);
        }

        [UnityTest]
        public IEnumerator Walk_IntoReversedStartView_DoesNotBounceBackOnHeldInput()
        {
            CreateRoute(secondStartView: 180);
            Keys(Key.D);
            yield return WaitForSegment(2);
            for (var frame = 0; frame < 10; frame++)
                yield return null;

            Assert.That(_player.Direction, Is.EqualTo(ViewDirection.Degrees180));
            Assert.That(_tracker.ActiveSegmentIndex, Is.EqualTo(2));
            Assert.That(_player.transform.localPosition.x, Is.EqualTo(6f));
        }

        [UnityTest]
        public IEnumerator LateUpdate_InConnector_CameraFollowsAndSettles()
        {
            CreateRoute();
            Keys(Key.D);
            yield return WaitForSegment(1);
            for (var frame = 0; frame < 10; frame++)
                yield return null;

            Assert.That(_pivot.transform.position.x, Is.GreaterThan(1f));
            Assert.That(_pivot.transform.position.x, Is.LessThan(_player.transform.position.x));
            Keys();
            for (var frame = 0; frame < 90; frame++)
                yield return null;

            Assert.That(_tracker.ActiveSegmentIndex, Is.EqualTo(1));
            Assert.That(_pivot.transform.position.x, Is.EqualTo(_player.transform.position.x).Within(0.01f));
        }

        private IEnumerator WaitForSegment(int expected)
        {
            for (var frame = 0; frame < 120 && _tracker.ActiveSegmentIndex < expected; frame++)
                yield return null;
            Assert.That(_tracker.ActiveSegmentIndex, Is.EqualTo(expected));
        }

        private void CreateRoute(int secondStartView = 0)
        {
            _firstZone = CreateLayout(0);
            _connector = CreateLayout(0);
            _secondZone = CreateLayout(secondStartView);
            _settings = ScriptableObject.CreateInstance<MovementSettings>();
            _pivot = new GameObject("Test View Pivot");
            _pivot.transform.position = new Vector3(1f, 2f, 0f);
            _playerObject = new GameObject(nameof(ZoneTrackerTests));
            _playerObject.SetActive(false);

            _player = _playerObject.AddComponent<PlayerController2D>();
            var playerData = new SerializedObject(_player);
            playerData.FindProperty("_zoneData").objectReferenceValue = _firstZone;
            playerData.FindProperty("_settings").objectReferenceValue = _settings;
            playerData.ApplyModifiedPropertiesWithoutUndo();

            _tracker = _playerObject.AddComponent<ZoneTracker>();
            var routeData = new SerializedObject(_tracker);
            var segments = routeData.FindProperty("_segments");
            segments.arraySize = 3;
            BindSegment(segments.GetArrayElementAtIndex(0), _firstZone, 0, false);
            BindSegment(segments.GetArrayElementAtIndex(1), _connector, 3, true);
            BindSegment(segments.GetArrayElementAtIndex(2), _secondZone, 6, false);
            routeData.ApplyModifiedPropertiesWithoutUndo();

            _rotator = _playerObject.AddComponent<ViewRotator>();
            var rotationData = new SerializedObject(_rotator);
            rotationData.FindProperty("_viewPivot").objectReferenceValue = _pivot.transform;
            rotationData.ApplyModifiedPropertiesWithoutUndo();

            var follow = _pivot.AddComponent<PlayerCameraFollow>();
            var followData = new SerializedObject(follow);
            followData.FindProperty("_target").objectReferenceValue = _playerObject.transform;
            followData.ApplyModifiedPropertiesWithoutUndo();
            _playerObject.SetActive(true);
        }

        private static void BindSegment(SerializedProperty segment, TextAsset layout, int x, bool connector)
        {
            segment.FindPropertyRelative("_layout").objectReferenceValue = layout;
            segment.FindPropertyRelative("_origin").vector3IntValue = new Vector3Int(x, 0, 0);
            segment.FindPropertyRelative("_connector").boolValue = connector;
        }

        private static TextAsset CreateLayout(int startView) => new($@"name: tracking
teaches: tracking
intent: walk through the shared boundary
size: 4 2 1
rotations: 0-0
start-view: {startView}
y=0
z0: ####
y=1
z0: .S.M");

        private void Keys(params Key[] keys) =>
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys));
    }
}
#endif
