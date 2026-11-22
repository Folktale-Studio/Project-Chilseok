using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Chilseok.Puzzle;
using NUnit.Framework;
using UnityEngine;

namespace Chilseok.Tests.EditMode
{
    public sealed class ZoneSolverTests
    {
        private static string LevelsPath => Path.Combine(Application.dataPath, "Chilseok", "Levels");

        private static IEnumerable<TestCaseData> ZoneFiles()
        {
            var files = Directory.GetFiles(LevelsPath, "zone-*.txt");
            Array.Sort(files, StringComparer.Ordinal);
            foreach (var path in files)
            {
                yield return new TestCaseData(path).SetName($"Solve_{Path.GetFileNameWithoutExtension(path)}_MatchesRotationGoal");
            }
        }

        [TestCaseSource(nameof(ZoneFiles))]
        public void Solve_ZoneAsset_MatchesRotationGoal(string path)
        {
            var zone = ZoneParser.Parse(File.ReadAllText(path));

            var result = PuzzleSolver.Solve(zone);

            Assert.That(result.CanClear, Is.True);
            Assert.That(result.MinimumRotations.Value, Is.InRange(zone.MinimumRotations, zone.MaximumRotations));
        }

        [Test]
        public void Solve_DocumentedExample_RequiresExactlyOneRotation()
        {
            var zone = ZoneParser.Parse(File.ReadAllText(Path.Combine(LevelsPath, "zone-02.txt")));

            var result = PuzzleSolver.Solve(zone);

            Assert.That(result.MinimumRotations, Is.EqualTo(1));
            Assert.That(result.MinimumActions, Is.Not.Null);
            Assert.That(result.ExploredStates, Is.GreaterThan(1));
            Assert.That(result.DeadEndRatio, Is.InRange(0d, 1d));
        }

        [Test]
        public void SolveWithFeathers_DocumentedExample_RequiresOneRotationForEachGoal()
        {
            var zone = ZoneParser.Parse(File.ReadAllText(Path.Combine(LevelsPath, "zone-02.txt")));

            var result = PuzzleSolver.SolveWithFeathers(zone);

            Assert.That(result.Clear.MinimumRotations, Is.EqualTo(1));
            Assert.That(result.MinimumRotationsWithAllFeathers, Is.EqualTo(1));
            Assert.That(result.Clear.MinimumActions, Is.EqualTo(7));
            Assert.That(result.Clear.ExploredStates, Is.EqualTo(31));
        }

        [Test]
        public void SolveWithFeathers_ZoneWithoutFeathers_CanClearWithEmptyMask()
        {
            var zone = Fixture(new[] { "#####" }, new[] { "S...M" });

            var result = PuzzleSolver.SolveWithFeathers(zone);

            Assert.That(result.Clear.MinimumRotations, Is.Zero);
            Assert.That(result.MinimumRotationsWithAllFeathers, Is.Zero);
        }

        [Test]
        public void SolveWithFeathers_FeatherRequiresOppositeView_ReportsMoreRotationsForCollection()
        {
            var zone = FixtureWithFeatherViews("2 1 0 = 180", new[] { "#####" }, new[] { "S.F.M" });

            var result = PuzzleSolver.SolveWithFeathers(zone);

            Assert.That(result.Clear.MinimumRotations, Is.Zero);
            Assert.That(result.MinimumRotationsWithAllFeathers, Is.EqualTo(2));
        }

        [Test]
        public void SolveWithFeathers_RotationReachesActiveFeather_CollectsBeforeWalkingToExit()
        {
            var zone = FixtureWithFeatherViews("4 1 0 = 90",
                new[] { "#####", "..#.." }, new[] { "S...F", "..M.." });

            var result = PuzzleSolver.SolveWithFeathers(zone);

            Assert.That(result.MinimumRotationsWithAllFeathers, Is.EqualTo(1));
        }

        [Test]
        public void SolveWithFeathers_MagpieBeforeFeather_EndsRouteBeforeFullCollection()
        {
            var zone = Fixture(new[] { "##.", "...", "...", "..#" }, new[] { "SM.", "...", "...", "..F" });

            var result = PuzzleSolver.SolveWithFeathers(zone);

            Assert.That(result.Clear.MinimumRotations, Is.Zero);
            Assert.That(result.MinimumRotationsWithAllFeathers, Is.Null);
        }

