using MultiplayerFramework.Runtime.Netcode.Messages;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MultiplayerFramework.Runtime.NetCode.StateSync
{
    public sealed class RemoteSnapshotBuffer
    {
        private List<PlayerStateSnapshot> _snapshots = new();
        public int Count => _snapshots.Count;

        public void AddSnapshot(PlayerStateSnapshot snapshot)
        {
            if(_snapshots.Count ==0)
            {
                _snapshots.Add(snapshot);
                return;
            }

            int lastIndex = _snapshots.Count - 1;
            PlayerStateSnapshot last = _snapshots[lastIndex];

            // 최신 tick과 같으면 덮어쓰기
            if (last.Tick == snapshot.Tick)
            {
                _snapshots[lastIndex] = snapshot;
                return;
            }

            // 더 최신 tick이면 append
            if (last.Tick < snapshot.Tick)
            {
                _snapshots.Add(snapshot);
                return;
            }

            // 중간 혹은 오래된 tick 처리
            for (int i = 0; i < _snapshots.Count; i++)
            {
                if (_snapshots[i].Tick == snapshot.Tick)
                {
                    _snapshots[i] = snapshot;
                    return;
                }

                if (_snapshots[i].Tick > snapshot.Tick)
                {
                    _snapshots.Insert(i, snapshot);
                    return;
                }
            }

            _snapshots.Add(snapshot);
        }

        public bool TryGetSnapshots(float renderTick, out PlayerStateSnapshot from, out PlayerStateSnapshot to, out float alpha)
        {
            from = default;
            to = default;
            alpha = 0f;

            if (_snapshots.Count == 0)
                return false;

            if (_snapshots.Count == 1)
            {
                from = _snapshots[0];
                to = _snapshots[0];
                alpha = 0f;
                return true;
            }

            // renderTick보다 작은/같은 가장 가까운 snapshot 찾기
            for (int i = 0; i < _snapshots.Count - 1; i++)
            {
                PlayerStateSnapshot a = _snapshots[i];
                PlayerStateSnapshot b = _snapshots[i + 1];

                if (renderTick >= a.Tick && renderTick <= b.Tick)
                {
                    from = a;
                    to = b;

                    int tickDelta = b.Tick - a.Tick;
                    if (tickDelta <= 0)
                    {
                        alpha = 0f;
                        return true;
                    }

                    alpha = Mathf.Clamp01((renderTick - a.Tick) / tickDelta);
                    return true;
                }
            }

            // renderTick이 가장 최신보다 크면 마지막 값 고정
            from = _snapshots[_snapshots.Count - 1];
            to = from;
            alpha = 0f;
            return true;
        }

        /// <summary>
        /// 오래된 snapshot 제거
        /// </summary>
        public void RemoveOlderThan(int minTick)
        {
            for (int i = _snapshots.Count - 1; i >= 0; i--)
            {
                if (_snapshots[i].Tick < minTick)
                    _snapshots.RemoveAt(i);
            }
        }
    }
}

