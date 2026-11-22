using UnityEngine;

namespace Chilseok.Puzzle
{
    public sealed class PlayerMovement
    {
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
        private bool _canUseCoyote;

        public Vector3Int Feet { get; private set; }
        public Vector3 Position { get; private set; }
        public ViewDirection Direction => _map.Direction;
        public bool IsGrounded => _motion is Motion.Grounded or Motion.Walking;

        public PlayerMovement(ZoneData zone)
        {
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

        public RotationResult PreviewRotation(ViewDirection target)
        {
            var feet = Feet;
            if (!IsGrounded)
                return new RotationResult(RotationOutcome.RejectedInAir, Direction, feet);

            // 칸 사이에서 돌 때는 현재 화면 위치에 더 가까운 발 칸을 기준으로 삼는다.
            if (_motion == Motion.Walking && _progress >= 0.5f &&
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
                    if (PuzzleMovement.TryWalk(_map, Feet, direction, out var adjacent))
                    {
                        _target = adjacent;
                        _origin = Position;
                        _landing = KeepDepth(adjacent, Feet);
                        _progress = 0f;
                        _motion = Motion.Walking;
                        Walk(dt, horizontal * tuning.MoveSpeed, tuning.Gravity);
                    }
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

        private void Walk(float dt, float speed, float gravity)
        {
            var direction = _map.Project(_target).x - _map.Project(Feet).x;
            _progress = Mathf.Clamp01(_progress + speed * direction * dt);
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
            if (_map.IsSolid(_map.Project(anchor) + Vector2Int.up))
                return;

            // 착지 후보는 솔버와 동일하다. 점프 수치는 포물선의 움직임을 조절한다.
            if ((!IsGrounded || !PuzzleMovement.TryStepUp(_map, anchor, direction, out _target)) &&
                !PuzzleMovement.TryJumpGap(_map, anchor, direction, out _target))
            {
                _target = Feet;
            }

            var takeoff = IsGrounded ? Feet : anchor;
            _origin = Position;
            _landing = KeepDepth(_target, takeoff);
            _airTime = 0f;
            _gravity = tuning.Gravity;
            _jumpDistance = tuning.JumpDistance;
            _upSpeed = Mathf.Sqrt(2f * _gravity * tuning.JumpHeight);
            var heightChange = _landing.y - _origin.y;
            _duration = (_upSpeed + Mathf.Sqrt(_upSpeed * _upSpeed - 2f * _gravity * heightChange)) / _gravity;
            _motion = Motion.Jumping;
            _canUseCoyote = false;
        }

        private void Jump(float dt)
        {
            _airTime += dt;
            if (_airTime >= _duration)
            {
                if (Reprojector.TryUpdateDepth(_map, _target, out var grounded))
                    GroundAt(grounded);
                else
                    BeginFall(_target, _gravity, canUseCoyote: false);
                return;
            }

            var t = _airTime / _duration;
            var approach = t + (_jumpDistance - 2f) * t * (1f - t);
            var y = _origin.y + _upSpeed * _airTime - _gravity * _airTime * _airTime * 0.5f;
            if (_origin.y < _landing.y && y < _landing.y)
            {
                // 상승 중에는 몸이 발판 높이에 닿기 전까지 발판 옆면을 통과하지 않는다.
                var distance = new Vector2(_landing.x - _origin.x, _landing.z - _origin.z).magnitude;
                if (distance > 0.5f)
                    approach = Mathf.Min(approach, 1f - 0.5f / distance);
                else
                    approach = 0f;
            }
            Position = Vector3.Lerp(_origin, _landing, approach);
            Position = new Vector3(Position.x, y, Position.z);
        }

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

        private void GroundAt(Vector3Int feet)
        {
            Feet = feet;
            Position = feet;
            _lastGroundedFeet = feet;
            _motion = Motion.Grounded;
            _canUseCoyote = false;
        }

        private Vector3 KeepDepth(Vector3Int destination, Vector3Int depth) => Direction switch
        {
            ViewDirection.Degrees0 or ViewDirection.Degrees180 =>
                new Vector3(destination.x, destination.y, depth.z),
            _ => new Vector3(depth.x, destination.y, destination.z)
        };
    }
}
