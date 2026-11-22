using System;
using System.Collections.Generic;
using UnityEngine;

namespace Chilseok.Puzzle
{
    public static class PuzzleSolver
    {
        public static PuzzleSolveResult Solve(ZoneData zone) => SolveCore(zone, null).Clear;

        public static PuzzleFeatherSolveResult SolveWithFeathers(ZoneData zone) =>
            SolveCore(zone, new FeatherCollection(zone));

        private static PuzzleFeatherSolveResult SolveCore(ZoneData zone, FeatherCollection collection)
        {
            if (zone == null)
            {
                throw new ArgumentNullException(nameof(zone));
            }

            var maps = new[]
            {
                new ProjectionMap(zone, ViewDirection.Degrees0),
                new ProjectionMap(zone, ViewDirection.Degrees90),
                new ProjectionMap(zone, ViewDirection.Degrees180),
                new ProjectionMap(zone, ViewDirection.Degrees270)
            };
            var states = new Dictionary<(Vector3Int, ViewDirection), SearchNode>();
            var pending = new Queue<SearchNode>();
            var goals = new List<SearchNode>();
            var start = new SearchNode(zone.Start, zone.StartView, 0, false, 0);
            states.Add((start.Feet, start.View), start);
            pending.Enqueue(start);

            while (pending.Count > 0)
            {
                var node = pending.Dequeue();
                if (node.IsGoal)
                {
                    goals.Add(node);
                    continue;
                }

                // 리셋으로 입구에 돌아가는 경로는 최단 경로를 줄이지 않는다.
                if (PuzzleMovement.HasFallenOut(node.Feet))
                {
                    continue;
                }

                var map = maps[(int)node.View / 90];
                if (map.IsSolid(map.Project(node.Feet)))
                {
                    continue;
                }

                if (PuzzleMovement.TryFall(map, node.Feet, out var landing))
                {
                    var next = PassesMagpie(node.Feet, landing, zone.Magpie) ? zone.Magpie : landing;
                    Connect(node, next, node.View, 0);
                    continue;
                }

                for (var direction = -1; direction <= 1; direction += 2)
                {
                    if (PuzzleMovement.TryWalk(map, node.Feet, direction, out var walked))
                    {
                        Connect(node, walked, node.View, 0);
                    }

                    if (PuzzleMovement.TryStepUp(map, node.Feet, direction, out var stepped))
                    {
                        Connect(node, stepped, node.View, 0);
                    }

                    if (PuzzleMovement.TryJumpGap(map, node.Feet, direction, out var jumped))
                    {
                        Connect(node, jumped, node.View, 0);
                    }

                    var targetIndex = ((int)node.View / 90 + direction + maps.Length) % maps.Length;
                    var rotation = Reprojector.Rotate(map, maps[targetIndex], node.Feet);
                    if (rotation.Succeeded)
                    {
                        Connect(node, rotation.Feet, rotation.Direction, 1);
                    }
                }
            }

            int? minimumActions = goals.Count == 0 ? null : goals[0].Actions;
            var deadEndRatio = (double)(states.Count - CountStatesLeadingToGoal(goals)) / states.Count;
            var minimumRotations = FindMinimumRotations(start, 0);
            var clear = new PuzzleSolveResult(minimumRotations, minimumActions, states.Count, deadEndRatio);
            int? minimumAllFeathers = null;
            if (collection != null && clear.CanClear)
            {
                minimumAllFeathers = collection.FullMask == 0
                    ? minimumRotations
                    : FindMinimumRotations(start, collection.FullMask);
            }

            return new PuzzleFeatherSolveResult(clear, minimumAllFeathers);

            void Connect(SearchNode from, Vector3Int feet, ViewDirection view, int rotationCost)
            {
                var key = (feet, view);
                if (!states.TryGetValue(key, out var to))
                {
                    // BFS에서 처음 만난 경로가 최소 행동 수를 갖는다.
                    var pickup = collection?.Collect(0, feet, view) ?? 0;
                    to = new SearchNode(feet, view, from.Actions + 1, feet == zone.Magpie, pickup);
                    states.Add(key, to);
                    pending.Enqueue(to);
                }

                from.Outgoing.Add(new Edge(to, rotationCost));
                to.Incoming.Add(from);
            }
        }

