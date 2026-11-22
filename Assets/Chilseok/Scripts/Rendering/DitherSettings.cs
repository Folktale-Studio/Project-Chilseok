using UnityEngine;

namespace Chilseok.Rendering
{
    public sealed class DitherSettings : ScriptableObject
    {
        private enum BayerMatrix
        {
            Size4x4 = 4,
            Size8x8 = 8
        }

        [SerializeField, Range(0f, 1f)]
        [Tooltip("디더 패턴이 나타나는 밝기 범위. 0~1, 0은 점 없는 두 색, 1은 전체 패턴이다.")]
        private float _strength = 1f;

        [SerializeField, Range(-1f, 1f)]
        [Tooltip("정규화 밝기 오프셋. -1~+1, 0이면 원본 밝기다.")]
        private float _brightness;

        [SerializeField, Range(0f, 1f)]
        [Tooltip("먹 색으로 바뀌는 진행도. 0~1, 0이면 장면 전체가 보인다.")]
        private float _dissolve;

        [SerializeField]
        [Tooltip("먹과 한지의 자리를 바꾼다. 끔 또는 켬.")]
        private bool _invert;

        [SerializeField]
        [Tooltip("디더 격자 해상도(px). 가로 320~640, 세로 180~360.")]
        private Vector2Int _internalResolution = new Vector2Int(480, 270);

        [SerializeField]
        [Tooltip("Bayer 격자의 한 변 크기. 4×4 또는 8×8.")]
        private BayerMatrix _bayerSize = BayerMatrix.Size4x4;

        [SerializeField, Range(0f, 1f)]
        [Tooltip("디더의 밝고 어두운 판정을 흔드는 정규화 강도. 0~1, 0이면 정지한다.")]
        private float _boiling;

        public float Strength => Mathf.Clamp01(_strength);
        public float Brightness => Mathf.Clamp(_brightness, -1f, 1f);
        public float Dissolve => Mathf.Clamp01(_dissolve);
        public bool Invert => _invert;
        public Vector2Int InternalResolution => new Vector2Int(
            Mathf.Clamp(_internalResolution.x, 320, 640), Mathf.Clamp(_internalResolution.y, 180, 360));
        public int BayerSize => _bayerSize == BayerMatrix.Size8x8 ? 8 : 4;
        public float Boiling => Mathf.Clamp01(_boiling);

        private void OnValidate()
        {
            _internalResolution = InternalResolution;
        }
    }
}