        [Test]
        public void Solve_ZoneAboveCollectionLimit_KeepsBasicClearAvailable()
        {
            var zone = Fixture(new[] { "########" }, new[] { "SFFFFFFM" });

            Assert.That(PuzzleSolver.Solve(zone).CanClear, Is.True);
            Assert.Throws<ArgumentException>(() => PuzzleSolver.SolveWithFeathers(zone));
        }

        [TestCase(1, 0, ViewDirection.Degrees0, 0)]
        [TestCase(1, 1, ViewDirection.Degrees90, 0)]
        [TestCase(1, 1, ViewDirection.Degrees0, 1)]
        [TestCase(2, 1, ViewDirection.Degrees180, 2)]
        public void Collect_FeetAndView_OnlyAddsTheMatchingActiveFeather(int x, int z, ViewDirection view, int expectedMask)
        {
            var zone = FixtureWithFeatherViews("1 1 1 = 0", new[] { "####", "####" }, new[] { "S..M", ".FF." });
            var collection = new FeatherCollection(zone);

            var mask = collection.Collect(0, new Vector3Int(x, 1, z), view);

            Assert.That(mask, Is.EqualTo(expectedMask));
        }

        [Test]
        public void Collect_RevisitedFeather_DoesNotChangeMask()
        {
            var zone = Fixture(new[] { "####" }, new[] { "SFFM" });
            var collection = new FeatherCollection(zone);
            var first = zone.Feathers[0].Position;
            var second = zone.Feathers[1].Position;

            var mask = collection.Collect(0, first, ViewDirection.Degrees0);
            Assert.That(collection.Collect(mask, first, ViewDirection.Degrees0), Is.EqualTo(mask));
            Assert.That(collection.Collect(mask, second, ViewDirection.Degrees0), Is.EqualTo(collection.FullMask));
        }

        [Test]
        public void Solve_FewerActionsWithRotation_MinimizesBothMetricsIndependently()
        {
            var zone = Fixture(new[] { "######" }, new[] { "S....M" });

            var result = PuzzleSolver.Solve(zone);

            Assert.That(result.CanClear, Is.True);
            Assert.That(result.MinimumRotations, Is.Zero);
            Assert.That(result.MinimumActions, Is.EqualTo(1));
        }

        [Test]
        public void Solve_SeparatedInBothAxes_ReturnsNoSolution()
        {
            var zone = Fixture(
                new[] { "#....", ".....", ".....", ".....", "....#" },
                new[] { "S....", ".....", ".....", ".....", "....M" });

            var result = PuzzleSolver.Solve(zone);

            Assert.That(result.CanClear, Is.False);
            Assert.That(result.MinimumRotations, Is.Null);
            Assert.That(result.MinimumActions, Is.Null);
            Assert.That(result.ExploredStates, Is.GreaterThan(1));
            Assert.That(result.DeadEndRatio, Is.EqualTo(1d));
        }

        [Test]
        public void Solve_FallOffZoneLeavesADeadEnd_DoesNotCountResetAsEscape()
        {
            var zone = Fixture(new[] { "##..", "...." }, new[] { "SM..", "...." });

            var result = PuzzleSolver.Solve(zone);

            Assert.That(result.CanClear, Is.True);
            Assert.That(result.DeadEndRatio, Is.GreaterThan(0d).And.LessThan(1d));
        }

        [Test]
        public void Solve_MagpieOnTheFallPath_EndsBeforeLanding()
        {
            var zone = Fixture(new[] { "..#" }, new[] { "..." }, new[] { ".#M" }, new[] { ".S." });

            var result = PuzzleSolver.Solve(zone);

            Assert.That(result.MinimumRotations, Is.Zero);
            Assert.That(result.MinimumActions, Is.EqualTo(2));
        }

