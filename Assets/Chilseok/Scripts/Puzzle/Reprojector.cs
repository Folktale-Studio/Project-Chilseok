using System;
using UnityEngine;

namespace Chilseok.Puzzle
{
    public static class Reprojector
    {
        public static bool TryUpdateDepth(ProjectionMap map, Vector3Int feet, out Vector3Int resolvedFeet)
        {
            var supportCell = map.Project(feet) + Vector2Int.down;
            if (map.TryGetNearestBlock(supportCell, out var support))
            {
                resolvedFeet = support + Vector3Int.up;
                return true;
            }

            resolvedFeet = feet;
            return false;
        }

        // 현재 맵과 목표 맵은 같은 구간에서 만들며, 목표 시점은 Q/E에 해당한다.
        public static RotationResult Rotate(ProjectionMap current, ProjectionMap target, Vector3Int feet)
        {
            var degrees = Math.Abs((int)current.Direction - (int)target.Direction);
            if (degrees != 90 && degrees != 270)
            {
                return new RotationResult(RotationOutcome.RejectedDirection, current.Direction, feet);
            }

            if (!current.IsSolid(current.Project(feet) + Vector2Int.down))
            {
                return new RotationResult(RotationOutcome.RejectedInAir, current.Direction, feet);
            }

            // 몸이 막히면 발밑 받침을 검사하기 전에 기존 시점과 위치를 유지한다.
            if (target.IsSolid(target.Project(feet)))
            {
                return new RotationResult(RotationOutcome.RejectedBlocked, current.Direction, feet);
            }

            var grounded = TryUpdateDepth(target, feet, out var resolvedFeet);
            var outcome = grounded ? RotationOutcome.Grounded : RotationOutcome.Falling;
            return new RotationResult(outcome, target.Direction, resolvedFeet);
        }
    }

    public enum RotationOutcome
    {
        RejectedDirection,
        RejectedInAir,
        RejectedBlocked,
        Grounded,
        Falling
    }

    public readonly struct RotationResult
    {
        public RotationOutcome Outcome { get; }
        public ViewDirection Direction { get; }
        public Vector3Int Feet { get; }
        public bool Succeeded => Outcome is RotationOutcome.Grounded or RotationOutcome.Falling;

        internal RotationResult(RotationOutcome outcome, ViewDirection direction, Vector3Int feet)
        {
            Outcome = outcome;
            Direction = direction;
            Feet = feet;
        }
    }
}
