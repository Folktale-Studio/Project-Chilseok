using Chilseok.Puzzle;
using NUnit.Framework;
using UnityEngine;

namespace Chilseok.Tests.EditMode
{
    public sealed class ZoneParserTests
    {
        private const string Example =
            "# zone-02\n" +
            "name: 둘째 구간\n" +
            "teaches: 한 번 돌면 떨어진 별이 붙는다\n" +
            "size: 8 3 4\n" +
            "rotations: 1-1\n" +
            "start-view: 90\n" +
            "intent: 시작 발판에서 90° 시점으로는 두 칸 틈이라 못 건넌다 → Q로 0° 시점 → 먼 다리가 붙어 보여 걸어 건넌다 → 한 칸 올라 까치\n" +
            "feather-views: 4 1 3 = 0\n" +
            "\n" +
            "y=0\n" +
            "z0: ###.....\n" +
            "z1: ........\n" +
            "z2: ........\n" +
            "z3: ...#####\n" +
            "y=1\n" +
            "z0: .SF.....\n" +
            "z1: ........\n" +
            "z2: ........\n" +
            "z3: ....FF##\n" +
            "y=2\n" +
            "z0: ........\n" +
            "z1: ........\n" +
            "z2: ........\n" +
            "z3: .......M";

        [Test]
        public void Parse_DesignExample_ReadsHeadersAndGrid()
        {
            var zone = ZoneParser.Parse(Example);

            Assert.That(zone.Name, Is.EqualTo("둘째 구간"));
            Assert.That(zone.Teaches, Is.EqualTo("한 번 돌면 떨어진 별이 붙는다"));
            Assert.That(zone.Intent, Does.Contain("Q로 0° 시점"));
            Assert.That(zone.Size, Is.EqualTo(new Vector3Int(8, 3, 4)));
            Assert.That(zone.MinimumRotations, Is.EqualTo(1));
            Assert.That(zone.MaximumRotations, Is.EqualTo(1));
            Assert.That(zone.StartView, Is.EqualTo(ViewDirection.Degrees90));
            Assert.That(zone.Start, Is.EqualTo(new Vector3Int(1, 1, 0)));
            Assert.That(zone.Magpie, Is.EqualTo(new Vector3Int(7, 2, 3)));
            Assert.That(zone.IsSolid(new Vector3Int(1, 0, 0)), Is.True);
            Assert.That(zone.IsSolid(new Vector3Int(3, 0, 3)), Is.True);
            Assert.That(zone.IsSolid(new Vector3Int(3, 0, 0)), Is.False);
            Assert.That(zone.IsSolid(new Vector3Int(-1, 0, 0)), Is.False);
            Assert.That(zone.IsSolid(zone.Start), Is.False);
            Assert.That(zone.IsSolid(zone.Magpie), Is.False);
        }

        [Test]
        public void Parse_FeathersWithAndWithoutViewRules_KeepsThemOutOfCollision()
        {
            var zone = ZoneParser.Parse(Example);

            Assert.That(zone.Feathers, Has.Count.EqualTo(3));
            Assert.That(zone.Feathers[0].Position, Is.EqualTo(new Vector3Int(2, 1, 0)));
            Assert.That(zone.Feathers[1].Position, Is.EqualTo(new Vector3Int(4, 1, 3)));
            Assert.That(zone.Feathers[2].Position, Is.EqualTo(new Vector3Int(5, 1, 3)));
            foreach (var feather in zone.Feathers)
            {
                Assert.That(zone.IsSolid(feather.Position), Is.False);
            }
        }

        [TestCase(ViewDirection.Degrees0, true)]
        [TestCase(ViewDirection.Degrees90, false)]
        [TestCase(ViewDirection.Degrees180, false)]
        [TestCase(ViewDirection.Degrees270, false)]
        public void Parse_FeatherViewRule_OnlyActivatesListedViews(ViewDirection view, bool active)
        {
            var zone = ZoneParser.Parse(Example);

            Assert.That(zone.Feathers[0].IsActive(view), Is.True);
            Assert.That(zone.Feathers[1].IsActive(view), Is.EqualTo(active));
            Assert.That(zone.Feathers[2].IsActive(view), Is.True);
        }

