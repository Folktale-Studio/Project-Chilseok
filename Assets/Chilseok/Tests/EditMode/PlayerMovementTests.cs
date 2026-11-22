using Chilseok.Puzzle;
using NUnit.Framework;
using UnityEngine;

namespace Chilseok.Tests.EditMode
{
    public sealed class PlayerMovementTests
    {
        private static readonly PlayerMovement.Tuning Tuning = new(4f, 1.25f, 2f, 30f, 0.08f);

        [Test]
        public void Tick_AfterSteppingOff_StillJumpsDuringCoyoteTime()
        {
            var movement = StartAtEdge();
            movement.Tick(0.04f, 0f, false, Tuning);
            var fallingHeight = movement.Position.y;

            movement.Tick(0.02f, 0f, true, Tuning);

            Assert.That(movement.Position.y, Is.GreaterThan(fallingHeight));
        }

        [Test]
        public void Tick_ExpiredCoyoteTime_ContinuesFalling()
        {
            var movement = StartAtEdge();
            movement.Tick(0.09f, 0f, false, Tuning);
            var fallingHeight = movement.Position.y;

            movement.Tick(0.02f, 0f, true, Tuning);

            Assert.That(movement.Position.y, Is.LessThan(fallingHeight));
        }

        [Test]
        public void Tick_PastResetHeight_ReturnsToStart()
        {
            var movement = StartAtEdge();

            movement.Tick(1f, 0f, false, Tuning);

            Assert.That(movement.Feet, Is.EqualTo(new Vector3Int(1, 1, 0)));
            Assert.That(movement.Direction, Is.EqualTo(ViewDirection.Degrees0));
            Assert.That(movement.IsGrounded, Is.True);
        }

        [Test]
        public void Tick_WalkingOntoAnotherDepth_UpdatesFeetFromProjectedSupport()
        {
            var zone = ZoneParser.Parse(@"name: depth
teaches: depth
intent: walk across projected supports
size: 3 2 2
rotations: 0-0
start-view: 0
y=0
z0: #..
z1: .##
y=1
z0: S..
z1: ..M");
            var movement = new PlayerMovement(zone);

            movement.Tick(0.25f, 1f, false, Tuning);

            Assert.That(movement.Feet, Is.EqualTo(new Vector3Int(1, 1, 1)));
        }

        [Test]
        public void ApplyRotation_ClearView_UsesReprojectedFeetAndMap()
        {
            var movement = CreateRotationFixture();
            var turn = movement.PreviewRotation(ViewDirection.Degrees90);

            movement.ApplyRotation(turn, Tuning.Gravity);

            Assert.That(movement.Direction, Is.EqualTo(ViewDirection.Degrees90));
            Assert.That(movement.Feet, Is.EqualTo(new Vector3Int(4, 1, 1)));
            Assert.That(movement.IsGrounded, Is.True);
        }

        [Test]
        public void ApplyRotation_BlockedView_KeepsPreviousFeetAndMap()
        {
            var movement = CreateRotationFixture();
            movement.Tick(0.25f, -1f, false, Tuning);
            var before = movement.Position;
            var turn = movement.PreviewRotation(ViewDirection.Degrees90);

            movement.ApplyRotation(turn, Tuning.Gravity);

            Assert.That(turn.Outcome, Is.EqualTo(RotationOutcome.RejectedBlocked));
            Assert.That(movement.Direction, Is.EqualTo(ViewDirection.Degrees0));
            Assert.That(movement.Position, Is.EqualTo(before));
        }

        [Test]
        public void Reset_AfterTurn_RestoresStartViewAndFeet()
        {
            var movement = CreateRotationFixture();
            movement.ApplyRotation(movement.PreviewRotation(ViewDirection.Degrees90), Tuning.Gravity);
            movement.Tick(0.25f, -1f, false, Tuning);

            movement.Reset();

            Assert.That(movement.Direction, Is.EqualTo(ViewDirection.Degrees0));
            Assert.That(movement.Feet, Is.EqualTo(new Vector3Int(3, 1, 1)));
        }

        private static PlayerMovement CreateRotationFixture() => new(ZoneParser.Parse(@"name: rotation
teaches: rotation
intent: turn at start and reject near the wall
size: 5 2 2
rotations: 0-0
start-view: 0
y=0
z0: ###.#
z1: ..###
y=1
z0: ....#
z1: ...SM"));

        private static PlayerMovement StartAtEdge()
        {
            var zone = ZoneParser.Parse(@"name: coyote
teaches: coyote
intent: walk off the left edge
size: 3 2 1
rotations: 0-0
start-view: 0
y=0
z0: .##
y=1
z0: .SM");
            var movement = new PlayerMovement(zone);
            movement.Tick(0.25f, -1f, false, Tuning);
            Assert.That(movement.IsGrounded, Is.False);
            return movement;
        }
    }
}
