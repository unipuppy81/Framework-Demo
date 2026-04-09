using MultiplayerFramework.Runtime.NetCode.Objects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MultiplayerFramework.Runtime.AOI
{
    /// <summary>
    /// 거리 기반 AOI
    /// </summary>
    public sealed class AOISystem
    {
        private float _enterRadius;
        private float _exitRadius;

        // 제곱값 캐시
        private float _enterRadiusSqr;
        private float _exitRadiusSqr;

        private Dictionary<int, HashSet<NetworkId>> _visibleByConnection = new();

        public AOISystem(float enterRadius, float exitRadius)
        {
            _enterRadius = enterRadius;
            _exitRadius = exitRadius;

            _enterRadiusSqr = enterRadius * enterRadius;
            _exitRadius = enterRadius * exitRadius;
        }


        public void UpdateObserver(
            int connectionId,
            int observerNetworkId,
            Vector3 observerPosition,
            NetworkObjectRegistry registry,
            List<NetworkId> entered,
            List<NetworkId> exited)
        {
            entered.Clear();
            exited.Clear();

            if (_visibleByConnection.TryGetValue(connectionId, out HashSet<NetworkId> currentVisible) == false)
            {
                currentVisible = new HashSet<NetworkId>();
                _visibleByConnection.Add(connectionId, currentVisible);
            }

            HashSet<NetworkId> nextVisible = new HashSet<NetworkId>();


            foreach (NetworkObject targetObject in registry.GetAll())
            {
                if (targetObject == null)
                    continue;

                NetworkId targetId = targetObject.NetworkId;

                if (targetId.Equals(observerNetworkId))
                    continue;

                Vector3 delta = targetObject.transform.position - observerPosition;
                delta.y = 0f;

                float distanceSqr = delta.sqrMagnitude;
                bool wasVisible = currentVisible.Contains(targetId);

                if (wasVisible)
                {
                    if (distanceSqr <= _exitRadiusSqr)
                    {
                        nextVisible.Add(targetId);
                    }
                }
                else
                {
                    if (distanceSqr <= _enterRadiusSqr)
                    {
                        nextVisible.Add(targetId);
                    }
                }
            }

            // Enter 계산
            foreach (NetworkId id in nextVisible)
            {
                if (currentVisible.Contains(id) == false)
                {
                    entered.Add(id);
                }
            }

            // Exit 계산
            foreach (NetworkId id in currentVisible)
            {
                if (nextVisible.Contains(id) == false)
                {
                    exited.Add(id);
                }
            }

            // visible set 갱신
            currentVisible.Clear();

            foreach (NetworkId id in nextVisible)
            {
                currentVisible.Add(id);
            }
        }



        /// <summary>
        /// connectionId 에게 보이는 대상인가
        /// </summary>
        public bool IsVisible(int connectionId, NetworkId targetId)
        {
            if (_visibleByConnection.TryGetValue(connectionId, out HashSet<NetworkId> visibleSet) == false)
                return false;

            return visibleSet.Contains(targetId);
        }

        public int GetVisibleCount(int connectionId)
        {
            if (_visibleByConnection.TryGetValue(connectionId, out HashSet<NetworkId> visibleSet) == false)
                return 0;

            return visibleSet.Count;
        }

        public void RemoveObserver(int connectionId)
        {
            _visibleByConnection.Remove(connectionId);
        }
    }
}
