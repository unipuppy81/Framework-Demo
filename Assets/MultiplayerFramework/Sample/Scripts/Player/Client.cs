using MultiplayerFramework.Runtime.Core.Diagnostics;
using MultiplayerFramework.Runtime.Core.Session;
using MultiplayerFramework.Runtime.Core.Tick;
using MultiplayerFramework.Runtime.Core.Transport;
using MultiplayerFramework.Runtime.Netcode.Messages;
using MultiplayerFramework.Runtime.Netcode.Messages.Event;
using MultiplayerFramework.Runtime.NetCode.Objects;
using MultiplayerFramework.Samples;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class Client : MonoBehaviour
{
    public PrefabManager PrefabMgr;

    [Header("Client A")]
    [SerializeField] private GameObject _clientAGameObject;

    private UnityTransportAdapter _transportA;
    private JsonMessageSerializer _serializerA;
    private NetworkSession _sessionA;
    private SessionDiagnosticsLogger _loggerA;
    [SerializeField] private FixedTickScheduler _playerATickScheduler;

    [SerializeField] private string _playerA_Name = "ClientA";

    [SerializeField] private NetworkId _playerANetworkId;
    [SerializeField] private NetworkObject _playerANetObject;

    void Update()
    {
        _transportA?.Poll();
        ConsumeEvent_PlayerA();
    }

    private void OnDestroy()
    {
        _transportA?.Dispose();
    }

    public void ConnectClient(string address, ushort port)
    {
        _transportA = new UnityTransportAdapter();
        _serializerA = new JsonMessageSerializer();
        _sessionA = new NetworkSession(_transportA, _serializerA);
        _loggerA = new SessionDiagnosticsLogger();
        PrefabMgr = new PrefabManager();

        bool result = _transportA.StartClient(address, port);

        _loggerA.Log(result
               ? $"<color=cyan>[Player A]</color> Client start requested. Target={address} : {port}"
               : $"<color=cyan>[Player A]</color> Client start failed. Target={address} : {port}");

    }
    private void ConsumeEvent_PlayerA()
    {
        if (_transportA == null)
            return;

        while (_transportA.TryDequeueEvent(out NetworkTransportEvent e))
        {
            HandleEvent_PlayerA(e);
        }
    }

    private void HandleEvent_PlayerA(NetworkTransportEvent transportEvent)
    {
        switch (transportEvent.Type)
        {
            case NetworkTransportEventType.Connected:
                Debug.Log($"<color=cyan>[Player A]</color>  {transportEvent.Type.ToString()}");
                JoinMessage joinMessage = new JoinMessage(_playerA_Name);
                byte[] connectedPayload = _serializerA.SerializeT(joinMessage);

                NetworkId net = new NetworkId(9999);

                NetworkEnvelope connectedEnvelope = new NetworkEnvelope(NetworkMessageType.Join, net, 0, connectedPayload);
                byte[] connectedData = _serializerA.Serialize(connectedEnvelope);
                _transportA.Send(new System.ArraySegment<byte>(connectedData));
                break;

            case NetworkTransportEventType.DataReceived:
                byte[] dataReceivedPayload = transportEvent.Payload;

                if (!_serializerA.TryDeserialize(dataReceivedPayload, out NetworkEnvelope dataReceivedEnvelope))
                    return;


                Debug.Log($"<color=cyan>[Player A]</color> 가 {dataReceivedEnvelope.SenderId} 로부터 데이터 받음 {transportEvent.Type.ToString()} 내역은 {dataReceivedEnvelope.Type.ToString()}");
                switch (dataReceivedEnvelope.Type)
                {
                    case NetworkMessageType.JoinResult:
                        {
                            if (_serializerA.TryDeserializeT(dataReceivedEnvelope.Payload, out JoinResultMessage joinResult))
                            {
                                if (joinResult.Success)
                                {
                                    Debug.Log($"<color=cyan>[Player A]</color> Join 성공. PlayerID={joinResult.CallbackId}, PlayerName={joinResult.CallbackName}");
                                }
                                else
                                {
                                    Debug.LogError($"<color=cyan>[Player A]</color> Join 실패: {joinResult.ErrorReason}");
                                }
                            }
                        }
                        break;
                    case NetworkMessageType.Spawn:
                        {
                            if (_serializerA.TryDeserializeT(dataReceivedEnvelope.Payload, out SpawnMessage spawnMessage))
                            {
                                if (spawnMessage.MessageType == SpawnMessageType.Spawn)
                                {
                                    _playerANetworkId = spawnMessage.NetworkId;
                                    _playerANetObject.AssignNetworkId(_playerANetworkId);
                                    _playerATickScheduler.StartTick(dataReceivedEnvelope.Tick);
                                }
                            }
                        }
                        break;
                    case NetworkMessageType.State:
                        {
                            if (_serializerA.TryDeserializeT(dataReceivedEnvelope.Payload, out PlayerStateMessage spawnMessage))
                            {
                                PlayerStateSnapshot temp = spawnMessage.Snapshot;
                                Debug.LogWarning($"<color=cyan>[Player A]</color> Receive Snapshot {temp.NetworkId}, {temp.Hp}, {temp.Rotation}, {temp.Position}");
                            }
                        }
                        break;
                    case NetworkMessageType.Event:
                        {
                            if (_serializerA.TryDeserializeT(dataReceivedEnvelope.Payload, out GameplayEventMessage gameEventMessage))
                            {
                                switch(gameEventMessage.EventType)
                                {
                                    case GameplayEventType.Jump:
                                        {
                                            Debug.LogWarning($"<color=cyan>[Player A]</color> Receive Jump");
                                        }
                                        break;
                                }
                            }
                        }
                        break;

                }
                break;

        }
    }

}
