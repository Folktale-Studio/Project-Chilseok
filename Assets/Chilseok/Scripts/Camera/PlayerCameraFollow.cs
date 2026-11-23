using UnityEngine;

namespace Chilseok.CameraSystem
{
    [DisallowMultipleComponent]
    public sealed class PlayerCameraFollow : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("카메라가 따라갈 플레이어의 발 기준 Transform.")]
        private Transform _target;

        [SerializeField, Range(0f, 0.5f)]
        [Tooltip("플레이어를 따라가는 카메라 위치 보간 시간. 0~0.5초, 0은 즉시 추적한다.")]
        private float _smoothSeconds = 0.2f;

        private Vector3 _velocity;
        private float _heightOffset;

        private void Start()
        {
            if (_target == null)
            {
                Debug.LogWarning("[PlayerCameraFollow] 따라갈 플레이어를 연결하세요.", this);
                enabled = false;
                return;
            }

            _heightOffset = transform.position.y - _target.position.y;
        }

        private void OnDisable() => _velocity = Vector3.zero;

        private void LateUpdate()
        {
            if (_target == null || Time.deltaTime <= 0f)
                return;

            var target = _target.position + Vector3.up * _heightOffset;
            var smoothing = Mathf.Clamp(_smoothSeconds, 0f, 0.5f);
            if (smoothing == 0f)
            {
                _velocity = Vector3.zero;
                transform.position = target;
                return;
            }

            transform.position = Vector3.SmoothDamp(transform.position, target, ref _velocity, smoothing);
        }
    }
}
