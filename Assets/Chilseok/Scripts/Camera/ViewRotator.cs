using Chilseok.Player;
using Chilseok.Puzzle;
using UnityEngine;

namespace Chilseok.CameraSystem
{
    [DefaultExecutionOrder(5)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController2D))]
    public sealed class ViewRotator : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("0도에서 구간을 +Z 방향으로 보는 카메라를 자식으로 둔 회전 중심.")]
        private Transform _viewPivot;

        [SerializeField]
        [Tooltip("회전이 막혔을 때 표시할 오브젝트. 다른 발 칸으로 이동하거나 다시 시도하면 숨긴다.")]
        private GameObject _rejectedIndicator;

        private PlayerController2D _player;
        private PlayerInputReader _input;
        private MovementSettings _settings;
        private int _requestedStep;
        private float _bufferRemaining;
        private RotationResult _pending;
        private float _elapsed;
        private float _duration;
        private float _arc;
        private Vector3Int? _rejectedFeet;

        public bool IsRotating { get; private set; }

        private void Awake()
        {
            _player = GetComponent<PlayerController2D>();
            _input = GetComponent<PlayerInputReader>();
            _settings = _player.Settings;
            if (_viewPivot == null || !_player.isActiveAndEnabled)
            {
                Debug.LogWarning("[ViewRotator] 회전 중심과 활성 플레이어를 연결하세요.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            _input.RotateLeftPressed += RequestLeft;
            _input.RotateRightPressed += RequestRight;
            _input.ResetPressed += ClearRequest;
            SetAngle((int)_player.Direction);
            ShowRejection(false);
        }

        private void OnDisable()
        {
            _input.RotateLeftPressed -= RequestLeft;
            _input.RotateRightPressed -= RequestRight;
            _input.ResetPressed -= ClearRequest;
            if (IsRotating)
            {
                SetAngle((int)_player.Direction);
                _input.SetInputLocked(false);
                IsRotating = false;
            }
            ClearRequest();
        }

        private void Update() => Tick(Time.deltaTime);

        private void Tick(float dt)
        {
            if (!_player.isActiveAndEnabled)
            {
                enabled = false;
                return;
            }

            if (IsRotating)
            {
                _elapsed += dt;
                var progress = Mathf.Clamp01(_elapsed / _duration);
                SetAngle((int)_player.Direction + _arc * progress);
                if (progress >= 1f)
                {
                    _player.ApplyRotation(_pending);
                    IsRotating = false;
                    _input.SetInputLocked(false);
                }
                return;
            }

            // 낙하나 수동 리셋이 시작 시점으로 돌아가면 카메라도 같은 프레임에 맞춘다.
            SetAngle((int)_player.Direction);
            if (_rejectedFeet.HasValue && _rejectedFeet.Value != _player.Feet)
                ShowRejection(false);

            if (_requestedStep == 0)
                return;

            var step = _requestedStep;
            var target = (ViewDirection)(((int)_player.Direction + 90 * step + 360) % 360);
            var result = _player.PreviewRotation(target);
            if (result.Outcome == RotationOutcome.RejectedInAir)
            {
                _bufferRemaining -= dt;
                if (_bufferRemaining <= 0f)
                    ClearRequest();
                return;
            }

            ClearRequest();
            if (!result.Succeeded)
            {
                ShowRejection(true);
                return;
            }

            _pending = result;
            _arc = 90f * step;
            _elapsed = 0f;
            _duration = _settings.RotationSeconds;
            IsRotating = true;
            _input.SetInputLocked(true);
        }

        private void RequestLeft() => Request(-1);
        private void RequestRight() => Request(1);

        private void Request(int step)
        {
            ShowRejection(false);
            _requestedStep = step;
            _bufferRemaining = _settings.RotationBufferSeconds;
        }

        private void ClearRequest()
        {
            _requestedStep = 0;
            _bufferRemaining = 0f;
            ShowRejection(false);
        }

        private void ShowRejection(bool show)
        {
            _rejectedFeet = show ? _player.Feet : null;
            if (_rejectedIndicator != null)
                _rejectedIndicator.SetActive(show);
        }

        private void SetAngle(float degrees) =>
            _viewPivot.localRotation = Quaternion.Euler(0f, -degrees, 0f);
    }
}
