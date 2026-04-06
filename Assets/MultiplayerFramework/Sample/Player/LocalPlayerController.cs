using MultiplayerFramework.Runtime.Core.Tick;
using MultiplayerFramework.Runtime.Gameplay.Combat;
using MultiplayerFramework.Runtime.Gameplay.Input;
using MultiplayerFramework.Sample.Combat;
using MultiplayerFramework.Sample.Player;
using UnityEngine;


namespace MultiplayerFramework.Runtime.Sample.Player
{
    public sealed class LocalPlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private FixedTickScheduler scheduler;
        [SerializeField] private AttackResolver attackResolver;

        [Header("Network")]
        [SerializeField] private int playerId = 1;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float dashSpeed = 12f;
        [SerializeField] private float dashDuration = 0.15f;

        [Header("Attack")]
        [SerializeField] private float attackCooldown = 0.4f;
        [SerializeField] private float attackLockDuration = 0.12f;
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private float attackRadius = 0.6f;
        [SerializeField] private int attackDamage = 10;
        [SerializeField] private LayerMask attackTargetMask;

        private readonly InputBuffer _inputBuffer = new();

        private PlayerStateMachine _stateMachine;
        private PlayerInputFrame _latestFrameInput;
        public PlayerState CurrentState => _stateMachine.CurrentState;

        private void Awake()
        {
            _stateMachine = new PlayerStateMachine(
                transform.position,
                moveSpeed,
                dashSpeed,
                dashDuration,
                attackCooldown,
                attackLockDuration);
        }

        private void OnEnable()
        {
            if(scheduler != null)
            {
                scheduler.OnTick += HandleTick;
            }
        }

        private void OnDisable()
        {
            if (scheduler != null)
            {
                scheduler.OnTick -= HandleTick;
            }
        }

        private void Update()
        {
            Debug.LogError("A");

            _latestFrameInput = CollectFrameInput();

            if (scheduler == null)
                return;

            int nextTick = scheduler.CurrentTick + 1;

            PlayerInputCommand command = new PlayerInputCommand
            {
                Tick = nextTick,
                Move = _latestFrameInput.Move,
                JumpPressed = _latestFrameInput.JumpPressed,
                AttackPressed = _latestFrameInput.AttackPressed
            };

            _inputBuffer.Store(command);
        }

        private void HandleTick(TickContext context)
        {
            PlayerInputCommand command = _inputBuffer.GetOrDefault(context.Tick);
            PlayerTickResult result = _stateMachine.Tick(command, context.DeltaTime);

            if (result.TriggerAttack)
            {
                attackResolver?.TryResolveAttack(transform, playerId, context.Tick);
            }

            _inputBuffer.ClearBefore(context.Tick - 2);
        }

        private static PlayerInputFrame CollectFrameInput()
        {
            Debug.LogError("A");

            Vector2 move = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical"));

            if (move.sqrMagnitude > 1f)
                move.Normalize();

            return new PlayerInputFrame
            {
                Move = move,
                JumpPressed = Input.GetKeyDown(KeyCode.Space),
                AttackPressed = Input.GetKeyDown(KeyCode.Q)
            };
        }

        /*
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying || _stateMachine == null)
                return;

            PlayerState state = _stateMachine.CurrentState;

            Vector3 facing = state.Facing.sqrMagnitude > 0.0001f
                ? state.Facing.normalized
                : transform.forward;

            Vector3 attackCenter = state.Position + facing * attackRange;

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackCenter, attackRadius);
        }
        */
    }
}

