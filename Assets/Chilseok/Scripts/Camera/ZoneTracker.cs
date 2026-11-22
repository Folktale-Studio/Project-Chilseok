using System;
using Chilseok.Player;
using Chilseok.Puzzle;
using UnityEngine;

namespace Chilseok.CameraSystem
{
    [DefaultExecutionOrder(10)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController2D))]
    public sealed class ZoneTracker : MonoBehaviour
    {
        [Serializable]
        private struct Segment
        {
            [SerializeField]
            [Tooltip("구간 또는 연결 길의 격자 텍스트.")]
            private TextAsset _layout;

            [SerializeField]
            [Tooltip("플레이어 부모 기준 격자 원점. 인접 구간과 X 끝 칸 하나를 공유해야 한다.")]
            private Vector3Int _origin;

            [SerializeField]
            [Tooltip("연결 길이면 0도 이동을 유지하고 회전 입력을 받지 않는다.")]
            private bool _connector;

            public TextAsset Layout => _layout;
            public Vector3Int Origin => _origin;
            public bool IsConnector => _connector;
        }

        [SerializeField]
        [Tooltip("월드 +X 순서로 이어진 구간과 연결 길. 첫 항목은 플레이어 시작 구간이다.")]
        private Segment[] _segments;

        private PlayerController2D _player;
        private PlayerMovement[] _movements;
        private int _activeIndex;
        private int _heldDirectionAtViewChange;

        public event Action SegmentChanged;

        public bool CanRotate => isActiveAndEnabled && _movements != null && !_segments[_activeIndex].IsConnector;
        public TextAsset ActiveZone => CanRotate ? _segments[_activeIndex].Layout : null;
        public int ActiveSegmentIndex => _activeIndex;

        private void Awake()
        {
            _player = GetComponent<PlayerController2D>();
            if (!_player.isActiveAndEnabled || _segments == null || _segments.Length == 0 ||
                _segments[0].Layout != _player.StartingLayout || _segments[0].Origin != Vector3Int.zero)
            {
                DisableForInvalidRoute("첫 구간에 플레이어의 시작 텍스트와 원점 (0, 0, 0)을 지정하세요.");
                return;
            }

            var movements = new PlayerMovement[_segments.Length];
            movements[0] = _player.Movement;
            for (var i = 0; i < _segments.Length; i++)
            {
                var segment = _segments[i];
                if (segment.Layout == null)
                {
                    DisableForInvalidRoute($"{i + 1}번째 구간의 격자 텍스트를 연결하세요.");
                    return;
                }

                try
                {
                    if (i > 0)
                    {
                        var previous = _segments[i - 1];
                        if (segment.Origin.x != previous.Origin.x + movements[i - 1].Size.x - 1)
                        {
                            DisableForInvalidRoute($"{i}번째와 {i + 1}번째 구간은 X 끝 칸 하나를 공유해야 합니다.");
                            return;
                        }
                        movements[i] = new PlayerMovement(ZoneParser.Parse(segment.Layout.text));
                    }
                }
                catch (FormatException error)
                {
                    DisableForInvalidRoute($"{segment.Layout.name}: {error.Message}");
                    return;
                }

                if (segment.IsConnector && movements[i].StartView != ViewDirection.Degrees0)
                {
                    DisableForInvalidRoute($"{segment.Layout.name} 연결 길의 시작 시점은 0도여야 합니다.");
                    return;
                }
            }
            _movements = movements;
        }

        private void Update()
        {
            if (!_player.isActiveAndEnabled)
                return;

            var input = _player.HorizontalInput;
            var inputDirection = Math.Sign(input);
            // 시점이 바뀌었을 때 계속 누른 입력으로 바로 경계를 되넘지 않게 한다.
            if (_heldDirectionAtViewChange != 0 && inputDirection == _heldDirectionAtViewChange)
                return;

            _heldDirectionAtViewChange = 0;
            if (inputDirection == 0 || !_player.Movement.IsAtRestOnSupport)
                return;

            // 회전된 화면에서의 좌우 입력을 경로의 +X/-X에 대응시킨다.
            var previousView = _player.Direction;
            var routeStep = previousView switch
            {
                ViewDirection.Degrees0 => inputDirection,
                ViewDirection.Degrees180 => -inputDirection,
                _ => 0
            };
            if (routeStep == 0)
                return;

            var nextIndex = _activeIndex + routeStep;
            if (nextIndex < 0 || nextIndex >= _segments.Length ||
                !_player.TryEnterSegment(_movements[nextIndex], _segments[nextIndex].Origin))
            {
                return;
            }

            _activeIndex = nextIndex;
            if (previousView != _player.Direction)
                _heldDirectionAtViewChange = inputDirection;
            SegmentChanged?.Invoke();
        }

        private void DisableForInvalidRoute(string message)
        {
            Debug.LogWarning($"[ZoneTracker] {message}", this);
            enabled = false;
        }
    }
}
