using Chilseok.Puzzle;
using NUnit.Framework;
using UnityEngine;

namespace Chilseok.Tests.EditMode
{
    public sealed class ReprojectorTests
    {
        private const string Example =
            "name: 재배치 테스트\n" +
            "teaches: 시점이 바뀌면 발밑의 가장 가까운 블록이 바뀐다\n" +
            "size: 5 3 3\n" +
            "rotations: 0-0\n" +
            "start-view: 0\n" +
            "intent: 발 위치 겹침과 받침을 확인한다\n" +
            "y=0\n" +
            "z0: .#.#.\n" +
            "z1: .....\n" +
            "z2: .#...\n" +
            "y=1\n" +
            "z0: .S...\n" +
            "z1: .....\n" +
            "z2: ..#..\n" +
            "y=2\n" +
            "z0: ....M\n" +
            "z1: .....\n" +
            "z2: .....";

        [TestCase(ViewDirection.Degrees0, 1, 2, 1, 0)]
        [TestCase(ViewDirection.Degrees90, 1, 0, 3, 0)]
        [TestCase(ViewDirection.Degrees180, 1, 0, 1, 2)]
        [TestCase(ViewDirection.Degrees270, 3, 0, 1, 0)]
        public void TryUpdateDepth_OnSupport_UsesClosestDepthAndKeepsScreenCell(
            ViewDirection direction, int x, int z, int expectedX, int expectedZ)
        {
            var map = new ProjectionMap(ZoneParser.Parse(Example), direction);
            var feet = new Vector3Int(x, 1, z);

            Assert.That(Reprojector.TryUpdateDepth(map, feet, out var resolvedFeet), Is.True);
            Assert.That(resolvedFeet, Is.EqualTo(new Vector3Int(expectedX, 1, expectedZ)));
            Assert.That(map.Project(resolvedFeet), Is.EqualTo(map.Project(feet)));
        }

        [Test]
        public void TryUpdateDepth_WithoutSupport_KeepsOriginalFeet()
        {
            var map = new ProjectionMap(ZoneParser.Parse(Example), ViewDirection.Degrees0);
            var feet = new Vector3Int(4, 1, 1);

            Assert.That(Reprojector.TryUpdateDepth(map, feet, out var resolvedFeet), Is.False);
            Assert.That(resolvedFeet, Is.EqualTo(feet));
        }

        [TestCase(ViewDirection.Degrees90, 3)]
        [TestCase(ViewDirection.Degrees270, 1)]
        public void Rotate_QuarterTurnWithSupport_RepositionsToTargetDepth(ViewDirection direction, int expectedX)
        {
            var zone = ZoneParser.Parse(Example);
            var current = new ProjectionMap(zone, ViewDirection.Degrees0);
            var target = new ProjectionMap(zone, direction);

            var result = Reprojector.Rotate(current, target, zone.Start);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Outcome, Is.EqualTo(RotationOutcome.Grounded));
            Assert.That(result.Direction, Is.EqualTo(direction));
            Assert.That(result.Feet, Is.EqualTo(new Vector3Int(expectedX, 1, 0)));
            Assert.That(target.Project(result.Feet), Is.EqualTo(target.Project(zone.Start)));
        }

        [Test]
        public void Rotate_BlockOverlapsProjectedFeet_PreservesOriginalState()
        {
            var zone = ZoneParser.Parse(Example);
            var current = new ProjectionMap(zone, ViewDirection.Degrees180);
            var target = new ProjectionMap(zone, ViewDirection.Degrees90);
            var feet = new Vector3Int(1, 1, 2);

            var result = Reprojector.Rotate(current, target, feet);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Outcome, Is.EqualTo(RotationOutcome.RejectedBlocked));
            Assert.That(result.Direction, Is.EqualTo(current.Direction));
            Assert.That(result.Feet, Is.EqualTo(feet));
        }

        [Test]
        public void Rotate_WithoutSupportAtTarget_StartsFallAtOriginalDepth()
        {
            var zone = ZoneParser.Parse(Example);
            var current = new ProjectionMap(zone, ViewDirection.Degrees0);
            var target = new ProjectionMap(zone, ViewDirection.Degrees90);
            // 현재 투영에서는 받침이 보이지만 새 시점에서는 이 깊이에 받침이 없다.
            var feet = new Vector3Int(1, 1, 1);

            var result = Reprojector.Rotate(current, target, feet);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Outcome, Is.EqualTo(RotationOutcome.Falling));
            Assert.That(result.Direction, Is.EqualTo(target.Direction));
            Assert.That(result.Feet, Is.EqualTo(feet));
        }

        [Test]
        public void Rotate_WhileAirborne_RejectsBeforeReachingTargetSupport()
        {
            var zone = ZoneParser.Parse(Example);
            var current = new ProjectionMap(zone, ViewDirection.Degrees0);
            var target = new ProjectionMap(zone, ViewDirection.Degrees90);
            var feet = new Vector3Int(4, 1, 0);

            var result = Reprojector.Rotate(current, target, feet);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Outcome, Is.EqualTo(RotationOutcome.RejectedInAir));
            Assert.That(result.Direction, Is.EqualTo(current.Direction));
            Assert.That(result.Feet, Is.EqualTo(feet));
        }

        [TestCase(ViewDirection.Degrees0)]
        [TestCase(ViewDirection.Degrees180)]
        public void Rotate_WhenTargetIsNotAdjacent_PreservesOriginalState(ViewDirection direction)
        {
            var zone = ZoneParser.Parse(Example);
            var current = new ProjectionMap(zone, ViewDirection.Degrees0);
            var target = new ProjectionMap(zone, direction);

            var result = Reprojector.Rotate(current, target, zone.Start);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Outcome, Is.EqualTo(RotationOutcome.RejectedDirection));
            Assert.That(result.Direction, Is.EqualTo(current.Direction));
            Assert.That(result.Feet, Is.EqualTo(zone.Start));
        }
    }
}
