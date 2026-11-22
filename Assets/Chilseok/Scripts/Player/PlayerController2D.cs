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
        private bool _jumpPressed;

        public Vector3Int Feet => _movement.Feet;
        public ViewDirection Direction => _movement.Direction;
        public bool IsGrounded => _movement != null && _movement.IsGrounded;

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
                transform.localPosition = _movement.Position;
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
            _movement.Tick(Time.deltaTime, _input.HorizontalInput, _jumpPressed, _settings.Read());
            _jumpPressed = false;
            transform.localPosition = _movement.Position;
        }

        private void RequestJump() => _jumpPressed = true;

        private void ResetToStart()
        {
            _jumpPressed = false;
            _movement.Reset();
            transform.localPosition = _movement.Position;
        }
    }
}
