using System;
using UnityEngine;

namespace Chilseok.Puzzle
{
    public sealed class ProjectionMap
    {
        private readonly Vector3Int _zoneSize;
        private readonly Vector3Int?[,] _nearestBlocks;

        public ViewDirection Direction { get; }
        public int Width { get; }
        public int Height => _zoneSize.y;

        public ProjectionMap(ZoneData zone, ViewDirection direction)
        {
            if (zone == null)
            {
                throw new ArgumentNullException(nameof(zone));
            }

            Direction = direction;
            _zoneSize = zone.Size;
            Width = direction switch
            {
                ViewDirection.Degrees0 or ViewDirection.Degrees180 => _zoneSize.x,
                ViewDirection.Degrees90 or ViewDirection.Degrees270 => _zoneSize.z,
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "지원하지 않는 시점입니다.")
            };
            _nearestBlocks = new Vector3Int?[Width, Height];
            Build(zone);
        }

        public Vector2Int Project(Vector3Int position)
        {
            var screenX = Direction switch
            {
                ViewDirection.Degrees0 => position.x,
                ViewDirection.Degrees90 => position.z,
                ViewDirection.Degrees180 => _zoneSize.x - 1 - position.x,
                ViewDirection.Degrees270 => _zoneSize.z - 1 - position.z,
                _ => throw new InvalidOperationException()
            };
            return new Vector2Int(screenX, position.y);
        }

        public bool IsSolid(Vector2Int cell) => TryGetNearestBlock(cell, out _);

        public bool TryGetNearestBlock(Vector2Int cell, out Vector3Int block)
        {
            if (cell.x >= 0 && cell.x < Width && cell.y >= 0 && cell.y < Height &&
                _nearestBlocks[cell.x, cell.y] is Vector3Int nearest)
            {
                block = nearest;
                return true;
            }

            block = default;
            return false;
        }

        private void Build(ZoneData zone)
        {
            for (var y = 0; y < _zoneSize.y; y++)
            {
                for (var z = 0; z < _zoneSize.z; z++)
                {
                    for (var x = 0; x < _zoneSize.x; x++)
                    {
                        var position = new Vector3Int(x, y, z);
                        if (!zone.IsSolid(position))
                        {
                            continue;
                        }

                        var cell = Project(position);
                        var nearest = _nearestBlocks[cell.x, cell.y];
                        if (!nearest.HasValue || CameraDepth(position) < CameraDepth(nearest.Value))
                        {
                            _nearestBlocks[cell.x, cell.y] = position;
                        }
                    }
                }
            }
        }

        // 시점마다 가까운 블록의 값이 더 작아지도록 깊이 축의 부호를 맞춘다.
        private int CameraDepth(Vector3Int position) => Direction switch
        {
            ViewDirection.Degrees0 => position.z,
            ViewDirection.Degrees90 => -position.x,
            ViewDirection.Degrees180 => -position.z,
            ViewDirection.Degrees270 => position.x,
            _ => throw new InvalidOperationException()
        };
    }
}
