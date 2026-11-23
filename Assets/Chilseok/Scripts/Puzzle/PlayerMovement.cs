using UnityEngine;

namespace Chilseok.Puzzle
{
    public sealed class PlayerMovement
    {
        public const float BodyWidth = 0.6f;
        private const float WallSeparation = (PuzzleMovement.BlockWidth + BodyWidth) * 0.5f;

        public readonly struct Tuning
        {
            public readonly float MoveSpeed;
            public readonly float JumpHeight;
            public readonly float JumpDistance;
            public readonly float Gravity;
            public readonly float CoyoteSeconds;

            public Tuning(float moveSpeed, float jumpHeight, float jumpDistance, float gravity, float coyoteSeconds)
            {
                MoveSpeed = moveSpeed;
                JumpHeight = jumpHeight;
                JumpDistance = jumpDistance;
                Gravity = gravity;
                CoyoteSeconds = coyoteSeconds;
            }
        }

        private enum Motion { Grounded, Walking, Jumping, Falling }

        private readonly ProjectionMap[] _views;
        private readonly ViewDirection _startView;
        private readonly Vector3Int _start;
        private ProjectionMap _map;
        private Motion _motion;
        private Vector3Int _target;
        private Vector3Int _lastGroundedFeet;
        private Vector3 _origin;
        private Vector3 _landing;
        private float _progress;
        private float _airTime;
        private float _duration;
        private float _upSpeed;
        private float _gravity;
        private float _jumpDistance;
        private float _clearanceTime;
        private float _clearanceProgress;
        private bool _canUseCoyote;

        public Vector3Int Feet { get; private set; }
        public Vector3 Position { get; private set; }
        public ViewDirection Direction => _map.Direction;
        public bool IsGrounded => _motion is Motion.Grounded or Motion.Walking;
        internal bool IsAtRestOnSupport => _motion == Motion.Grounded && Position == (Vector3)Feet;
        internal Vector3Int Size { get; }
        internal ViewDirection StartView => _startView;

        public PlayerMovement(ZoneData zone)
        {
            Size = zone.Size;
            _views = new[]
            {
                new ProjectionMap(zone, ViewDirection.Degrees0),
                new ProjectionMap(zone, ViewDirection.Degrees90),
                new ProjectionMap(zone, ViewDirection.Degrees180),
                new ProjectionMap(zone, ViewDirection.Degrees270)
            };
            _startView = zone.StartView;
            _map = MapFor(_startView);
            Reprojector.TryUpdateDepth(_map, zone.Start, out _start);
            Reset();
        }

        public void Reset()
        {
            _map = MapFor(_startView);
            GroundAt(_start);
        }

        internal bool TryEnter(Vector3Int feet)
        {
            var entrance = MapFor(_startView);
            // 이웃 격자와 같은 발 칸을 공유할 때만 이동 중인 위치를 넘긴다.
            if (!ZoneData.IsInside(feet, Size) || entrance.IsSolid(entrance.Project(feet)) ||
                !Reprojector.TryUpdateDepth(entrance, feet, out var grounded) || grounded != feet)
            {
                return false;
            }

            _map = entrance;
            GroundAt(feet);
            return true;
        }

        public RotationResult PreviewRotation(ViewDirection target)
        {
            var feet = Feet;
            if (!IsGrounded)
                return new RotationResult(RotationOutcome.RejectedInAir, Direction, feet);

            // 칸 사이에서 돌 때는 현재 화면 위치에 더 가까운 발 칸을 기준으로 삼는다.
            if (HasCrossedCellBoundary() &&
                !Reprojector.TryUpdateDepth(_map, _target, out feet))
            {
                return new RotationResult(RotationOutcome.RejectedInAir, Direction, Feet);
            }

            return Reprojector.Rotate(_map, MapFor(target), feet);
        }

        public void ApplyRotation(RotationResult rotation, float gravity)
        {
            if (!rotation.Succeeded)
                return;

            _map = MapFor(rotation.Direction);
            if (rotation.Outcome == RotationOutcome.Grounded)
                GroundAt(rotation.Feet);
            else
                BeginFall(rotation.Feet, gravity, canUseCoyote: false);
        }

        private ProjectionMap MapFor(ViewDirection direction) => _views[(int)direction / 90];