        [TestCase(ViewDirection.Degrees0, 2, 0)]
        [TestCase(ViewDirection.Degrees90, 4, 2)]
        [TestCase(ViewDirection.Degrees180, 0, 2)]
        [TestCase(ViewDirection.Degrees270, 0, 0)]
        public void TryWalk_AllViews_MovesOneScreenCellAndUpdatesDepth(ViewDirection view, int x, int z)
        {
            var zone = Fixture(new[] { "#####", "#####", "#####" }, new[] { ".....", ".S..M", "....." });
            var map = new ProjectionMap(zone, view);

            Assert.That(PuzzleMovement.TryWalk(map, zone.Start, 1, out var next), Is.True);
            Assert.That(next, Is.EqualTo(new Vector3Int(x, 1, z)));
            Assert.That(map.Project(next), Is.EqualTo(map.Project(zone.Start) + Vector2Int.right));
        }

        [TestCase("#.#.", "S.M.", true)]
        [TestCase("#..#", "S..M", false)]
        public void TryJumpGap_OneOrTwoEmptyCells_OnlyCrossesOne(string floor, string markers, bool possible)
        {
            var zone = Fixture(new[] { floor }, new[] { markers });
            var map = new ProjectionMap(zone, ViewDirection.Degrees0);

            var jumped = PuzzleMovement.TryJumpGap(map, zone.Start, 1, out var next);

            Assert.That(jumped, Is.EqualTo(possible));
            Assert.That(next, Is.EqualTo(possible ? zone.Magpie : zone.Start));
        }

        [TestCase(".M.", true)]
        [TestCase(".#M", false)]
        [TestCase("#M.", false)]
        public void TryStepUp_HeightAndHeadroom_OnlyClimbsOneClearCell(string upper, bool possible)
        {
            var zone = Fixture(new[] { "###" }, new[] { "S#." }, new[] { upper });
            var map = new ProjectionMap(zone, ViewDirection.Degrees0);

            var stepped = PuzzleMovement.TryStepUp(map, zone.Start, 1, out var next);

            Assert.That(stepped, Is.EqualTo(possible));
            Assert.That(next, Is.EqualTo(possible ? zone.Magpie : zone.Start));
        }

        [Test]
        public void TryFall_AfterWalkingOffLedge_FallsToFirstSupport()
        {
            var zone = Fixture(new[] { "..#" }, new[] { "..M" }, new[] { ".#." }, new[] { ".S." });
            var map = new ProjectionMap(zone, ViewDirection.Degrees0);

            Assert.That(PuzzleMovement.TryWalk(map, zone.Start, 1, out var ledge), Is.True);
            Assert.That(ledge, Is.EqualTo(new Vector3Int(2, 3, 0)));
            Assert.That(PuzzleMovement.TryWalk(map, ledge, -1, out _), Is.False);
            Assert.That(PuzzleMovement.TryFall(map, ledge, out var next), Is.True);
            Assert.That(next, Is.EqualTo(zone.Magpie));
        }

        [Test]
        public void TryFall_WithoutSupport_ReachesResetThreshold()
        {
            var zone = Fixture(new[] { "#." }, new[] { "SM" });
            var map = new ProjectionMap(zone, ViewDirection.Degrees0);

            Assert.That(PuzzleMovement.TryFall(map, zone.Magpie, out var next), Is.True);
            Assert.That(next, Is.EqualTo(new Vector3Int(1, -4, 0)));
            Assert.That(PuzzleMovement.TryFall(map, next, out _), Is.False);
        }

        private static ZoneData Fixture(params string[][] layers) => FixtureWithFeatherViews(null, layers);

        private static ZoneData FixtureWithFeatherViews(string featherViews, params string[][] layers)
        {
            var zone = new StringBuilder();
            zone.AppendLine("name: 솔버 테스트");
            zone.AppendLine("teaches: 이동과 회전 확인");
            zone.AppendLine($"size: {layers[0][0].Length} {layers.Length} {layers[0].Length}");
            zone.AppendLine("rotations: 0-16");
            zone.AppendLine("start-view: 0");
            zone.AppendLine("intent: 목표에 이르는 경로 확인");
            if (featherViews != null)
            {
                zone.AppendLine($"feather-views: {featherViews}");
            }

            for (var y = 0; y < layers.Length; y++)
            {
                zone.AppendLine($"y={y}");
                for (var z = 0; z < layers[y].Length; z++)
                {
                    zone.AppendLine($"z{z}: {layers[y][z]}");
                }
            }

            return ZoneParser.Parse(zone.ToString());
        }
    }
}
