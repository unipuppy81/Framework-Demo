using System.Collections.Generic;
using UnityEngine;
using MultiplayerFramework.Runtime.Netcode.StateSync;


namespace MultiplayerFramework.Runtime.NetCode.Objects
{
    public sealed class ConnectedClientInfo
    {
        public int ConnectionId;
        public int PlayerId;
        public string PlayerName;
        public NetworkId PlayerNetworkId;
    }

    public class NetworkObject : MonoBehaviour
    {
        private readonly List<INetworkSyncBehaviour> _syncBehaviours = new();

        public NetworkId NetworkId { get; private set; } = NetworkId.Invalid;
        public bool IsRegistered { get; private set; }


        public void AssignNetworkId(NetworkId networkId)
        {
            //_syncBehaviours.Clear();
            //GetComponents(_syncBehaviours);

            NetworkId = networkId;
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