        public void Tick(float dt, float horizontal, bool jumpPressed, Tuning tuning)
        {
            var direction = horizontal == 0f ? 0 : horizontal < 0f ? -1 : 1;

            if (jumpPressed && (IsGrounded || (_canUseCoyote && _airTime <= tuning.CoyoteSeconds)))
            {
                TryJump(direction, tuning);
            }

            switch (_motion)
            {
                case Motion.Grounded:
                    MoveOnGround(dt, horizontal * tuning.MoveSpeed, direction, tuning.Gravity);
                    break;
                case Motion.Walking:
                    Walk(dt, horizontal * tuning.MoveSpeed, tuning.Gravity);
                    break;
                case Motion.Jumping:
                    Jump(dt);
                    break;
                case Motion.Falling:
                    Fall(dt);
                    break;
            }
        }

        private void MoveOnGround(float dt, float speed, int direction, float gravity)
        {
            if (direction == 0)
                return;

            if (PuzzleMovement.TryWalk(_map, Feet, direction, out var adjacent))
            {
                _target = adjacent;
                _origin = Position;
                _landing = KeepDepth(adjacent, Feet);
                _progress = 0f;
                _motion = Motion.Walking;
                Walk(dt, speed, gravity);
                return;
            }

            // 벽 앞에서는 같은 발 칸 안에서 몸체와 벽면이 만나는 지점까지 걷는다.
            var offset = PuzzleMovement.IsWallAhead(_map, Feet, direction) ? direction * (1f - WallSeparation) : 0f;
            if ((offset - ScreenOffset(Feet)) * direction > 0f)
                Position = Vector3.MoveTowards(Position, (Vector3)Feet + ScreenRight * offset, Mathf.Abs(speed) * dt);
        }

        private void Walk(float dt, float speed, float gravity)
        {
            var direction = _map.Project(_target).x - _map.Project(Feet).x;
            var distance = Mathf.Abs(Vector3.Dot(_landing - _origin, ScreenRight));
            _progress = Mathf.Clamp01(_progress + speed * direction * dt / distance);
            Position = Vector3.Lerp(_origin, _landing, _progress);
            if (_progress == 0f)
            {
                _motion = Motion.Grounded;
            }
            else if (_progress == 1f)
            {
                if (Reprojector.TryUpdateDepth(_map, _target, out var grounded))
                    GroundAt(grounded);
                else
                    BeginFall(_target, gravity, canUseCoyote: true);
            }
        }

        private void TryJump(int direction, Tuning tuning)
        {
            var anchor = IsGrounded ? Feet : _lastGroundedFeet;
            var fallback = Feet;
            if (HasCrossedCellBoundary())
            {
                fallback = _target;
                if (Reprojector.TryUpdateDepth(_map, _target, out var support))
                    anchor = support;
            }

            if (_map.IsSolid(_map.Project(anchor) + Vector2Int.up))
                return;

            // 착지 후보는 솔버와 동일하다. 점프 수치는 포물선의 움직임을 조절한다.
            var takeoff = IsGrounded ? Feet : anchor;
            if ((IsGrounded && PuzzleMovement.TryStepUp(_map, anchor, direction, out var target)) ||
                PuzzleMovement.TryJumpGap(_map, anchor, direction, out target))
            {
                _target = target;
                _landing = KeepDepth(target, takeoff);
            }
            else
            {
                PickFlatLanding(anchor, takeoff, fallback, direction);
            }

            _origin = Position;
            _airTime = 0f;
            _gravity = tuning.Gravity;
            _jumpDistance = tuning.JumpDistance;
            _upSpeed = Mathf.Sqrt(2f * _gravity * tuning.JumpHeight);
            var heightChange = _landing.y - _origin.y;
            var landingSpeed = Mathf.Sqrt(_upSpeed * _upSpeed - 2f * _gravity * heightChange);
            _duration = (_upSpeed + landingSpeed) / _gravity;
            _clearanceTime = heightChange > 0f ? (_upSpeed - landingSpeed) / _gravity : 0f;
            var horizontalDistance = Mathf.Abs(Vector3.Dot(_landing - _origin, ScreenRight));
            _clearanceProgress = horizontalDistance > WallSeparation
                ? 1f - WallSeparation / horizontalDistance
                : 0f;
            _motion = Motion.Jumping;
            _canUseCoyote = false;
        }

