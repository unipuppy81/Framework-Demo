using System.Collections.Generic;
using UnityEngine;
using MultiplayerFramework.Runtime.Netcode.StateSync;


namespace MultiplayerFramework.Runtime.NetCode.Objects
{
    public class NetworkObject : MonoBehaviour
    {
        [SerializeField] private int _networkIdValue;
        private readonly List<INetworkSyncBehaviour> _syncBehaviours = new();

        public NetworkId NetworkId { get; private set; } = NetworkId.Invalid;
        public bool IsRegistered { get; private set; }

        private void Awake()
        {
            NetworkId = new NetworkId(_networkIdValue);

            _syncBehaviours.Clear();
            GetComponents(_syncBehaviours);
        }

        public void AssignNetworkId(NetworkId networkId)
        {
            NetworkId = networkId;
            _networkIdValue = networkId.Value;
        }

        public void MarkRegistered(bool value)
        {
            IsRegistered = value;
        }

        public IReadOnlyList<INetworkSyncBehaviour> GetSyncBehaviours()
        {
            return _syncBehaviours;
        }
    }
}

