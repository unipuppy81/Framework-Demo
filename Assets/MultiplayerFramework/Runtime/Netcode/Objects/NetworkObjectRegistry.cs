using MultiplayerFramework.Runtime.NetCode.Objects;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MultiplayerFramework.Runtime.NetCode.Objects
{
    public class NetworkObjectRegistry
    {
        private Dictionary<NetworkId, NetworkObject> _objects = new();

        public IEnumerable<NetworkObject> GetAll()
        {
            return _objects.Values;
        }

        public bool Register(NetworkObject networkObject)
        {
            if (networkObject == null)
            {
                Debug.LogError("[NetworkObjectRegistry] Register failed: object is null.");
                return false;
            }

            if (_objects.ContainsKey(networkObject.NetworkId))
            {
                Debug.LogError($"[NetworkObjectRegistry] Duplicate NetworkId: {networkObject.NetworkId}");
                return false;
            }

            _objects.Add(networkObject.NetworkId, networkObject);
            networkObject.MarkRegistered(true);
            return true;
        }

        public bool Unregister(NetworkId networkId)
        {
            if (!_objects.TryGetValue(networkId, out var networkObject))
                return false;

            networkObject.MarkRegistered(false);
            return _objects.Remove(networkId);
        }

        public bool TryGet(NetworkId networkId, out NetworkObject networkObject)
        {
            return _objects.TryGetValue(networkId, out networkObject);
        }


        public void Clear()
        {
            foreach (var pair in _objects)
            {
                if (pair.Value != null)
                    pair.Value.MarkRegistered(false);
            }

            _objects.Clear();
        }
    }
}