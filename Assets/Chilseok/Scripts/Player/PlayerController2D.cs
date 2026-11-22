using Chilseok.Puzzle;
using UnityEngine;

namespace Chilseok.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputReader))]
    public sealed class PlayerController2D : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("이 캐릭터가 시작할 구간 격자 텍스트.")]
        private TextAsset _zoneData;

        [SerializeField]
        [Tooltip("이동·점프·중력·점프 유예 수치 에셋.")]
        private MovementSettings _settings;

        private PlayerInputReader _input;
        private PlayerMovement _movement;
        private Vector3Int _origin;
        private bool _jumpPressed;

        public Vector3Int Feet => _movement.Feet;
        public ViewDirection Direction => _movement.Direction;
        public bool IsGrounded => _movement != null && _movement.IsGrounded;
        internal MovementSettings Settings => _settings;
        internal TextAsset StartingLayout => _zoneData;
        internal PlayerMovement Movement => _movement;
        internal Vector3Int RouteFeet => _origin + _movement.Feet;
        internal float HorizontalInput => _input.HorizontalInput;

        private void Awake()
        {
            _input = GetComponent<PlayerInputReader>();
            if (_zoneData == null || _settings == null)
            {
                Debug.LogWarning("[PlayerController2D] 구간 텍스트와 MovementSettings를 연결하세요.", this);
                enabled = false;
                return;
            }

            try
            {
                _movement = new PlayerMovement(ZoneParser.Parse(_zoneData.text));
                ApplyPosition();
            }
            catch (System.FormatException error)
            {
                Debug.LogWarning($"[PlayerController2D] {_zoneData.name}: {error.Message}", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            _input.JumpPressed += RequestJump;
            _input.ResetPressed += ResetToStart;
        }

        private void OnDisable()
        {
            _input.JumpPressed -= RequestJump;
            _input.ResetPressed -= ResetToStart;
            _jumpPressed = false;
        }

        private void Update()
        {
            if (_input.IsInputLocked)
            {
                _jumpPressed = false;
                return;
            }

            _movement.Tick(Time.deltaTime, _input.HorizontalInput, _jumpPressed, _settings.Read());
            _jumpPressed = false;
            ApplyPosition();
        }

        private void RequestJump() => _jumpPressed = true;

        internal bool TryEnterSegment(PlayerMovement target, Vector3Int origin)
        {
            if (!_movement.IsAtRestOnSupport || !target.TryEnter(RouteFeet - origin))
                return false;

            _movement = target;
            _origin = origin;
            _jumpPressed = false;
            ApplyPosition();
            return true;
        }

        internal RotationResult PreviewRotation(ViewDirection direction) => _movement.PreviewRotation(direction);

        internal void ApplyRotation(RotationResult rotation)
        {
            _jumpPressed = false;
            _movement.ApplyRotation(rotation, _settings.Read().Gravity);
            ApplyPosition();
        }

        private void ResetToStart()
        {
            _jumpPressed = false;
            _movement.Reset();
            ApplyPosition();
        }

        private void ApplyPosition() => transform.localPosition = _origin + _movement.Position;
    }
}
