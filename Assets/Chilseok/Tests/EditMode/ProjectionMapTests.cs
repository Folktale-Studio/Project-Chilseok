using System;
using Chilseok.Puzzle;
using NUnit.Framework;
using UnityEngine;

namespace Chilseok.Tests.EditMode
{
    public sealed class ProjectionMapTests
    {
        private const string Example =
            "name: 투영 테스트\n" +
            "teaches: 시점에 따라 앞쪽 블록이 바뀐다\n" +
            "size: 5 3 3\n" +
            "rotations: 0-0\n" +
            "start-view: 0\n" +
            "intent: 겹친 블록의 투영을 확인한다\n" +
            "y=0\n" +
            "z0: .#.#.\n" +
            "z1: .....\n" +
            "z2: .#...\n" +
            "y=1\n" +
            "z0: .S...\n" +
            "z1: ....F\n" +
            "z2: .....\n" +
            "y=2\n" +
            "z0: ....M\n" +
            "z1: .....\n" +
            "z2: .....";

        [TestCase(ViewDirection.Degrees0, 5, 3)]
        [TestCase(ViewDirection.Degrees90, 3, 2)]
        [TestCase(ViewDirection.Degrees180, 5, 1)]
        [TestCase(ViewDirection.Degrees270, 3, 0)]
        public void Project_RectangularZone_UsesViewWidthAndKeepsHeight(
            ViewDirection direction, int expectedWidth, int expectedX)
        {
            var map = new ProjectionMap(ZoneParser.Parse(Example), direction);

            var cell = map.Project(new Vector3Int(3, 1, 2));

            Assert.That(map.Width, Is.EqualTo(expectedWidth));
            Assert.That(map.Height, Is.EqualTo(3));
            Assert.That(cell, Is.EqualTo(new Vector2Int(expectedX, 1)));
        }

        [TestCase(ViewDirection.Degrees0, 1, 1, 0)]
        [TestCase(ViewDirection.Degrees90, 0, 3, 0)]
        [TestCase(ViewDirection.Degrees180, 3, 1, 2)]
        [TestCase(ViewDirection.Degrees270, 2, 1, 0)]
        public void TryGetNearestBlock_OverlappingBlocks_ReturnsClosestSupport(
            ViewDirection direction, int screenX, int blockX, int blockZ)
        {
            var map = new ProjectionMap(ZoneParser.Parse(Example), direction);
            var feet = new Vector2Int(screenX, 1);
            var support = feet + Vector2Int.down;

            Assert.That(map.TryGetNearestBlock(support, out var block), Is.True);
            Assert.That(block, Is.EqualTo(new Vector3Int(blockX, 0, blockZ)));
            Assert.That(map.IsSolid(support), Is.True);
            Assert.That(map.IsSolid(feet), Is.False);
        }

        [TestCase(ViewDirection.Degrees0)]
        [TestCase(ViewDirection.Degrees90)]
        [TestCase(ViewDirection.Degrees180)]
        [TestCase(ViewDirection.Degrees270)]
        public void IsSolid_FeatherAndOpenSpace_ReturnsFalse(ViewDirection direction)
        {
            var zone = ZoneParser.Parse(Example);
            var map = new ProjectionMap(zone, direction);
            var featherCell = map.Project(zone.Feathers[0].Position);

            Assert.That(map.IsSolid(featherCell), Is.False);
            Assert.That(map.TryGetNearestBlock(featherCell, out _), Is.False);
        }

        [TestCase(-1, 0)]
        [TestCase(5, 0)]
        [TestCase(0, -1)]
        [TestCase(0, 3)]
        public void IsSolid_CellOutsideProjectedGrid_ReturnsFalse(int x, int y)
        {
            var map = new ProjectionMap(ZoneParser.Parse(Example), ViewDirection.Degrees0);
            var cell = new Vector2Int(x, y);

            Assert.That(map.IsSolid(cell), Is.False);
            Assert.That(map.TryGetNearestBlock(cell, out _), Is.False);
        }

        [Test]
        public void Constructor_UnsupportedView_RejectsDirection()
        {
            var zone = ZoneParser.Parse(Example);

            Assert.Throws<ArgumentOutOfRangeException>(() => new ProjectionMap(zone, (ViewDirection)45));
        }
    }
}
