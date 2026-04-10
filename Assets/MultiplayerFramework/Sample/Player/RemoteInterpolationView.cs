using MultiplayerFramework.Runtime.Core.Tick;
using MultiplayerFramework.Runtime.Netcode.Messages;
using MultiplayerFramework.Runtime.Netcode.StateSync;
using MultiplayerFramework.Runtime.NetCode.StateSync;
using UnityEngine;

namespace MultiplayerFramework.Runtime.Sample.Player
{
    /// <summary>
    /// 원격 엔티티를 snapshot 기반으로 보간 렌더링하는 View
    /// 시뮬레이션 상태를 바꾸지 않고, 화면 표시만 부드럽게 설정
    /// </summary>
    public sealed class RemoteInterpolationView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private FixedTickScheduler _clientTickScheduler;

        [Header("Interpolation")]
        [SerializeField] private float _interpolationBackTimeInTicks = 2f;
        [SerializeField] private bool _useInterpolation = true;

        private readonly RemoteSnapshotBuffer _snapshotBuffer = new();

        // Diagnostics 용
        private float _lastRenderTick;
        private int _lastFromTick;
        private int _lastToTick;
        private float _lastAlpha;
        private float _logTimer;

        public float LastRenderTick => _lastRenderTick;
        public int LastFromTick => _lastFromTick;
        public int LastToTick => _lastToTick;
        public float LastAlpha => _lastAlpha;
        public int SnapshotCount => _snapshotBuffer.Count;

        /// <summary>
        /// Client가 host state를 수신했을 때 호출
        /// </summary>
        public void PushSnapshot(PlayerStateSnapshot snapshot)
        {
            _snapshotBuffer.AddSnapshot(snapshot);
        }

        private void Update()
        {
            if (_clientTickScheduler == null)
                return;

            if (_useInterpolation == false)
                return;

            float renderTick = _clientTickScheduler.CurrentTick - _interpolationBackTimeInTicks;
            _lastRenderTick = renderTick;

            if (_snapshotBuffer.TryGetSnapshots(renderTick, out PlayerStateSnapshot from, out PlayerStateSnapshot to, out float alpha) == false)
                return;

            _lastFromTick = from.Tick;
            _lastToTick = to.Tick;
            _lastAlpha = alpha;

            Vector3 interpolatedPosition = Vector3.Lerp(from.Position, to.Position, alpha);
            Quaternion interpolatedRotation = Quaternion.Slerp(from.Rotation, to.Rotation, alpha);

            transform.position = interpolatedPosition;
            transform.rotation = interpolatedRotation;

            _logTimer += Time.deltaTime;
            if (_logTimer >= 0.2f)
            {
                _logTimer = 0f;

                Debug.LogError(
                    $"<color=cyan>[Interpolation]</color> " +
                    $"\ncurrentTick={_clientTickScheduler.CurrentTick} " +
                    $"\nrenderTick={renderTick:F2} " +
                    $"\nfromTick={from.Tick} " +
                    $"\ntoTick={to.Tick} " +
                    $"\nalpha={alpha:F2} " +
                    $"\nfromPos={from.Position} " +
                    $"\ntoPos={to.Position} " +
                    $"\nresultPos={interpolatedPosition}"
                );
            }

            _snapshotBuffer.RemoveOlderThan(from.Tick - 5);
        }
    }
}