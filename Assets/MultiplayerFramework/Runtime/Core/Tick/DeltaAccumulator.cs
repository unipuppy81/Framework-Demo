using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerFramework.Runtime.Core.Tick
{
    public struct DeltaAccumulator
    {
        private float _value;
        public float Value => _value;

        /// <summary>
        /// 이번 프레임에 지난 시간 누적
        /// </summary>
        /// <param name="deltaTime"></param>
        public void Add(float deltaTime)
        {
            _value += deltaTime;
        }

        /// <summary>
        /// 지금 누적된 시간이 Tick 1회 돌릴 만큼 충분한지 확인
        /// </summary>
        /// <param name="tickInterval"></param>
        /// <returns></returns>
        public bool CanStep(float tickInterval)
        {
            return _value > tickInterval;
        }

        /// <summary>
        /// Tick 1번 실행했으니 그만큼 시간 차감
        /// </summary>
        /// <param name="tickInterval"></param>
        public void Consume(float tickInterval)
        {
            _value -= tickInterval;
        }

        /// <summary>
        /// accumulator 가 너무 많이 쌓이는 거 방지
        /// 
        /// 한 프레임이 더 무거워지고 또 느려지고, 다시 Tick이 더 밀리는 spiral of death 방지
        /// </summary>
        /// <param name="maxValue"></param>
        public void Clamp(float maxValue)
        {
            _value = Mathf.Min(_value, maxValue);
        }

        /// <summary>
        /// 현재 Tick 사이에서 얼마나 진행됐는지 비율 구함
        /// 
        /// 렌더 보간(interpolation)에 사용
        /// </summary>
        /// <param name="tickInterval"></param>
        /// <returns></returns>
        public float GetAlpha(float tickInterval)
        {
            if (tickInterval <= 0f)
                return 0f;

            return _value / tickInterval;
        }

        public void Reset()
        {
            _value = 0f;
        }
    }
}
