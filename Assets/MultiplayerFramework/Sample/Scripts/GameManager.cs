using MultiplayerFramework.Runtime.Core.Diagnostics;
using MultiplayerFramework.Runtime.Core.Session;
using MultiplayerFramework.Runtime.Core.Tick;
using MultiplayerFramework.Runtime.Core.Transport;
using MultiplayerFramework.Runtime.Netcode.Messages;
using MultiplayerFramework.Runtime.NetCode.Objects;
using MultiplayerFramework.Samples.Scripts.UI;
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


        [Header("Diagnostics")]
        [SerializeField] private NetworkDiagnosticsHud _hostHud;
        [SerializeField] private NetworkDiagnosticsHud _clientHud;


        [Header("Simulation")]
        [SerializeField] private NetworkSimulationSettings simulationSettings;

        private void Start()
        {
            HostObj = prefabManager.CreateHost();
            ClientObj = prefabManager.CreateClient();

            _host = HostObj.GetComponent<Host>();
            _client = ClientObj.GetComponentInChildren<Client>();

            if (_hostHud != null)
            {
                _hostHud._host = _host;
                _hostHud.Bind(_host.Diagnostics);
            }


            if (_clientHud != null)
            {
                _clientHud._client = _client;
                _clientHud.Bind(_client.Diagnostics);
            }

            _host.ConnectHost(this, address, port, simulationSettings);
            _client.ConnectClient(address, port);
        }

    }
}
