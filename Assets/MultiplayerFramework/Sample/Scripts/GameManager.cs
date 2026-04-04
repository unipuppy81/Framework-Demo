using MultiplayerFramework.Runtime.Core.Diagnostics;
using MultiplayerFramework.Runtime.Core.Session;
using MultiplayerFramework.Runtime.Core.Tick;
using MultiplayerFramework.Runtime.Core.Transport;
using MultiplayerFramework.Runtime.Netcode.Messages;
using MultiplayerFramework.Runtime.NetCode.Objects;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerFramework.Samples
{
    public class GameManager : MonoBehaviour
    {
        [Header("Prefab")]
        public PrefabManager prefabManager;

        [Header("Server")]
        [SerializeField] private string address = "127.0.0.1";
        [SerializeField] private ushort port = 7777;

        [Header("User")]
        public GameObject HostObj;
        public GameObject ClientObj;
        public Host _host;
        public Client _client;

        private void Start()
        {
            HostObj = prefabManager.CreateHost();
            ClientObj = prefabManager.CreateClient();

            _host = HostObj.GetComponent<Host>();
            _client = ClientObj.GetComponent<Client>();

            _host.ConnectHost(this, port);
            _client.ConnectClient(address, port);
        }

    }
}
