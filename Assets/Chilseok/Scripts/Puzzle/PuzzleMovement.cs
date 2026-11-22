using UnityEngine;

namespace Chilseok.Puzzle
{
    public static class PuzzleMovement
    {
        private const int ResetHeight = -3;

        public static bool TryWalk(ProjectionMap map, Vector3Int feet, int direction, out Vector3Int next)
        {
            next = feet;
            if (!TryGetAdjacent(map, feet, direction, out var adjacent) || map.IsSolid(map.Project(adjacent)))
            {
                return false;
            }

            Reprojector.TryUpdateDepth(map, adjacent, out next);
            return true;
        }

        public static bool TryStepUp(ProjectionMap map, Vector3Int feet, int direction, out Vector3Int next)
        {
            next = feet;
            if (!TryGetAdjacent(map, feet, direction, out var adjacent))
            {
                return false;
            }

            var cell = map.Project(feet);
            var obstacleCell = map.Project(adjacent);
            if (!map.IsSolid(obstacleCell) || map.IsSolid(obstacleCell + Vector2Int.up) ||
                map.IsSolid(cell + Vector2Int.up))
            {
                return false;
            }

            return Reprojector.TryUpdateDepth(map, adjacent + Vector3Int.up, out next);
        }

        public static bool TryJumpGap(ProjectionMap map, Vector3Int feet, int direction, out Vector3Int next)
        {
            next = feet;
            if (!TryGetAdjacent(map, feet, direction, out var adjacent))
            {
                return false;
            }

            var gap = map.Project(adjacent);
            var landing = OffsetHorizontally(map.Direction, adjacent, direction);
            var landingCell = map.Project(landing);
            if (!IsWithinWidth(map, landingCell.x) || map.IsSolid(landingCell) || map.IsSolid(gap) ||
                map.IsSolid(gap + Vector2Int.down) || map.IsSolid(gap + Vector2Int.up) ||
                map.IsSolid(map.Project(feet) + Vector2Int.up))
            {
                return false;
            }

            if (!Reprojector.TryUpdateDepth(map, landing, out var resolved))
            {
                return false;
            }

            next = resolved;
            return true;
        }

        public static bool TryFall(ProjectionMap map, Vector3Int feet, out Vector3Int next)
        {
            next = feet;
            var cell = map.Project(feet);
            if (HasFallenOut(feet) || map.IsSolid(cell) || map.IsSolid(cell + Vector2Int.down))
            {
                return false;
            }

            // 공중에서는 깊이를 유지하고, 처음 닿는 받침에서만 깊이를 갱신한다.
            for (var y = feet.y - 1; y >= ResetHeight; y--)
            {
                var candidate = new Vector3Int(feet.x, y, feet.z);
                if (Reprojector.TryUpdateDepth(map, candidate, out var grounded))
                {
                    next = grounded;
                    return true;
                }
            }

            next = new Vector3Int(feet.x, ResetHeight - 1, feet.z);
            return true;
        }

        internal static bool HasFallenOut(Vector3Int feet) => feet.y < ResetHeight;

        private static bool TryGetAdjacent(ProjectionMap map, Vector3Int feet, int direction, out Vector3Int next)
        {
            next = feet;
            var cell = map.Project(feet);
            if ((direction != -1 && direction != 1) || !IsWithinWidth(map, cell.x + direction) ||
                map.IsSolid(cell) || !map.IsSolid(cell + Vector2Int.down))
            {
                return false;
            }

            next = OffsetHorizontally(map.Direction, feet, direction);
            return true;
        }

        private static bool IsWithinWidth(ProjectionMap map, int x) => x >= 0 && x < map.Width;

        private static Vector3Int OffsetHorizontally(ViewDirection view, Vector3Int feet, int delta) => view switch
        {
            ViewDirection.Degrees0 => feet + new Vector3Int(delta, 0, 0),
            ViewDirection.Degrees90 => feet + new Vector3Int(0, 0, delta),
            ViewDirection.Degrees180 => feet - new Vector3Int(delta, 0, 0),
            ViewDirection.Degrees270 => feet - new Vector3Int(0, 0, delta),
            _ => throw new System.ArgumentOutOfRangeException(nameof(view))
        };
    }
}
