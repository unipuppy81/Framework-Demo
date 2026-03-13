using MultiplayerFramework.Runtime.Gameplay.Input;
using MultiplayerFramework.Sample.Player;
using UnityEngine;

namespace MultiplayerFramework.Runtime.Sample.Player
{
    public sealed class PlayerStateMachine
    {
        private readonly float _moveSpeed;
        private readonly float _dashSpeed;
        private readonly float _dashDuration;
        private readonly float _attackCooldown;
        private readonly float _attackLockDuration;

        private PlayerState _state;

        public PlayerStateMachine(
            Vector3 startPosition,
            float moveSpeed,
            float dashSpeed,
            float dashDuration,
            float attackCooldown,
            float attackLockDuration)
        {
            _moveSpeed = moveSpeed;
            _dashSpeed = dashSpeed;
            _dashDuration = dashDuration;
            _attackCooldown = attackCooldown;
            _attackLockDuration = attackLockDuration;

            _state = new PlayerState
            {
                Position = startPosition,
                Facing = Vector3.forward,
                MotionState = PlayerMotionState.Idle,
                DashRemainingTime = 0f,
                AttackCooldownRemaining = 0f,
                AttackLockRemaining = 0f
            };
        }

        public PlayerState CurrentState => _state;

        public PlayerTickResult Tick(PlayerInputCommand command, float deltaTime)
        {
            PlayerTickResult result = default;

            UpdateTimers(deltaTime);

            Vector3 moveDirection = ToWorldMoveDirection(command.Move);
            UpdateFacing(moveDirection);

            HandleDash(command, moveDirection);

            if (TryStartAttack(command))
            {
                result.TriggerAttack = true;
            }

            if (_state.DashRemainingTime > 0f)
            {
                SimulateDash(deltaTime);
                return result;
            }

            if (_state.AttackLockRemaining > 0f)
            {
                _state.MotionState = PlayerMotionState.Attack;
                return result;
            }

            SimulateMove(moveDirection, deltaTime);
            return result;
        }

        private void UpdateTimers(float deltaTime)
        {
            if (_state.DashRemainingTime > 0f)
            {
                _state.DashRemainingTime -= deltaTime;
                if (_state.DashRemainingTime < 0f)
                    _state.DashRemainingTime = 0f;
            }

            if (_state.AttackCooldownRemaining > 0f)
            {
                _state.AttackCooldownRemaining -= deltaTime;
                if (_state.AttackCooldownRemaining < 0f)
                    _state.AttackCooldownRemaining = 0f;
            }

            if (_state.AttackLockRemaining > 0f)
            {
                _state.AttackLockRemaining -= deltaTime;
                if (_state.AttackLockRemaining < 0f)
                    _state.AttackLockRemaining = 0f;
            }
        }

        private static Vector3 ToWorldMoveDirection(Vector2 moveInput)
        {
            Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y);

            if (direction.sqrMagnitude > 1f)
                direction.Normalize();

            return direction;
        }

        private void UpdateFacing(Vector3 moveDirection)
        {
            if (moveDirection.sqrMagnitude > 0.0001f)
            {
                _state.Facing = moveDirection.normalized;
            }
        }

        private void HandleDash(PlayerInputCommand command, Vector3 moveDirection)
        {
            if (!command.DashPressed)
                return;

            if (_state.DashRemainingTime > 0f)
                return;

            if (_state.AttackLockRemaining > 0f)
                return;

            if (moveDirection.sqrMagnitude > 0.0001f)
            {
                _state.Facing = moveDirection.normalized;
            }

            _state.DashRemainingTime = _dashDuration;
            _state.MotionState = PlayerMotionState.Dash;
        }

        private bool TryStartAttack(PlayerInputCommand command)
        {
            if (!command.AttackPressed)
                return false;

            if (_state.DashRemainingTime > 0f)
                return false;

            if (_state.AttackLockRemaining > 0f)
                return false;

            if (_state.AttackCooldownRemaining > 0f)
                return false;

            _state.AttackCooldownRemaining = _attackCooldown;
            _state.AttackLockRemaining = _attackLockDuration;
            _state.MotionState = PlayerMotionState.Attack;

            return true;
        }

        private void SimulateDash(float deltaTime)
        {
            Vector3 dashDirection = _state.Facing.sqrMagnitude > 0.0001f
                ? _state.Facing.normalized
                : Vector3.forward;

            _state.Position += dashDirection * (_dashSpeed * deltaTime);
            _state.MotionState = PlayerMotionState.Dash;
        }

        private void SimulateMove(Vector3 moveDirection, float deltaTime)
        {
            if (moveDirection.sqrMagnitude <= 0.0001f)
            {
                _state.MotionState = PlayerMotionState.Idle;
                return;
            }

            _state.Position += moveDirection * (_moveSpeed * deltaTime);
            _state.MotionState = PlayerMotionState.Move;
        }
    }
}