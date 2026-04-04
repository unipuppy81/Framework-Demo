using MultiplayerFramework.Runtime.Core.Diagnostics;
using MultiplayerFramework.Runtime.Core.Session;
using MultiplayerFramework.Runtime.Core.Tick;
using MultiplayerFramework.Runtime.Core.Transport;
using MultiplayerFramework.Runtime.Netcode.Messages;
using MultiplayerFramework.Runtime.Netcode.Messages.Event;
using MultiplayerFramework.Runtime.NetCode.Objects;
using MultiplayerFramework.Samples;
using PlasticPipe.PlasticProtocol.Client;
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


    [Header("로그")]
    private RuntimeDiagnosticsCollector _diagnostics;
    public RuntimeDiagnosticsCollector Diagnostics => _diagnostics;

    [Header("Ping")]
    private float _pingInterval = 1f;
    private float _pingTimer;
    private int _pingSequence;


    private void Awake()
    {
        _diagnostics = new RuntimeDiagnosticsCollector();
    }

    void Update()
    {   
        _diagnostics.Update(Time.deltaTime);

        _pingTimer += Time.deltaTime;

        if (_pingTimer >= _pingInterval)
        {
            _pingTimer = 0f;
            SendPing();
        }
        

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
                
                if(_transportA.Send(new System.ArraySegment<byte>(connectedData)))
                {
                    Debug.Log($"<color=cyan>[Player A]</color> Send Join");
                }
                else
                {
                    Debug.Log($"<color=cyan>[Player A]</color> Send Join Failed");
                }
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
                                Debug.Log($"<color=cyan>[Player A]</color> Receive Snapshot {temp.NetworkId}, {temp.Hp}, {temp.Rotation}, {temp.Position}");
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
                    case NetworkMessageType.Pong:
                        {
                            _diagnostics.ReportPacketReceived();

                            if (_serializerA.TryDeserializeT(dataReceivedEnvelope.Payload, out PongMessage pongMessage))
                            {
                                Debug.LogError($"<color=cyan>[Player A]</color> 가 {dataReceivedEnvelope.SenderId} 로부터 pong 받음");

                                float rttMs = (Time.realtimeSinceStartup - pongMessage.SentTime) * 1000;
                                Debug.LogError($"<color=cyan>[Playr A]</color> {Time.realtimeSinceStartup} - {pongMessage.SentTime} = {rttMs}");

                                _diagnostics.ReportRtt(rttMs);
                            }
                        }
                        break;
                    default:
                        break;
                }
                break;

        }
    }

    private void SendPing()
    {
        if (_serializerA == null || _transportA == null)
            return;

        PingMessage pingMessage = new PingMessage();
        pingMessage.Sequence = _pingSequence++;
        pingMessage.SentTime = Time.realtimeSinceStartup;

        byte[] message = _serializerA.SerializeT(pingMessage);

        NetworkEnvelope connectedEnvelope = new NetworkEnvelope(NetworkMessageType.Ping, _playerANetworkId, -99, message);
        byte[] connectedData = _serializerA.Serialize(connectedEnvelope);

        Debug.Log($"<color=cyan>[Client]</color>[Ping Send] sentTime={pingMessage.SentTime}");

        _transportA.Send(new System.ArraySegment<byte>(connectedData));

        _diagnostics.ReportPacketSent();

        Debug.Log($"<color=cyan>[Player A]</color> 가 ping 전송");
    }
}
