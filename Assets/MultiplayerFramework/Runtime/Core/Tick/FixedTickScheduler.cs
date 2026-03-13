using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerFramework.Runtime.Core.Tick
{
    public sealed class FixedTickScheduler : MonoBehaviour
    {
        [SerializeField] private int tickRate = 30;
        [SerializeField] private int maxTicksPerFrame = 4;

        private float _tickInterval;
        private int _currentTick;
        private float _elapsedTime;
        private DeltaAccumulator _accmulator;

        public int TickRate => tickRate;
        public float TickInterval => _tickInterval;
        public int CurrentTick => _currentTick;
        public float Alpha => _accmulator.GetAlpha(_tickInterval);
        public event Action<float> OnFrameUpdated;  // 프레임 기반 처리용 이벤트 : UI, 디버그 표시, 카메라, 시각 처리 등에 이용
        public event Action<TickContext> OnTick;    // 시뮬레이션 기반 처리용 이벤트 : 실제 게임 로직 여기에 연결

        private void Awake()
        {
            RebuildTickInterval();
        }

        /// <summary>
        /// Tick 속도 바꾸고 interval 다시 계산
        /// </summary>
        /// <param name="newTickRate"></param>
        public void SetTickRate(int newTickRate)
        {
            _tickInterval = Mathf.Max(1, newTickRate);
            _tickInterval = 1f / tickRate;
        }

        /// <summary>
        /// TickRate 값을 바탕으로 실제 Tick 간격 다시 계산
        /// 
        /// 예:
        /// tickRate = 30 -> _tickInterval = 1f / 30f;
        /// tickRate = 60 -> _tickInterval = 1f / 60f;
        /// 
        /// 설정값을 실제 시간 간격으로 반환
        /// </summary>
        private void RebuildTickInterval()
        {
            tickRate = Mathf.Max(1, tickRate);
            _tickInterval = 1f / tickRate;
        }

        private void Update()
        {
            float frameDelta = Time.deltaTime;
            _accmulator.Add(frameDelta);

            OnFrameUpdated?.Invoke(frameDelta);

            int processedTicks = 0;

            while(_accmulator.CanStep(_tickInterval) && processedTicks < maxTicksPerFrame)
            {
                _currentTick++;
                _elapsedTime += _tickInterval;

                TickContext context = new TickContext(_currentTick, _tickInterval, _elapsedTime);
                OnTick?.Invoke(context);

                _accmulator.Consume(_tickInterval);
                processedTicks++;
            }

            if(processedTicks == maxTicksPerFrame && _accmulator.CanStep(_tickInterval))
            {
                _accmulator.Clamp(_tickInterval);
            }
        }
    }

}