        private static bool PassesMagpie(Vector3Int from, Vector3Int landing, Vector3Int magpie) =>
            from.x == magpie.x && from.z == magpie.z && magpie.y < from.y && magpie.y > landing.y;

        private static int? FindMinimumRotations(SearchNode start, int requiredMask)
        {
            var deque = new LinkedList<(SearchNode Node, int Mask, int Cost)>();
            var rotations = new Dictionary<(SearchNode Node, int Mask), int> { [(start, 0)] = 0 };
            deque.AddFirst((start, 0, 0));
            while (deque.First != null)
            {
                var (node, mask, cost) = deque.First.Value;
                deque.RemoveFirst();
                if (cost != rotations[(node, mask)])
                {
                    continue;
                }

                if (node.IsGoal)
                {
                    if (mask == requiredMask)
                    {
                        return cost;
                    }

                    // 일부만 수집한 채 까치에 닿은 경로도 여기서 끝난다.
                    continue;
                }

                foreach (var edge in node.Outgoing)
                {
                    var nextCost = cost + edge.RotationCost;
                    var nextMask = mask | (edge.Target.PickupMask & requiredMask);
                    var next = (edge.Target, nextMask);
                    if (rotations.TryGetValue(next, out var recorded) && nextCost >= recorded)
                    {
                        continue;
                    }

                    rotations[next] = nextCost;
                    if (edge.RotationCost == 0)
                    {
                        deque.AddFirst((edge.Target, nextMask, nextCost));
                    }
                    else
                    {
                        deque.AddLast((edge.Target, nextMask, nextCost));
                    }
                }
            }

            return null;
        }

        private static int CountStatesLeadingToGoal(List<SearchNode> goals)
        {
            var reachingGoal = new HashSet<SearchNode>(goals);
            var reverseSearch = new Queue<SearchNode>(goals);
            while (reverseSearch.Count > 0)
            {
                foreach (var previous in reverseSearch.Dequeue().Incoming)
                {
                    if (reachingGoal.Add(previous))
                    {
                        reverseSearch.Enqueue(previous);
                    }
                }
            }

            return reachingGoal.Count;
        }

        private sealed class SearchNode
        {
            public Vector3Int Feet { get; }
            public ViewDirection View { get; }
            public int Actions { get; }
            public bool IsGoal { get; }
            public int PickupMask { get; }
            public List<Edge> Outgoing { get; } = new List<Edge>();
            public List<SearchNode> Incoming { get; } = new List<SearchNode>();

            public SearchNode(Vector3Int feet, ViewDirection view, int actions, bool isGoal, int pickupMask)
            {
                Feet = feet;
                View = view;
                Actions = actions;
                IsGoal = isGoal;
                PickupMask = pickupMask;
            }
        }

        private readonly struct Edge
        {
            public SearchNode Target { get; }
            public int RotationCost { get; }

            public Edge(SearchNode target, int rotationCost)
            {
                Target = target;
                RotationCost = rotationCost;
            }
        }
    }

    public readonly struct PuzzleSolveResult
    {
        public bool CanClear => MinimumRotations.HasValue;
        public int? MinimumRotations { get; }
        public int? MinimumActions { get; }
        public int ExploredStates { get; }
        public double DeadEndRatio { get; }

        internal PuzzleSolveResult(int? minimumRotations, int? minimumActions, int exploredStates, double deadEndRatio)
        {
            MinimumRotations = minimumRotations;
            MinimumActions = minimumActions;
            ExploredStates = exploredStates;
            DeadEndRatio = deadEndRatio;
        }
    }

    public readonly struct PuzzleFeatherSolveResult
    {
        public PuzzleSolveResult Clear { get; }
        public int? MinimumRotationsWithAllFeathers { get; }

        internal PuzzleFeatherSolveResult(PuzzleSolveResult clear, int? minimumRotationsWithAllFeathers)
        {
            Clear = clear;
            MinimumRotationsWithAllFeathers = minimumRotationsWithAllFeathers;
        }
    }
}
