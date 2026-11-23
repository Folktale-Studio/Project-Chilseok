using System;
using Chilseok.CameraSystem;
using Chilseok.Player;
using Chilseok.Puzzle;
using UnityEngine;

namespace Chilseok.Rendering
{
    [DisallowMultipleComponent]
    public sealed class ZonePreviewMarkers : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("시점과 활성 구간을 읽을 플레이어.")]
        private PlayerController2D _player;

        [SerializeField]
        [Tooltip("연결 길에서 구간 표식을 숨기는 데 쓰는 선택 참조.")]
        private ZoneTracker _tracker;

        [SerializeField]
        [Tooltip("미리보기에 사용한 구간 격자 텍스트.")]
        private TextAsset _layout;

        [SerializeField]
        [Tooltip("구간 파서가 읽은 순서대로 연결한 깃털 표식.")]
        private GameObject[] _feathers;

        private ZoneData _zone;
        private ViewDirection? _lastView;
        private bool _wasActive;

        private void Awake()
        {
            if (_player == null || _layout == null || _feathers == null)
            {
                DisableWithWarning("플레이어와 구간 텍스트, 깃털 표식을 연결하세요.");
                return;
            }

            try
            {
                _zone = ZoneParser.Parse(_layout.text);
            }
            catch (FormatException error)
            {
                DisableWithWarning(error.Message);
                return;
            }

            if (_zone.Feathers.Count != _feathers.Length)
                DisableWithWarning("구간 텍스트에서 읽은 깃털 수와 표식 개수가 다릅니다.");
        }

        private void OnEnable() => _lastView = null;

        private void LateUpdate()
        {
            if (!_player.isActiveAndEnabled)
                return;

            var view = _player.Direction;
            var active = _tracker == null || _tracker.ActiveZone == _layout;
            if (_lastView == view && _wasActive == active)
                return;

            _lastView = view;
            _wasActive = active;
            for (var i = 0; i < _feathers.Length; i++)
            {
                if (_feathers[i] != null)
                    _feathers[i].SetActive(active && _zone.Feathers[i].IsActive(view));
            }
        }

        private void DisableWithWarning(string message)
        {
            Debug.LogWarning($"[ZonePreviewMarkers] {message}", this);
            enabled = false;
        }
    }
}
