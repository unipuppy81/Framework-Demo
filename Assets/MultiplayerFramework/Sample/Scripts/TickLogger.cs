using MultiplayerFramework.Runtime.Core.Tick;
using UnityEngine;


namespace MultiplayerFramework.Sample
{
    public sealed class TickLogger : MonoBehaviour
    {
        [SerializeField] private FixedTickScheduler scheduler;

        private void OnEnable()
        {
            if (scheduler == null)
                return;

            scheduler.OnTick += HandleTick;
            scheduler.OnFrameUpdated -= HandleFrameUpdated;
        }

        private void OnDisable()
        {
            if (scheduler == null)
                return;

            scheduler.OnTick -= HandleTick;
            scheduler.OnFrameUpdated += HandleFrameUpdated;
        }

        private void HandleFrameUpdated(float frameDelta)
        {

        }

        private void HandleTick(TickContext context)
        {
            Debug.Log($"Tick={context.Tick}, TickDt={context.DeltaTime:F4}, Elapsed={context.ElapsedTime:F2}");
        }
    }



}
