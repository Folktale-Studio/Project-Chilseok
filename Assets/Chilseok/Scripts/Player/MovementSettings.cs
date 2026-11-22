using Chilseok.Puzzle;
using UnityEngine;

namespace Chilseok.Player
{
    [CreateAssetMenu(menuName = "Chilseok/Movement Settings")]
    public sealed class MovementSettings : ScriptableObject
    {
        [SerializeField, Range(3f, 5f)]
        [Tooltip("걷기 속도. 3~5칸/초. 점프 착지는 1칸 높이·틈을 허용하고 2칸을 막는 격자 규칙을 따른다.")]
        private float _moveSpeed = 4f;

        [SerializeField, Range(1.1f, 1.4f)]
        [Tooltip("점프 포물선 최고 높이. 1.1~1.4칸. 1칸 높이·틈을 허용하고 2칸을 막는다.")]
        private float _jumpHeight = 1.25f;

        [SerializeField, Range(1.8f, 2.4f)]
        [Tooltip("점프 가로 거리 기준. 1.8~2.4칸. 가로 접근 속도를 바꾸며 착지 판정은 1칸 높이·틈을 허용하고 2칸을 막는다.")]
        private float _jumpDistance = 2f;

        [SerializeField, Range(25f, 40f)]
        [Tooltip("점프와 낙하 중력. 25~40칸/초². 착지 판정은 1칸 높이·틈을 허용하고 2칸을 막는 격자 규칙을 따른다.")]
        private float _gravity = 30f;

        [SerializeField, Range(0.05f, 0.12f)]
        [Tooltip("발판을 벗어난 뒤 점프 유예 시간. 0.05~0.12초. 유예 중에도 1칸 높이·틈만 넘고 2칸은 못 넘는다.")]
        private float _coyoteSeconds = 0.08f;

        [SerializeField, Range(0.25f, 0.5f)]
        [Tooltip("90도 회전에 걸리는 시간. 0.25~0.5초. 회전이 끝날 때까지 입력을 잠근다.")]
        private float _rotationSeconds = 0.35f;

        [SerializeField, Range(0.1f, 0.2f)]
        [Tooltip("공중에서 누른 회전 입력을 착지까지 보관하는 시간. 0.1~0.2초.")]
        private float _rotationBufferSeconds = 0.15f;

        public float RotationSeconds => Mathf.Clamp(_rotationSeconds, 0.25f, 0.5f);
        public float RotationBufferSeconds => Mathf.Clamp(_rotationBufferSeconds, 0.1f, 0.2f);

        public PlayerMovement.Tuning Read() => new(
            Mathf.Clamp(_moveSpeed, 3f, 5f), Mathf.Clamp(_jumpHeight, 1.1f, 1.4f),
            Mathf.Clamp(_jumpDistance, 1.8f, 2.4f), Mathf.Clamp(_gravity, 25f, 40f),
            Mathf.Clamp(_coyoteSeconds, 0.05f, 0.12f));
    }
}