        private void PickFlatLanding(Vector3Int anchor, Vector3Int takeoff, Vector3Int fallback, int direction)
        {
            _target = fallback;
            _landing = new Vector3(Position.x, fallback.y, Position.z);
            if (!IsGrounded || direction == 0)
                return;

            if (ScreenOffset(anchor) * direction < 0f)
            {
                _target = anchor;
                _landing = KeepDepth(anchor, takeoff);
            }
            else if (PuzzleMovement.TryWalk(_map, anchor, direction, out var adjacent) &&
                !_map.IsSolid(_map.Project(adjacent) + Vector2Int.up) &&
                Reprojector.TryUpdateDepth(_map, adjacent, out var supported))
            {
                _target = supported;
                _landing = KeepDepth(supported, takeoff);
            }
        }

        private void Jump(float dt)
        {
            _airTime += dt;
            if (_airTime >= _duration)
            {
                if (Reprojector.TryUpdateDepth(_map, _target, out var grounded))
                    GroundAt(grounded, Vector3.Dot(_landing - (Vector3)_target, ScreenRight));
                else
                    BeginFall(_target, _gravity, canUseCoyote: false);
                return;
            }

            var approach = JumpProgress();
            var y = _origin.y + _upSpeed * _airTime - _gravity * _airTime * _airTime * 0.5f;
            Position = Vector3.Lerp(_origin, _landing, approach);
            Position = new Vector3(Position.x, y, Position.z);
        }

        private float JumpProgress()
        {
            if (_clearanceTime <= 0f)
                return ShapeJump(_airTime / _duration);

            // 발판 높이에 도달하는 순간 벽 앞 경로와 착지 경로를 끊김 없이 잇는다.
            if (_airTime < _clearanceTime)
                return _clearanceProgress * ShapeJump(_airTime / _clearanceTime);

            var progress = (_airTime - _clearanceTime) / (_duration - _clearanceTime);
            return Mathf.Lerp(_clearanceProgress, 1f, ShapeJump(progress));
        }

        private float ShapeJump(float progress) =>
            progress + (_jumpDistance - 2f) * progress * (1f - progress);

        private void BeginFall(Vector3Int from, float gravity, bool canUseCoyote)
        {
            Feet = from;
            Position = from;
            _origin = from;
            _airTime = 0f;
            _gravity = gravity;
            _motion = Motion.Falling;
            _canUseCoyote = canUseCoyote;
            PuzzleMovement.TryFall(_map, from, out _target);
        }

        private void Fall(float dt)
        {
            _airTime += dt;
            var y = _origin.y - _gravity * _airTime * _airTime * 0.5f;
            if (y < PuzzleMovement.ResetHeight)
            {
                if (PuzzleMovement.HasFallenOut(_target))
                    Reset();
                else
                    GroundAt(_target);
            }
            else if (y <= _target.y)
            {
                GroundAt(_target);
            }
            else
            {
                Position = new Vector3(_origin.x, y, _origin.z);
            }
        }

        private void GroundAt(Vector3Int feet, float screenOffset = 0f)
        {
            Feet = feet;
            Position = (Vector3)feet + ScreenRight * screenOffset;
            _lastGroundedFeet = feet;
            _motion = Motion.Grounded;
            _canUseCoyote = false;
        }

        private bool HasCrossedCellBoundary() => _motion == Motion.Walking && Mathf.Abs(ScreenOffset(Feet)) >= 0.5f;

        private float ScreenOffset(Vector3Int feet) => Vector3.Dot(Position - (Vector3)feet, ScreenRight);

        private Vector3 ScreenRight => Direction switch
        {
            ViewDirection.Degrees0 => Vector3.right,
            ViewDirection.Degrees90 => Vector3.forward,
            ViewDirection.Degrees180 => Vector3.left,
            _ => Vector3.back
        };

        private Vector3 KeepDepth(Vector3Int destination, Vector3Int depth) => Direction switch
        {
            ViewDirection.Degrees0 or ViewDirection.Degrees180 =>
                new Vector3(destination.x, destination.y, depth.z),
            _ => new Vector3(depth.x, destination.y, destination.z)
        };
    }
}