        [Test]
        public void Parse_TwoFeatherRules_AppliesEachRuleToItsOwnPosition()
        {
            var text = Example.Replace("feather-views: 4 1 3 = 0",
                "feather-views: 4 1 3 = 0\nfeather-views: 2 1 0 = 90, 180");

            var zone = ZoneParser.Parse(text);

            Assert.That(zone.Feathers[0].IsActive(ViewDirection.Degrees0), Is.False);
            Assert.That(zone.Feathers[0].IsActive(ViewDirection.Degrees90), Is.True);
            Assert.That(zone.Feathers[0].IsActive(ViewDirection.Degrees180), Is.True);
            Assert.That(zone.Feathers[0].IsActive(ViewDirection.Degrees270), Is.False);
            Assert.That(zone.Feathers[1].IsActive(ViewDirection.Degrees0), Is.True);
        }

        [Test]
        public void Parse_WindowsLineEndingsAndZeroRotations_ReadsLessonRange()
        {
            var text = "\uFEFF" + Example.Replace("rotations: 1-1", "rotations: 0-0").Replace("\n", "\r\n");

            var zone = ZoneParser.Parse(text);

            Assert.That(zone.MinimumRotations, Is.Zero);
            Assert.That(zone.MaximumRotations, Is.Zero);
        }

        [TestCase("size: 8 3 4", "size: 9 3 4", 4, "격자 크기")]
        [TestCase("size: 8 3 4", "size: 8 0 4", 4, "격자 크기")]
        [TestCase("rotations: 1-1", "rotations: 2-1", 5, "rotations")]
        [TestCase("start-view: 90", "start-view: 45", 6, "시점")]
        [TestCase("start-view: 90", "start-view: 90\nstart-view: 0", 7, "중복")]
        [TestCase("start-view: 90\n", "", 9, "start-view")]
        [TestCase("y=1", "y=2", 15, "y=1")]
        [TestCase("y=1\nz0: .SF.....", "y=1\nz1: .SF.....", 16, "z0")]
        [TestCase("z0: .SF.....", "z0: .SF....", 16, "행 길이")]
        [TestCase("z0: .SF.....", "z0: .SFP....", 16, "'P'")]
        [TestCase("z0: ###.....", "z0: #.#.....", 16, "바로 아래")]
        [TestCase("z0: .SF.....", "z0: SSF.....", 16, "S 표시가 중복")]
        [TestCase("z3: .......M", "z3: ......MM", 24, "M 표시가 중복")]
        [TestCase("z3: .......M", "z3: ........", 25, "까치 M")]
        [TestCase("\nz3: .......M", "", 24, "격자가 끝났습니다")]
        [TestCase("z3: .......M", "z3: .......M\ny=3", 25, "격자 크기를 넘는")]
        [TestCase("4 1 3 = 0", "4 1 = 0", 8, "feather-views")]
        [TestCase("4 1 3 = 0", "-1 1 3 = 0", 8, "격자 밖")]
        [TestCase("4 1 3 = 0", "3 1 3 = 0", 8, "F가 없습니다")]
        [TestCase("4 1 3 = 0", "4 1 3 = 360", 8, "시점")]
        [TestCase("4 1 3 = 0", "4 1 3 = 0,", 8, "시점")]
        [TestCase("4 1 3 = 0", "4 1 3 = 0,0", 8, "시점이 중복")]
        [TestCase("4 1 3 = 0", "4 1 3 = 0\nfeather-views: 4 1 3 = 90", 9, "좌표가 중복")]
        public void Parse_InvalidZone_ReportsOriginalLine(string original, string replacement, int line, string reason)
        {
            var text = Example.Replace(original, replacement);

            var error = Assert.Throws<ZoneParseException>(() => ZoneParser.Parse(text));

            Assert.That(error.LineNumber, Is.EqualTo(line));
            Assert.That(error.Message, Does.StartWith($"{line}줄:").And.Contain(reason));
        }
    }
}
