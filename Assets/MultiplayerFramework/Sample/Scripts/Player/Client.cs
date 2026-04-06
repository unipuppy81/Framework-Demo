using MultiplayerFramework.Runtime.Core.Diagnostics;
using MultiplayerFramework.Runtime.Core.Session;
using MultiplayerFramework.Runtime.Core.Tick;
using MultiplayerFramework.Runtime.Core.Transport;
using MultiplayerFramework.Runtime.Gameplay.Input;
using MultiplayerFramework.Runtime.Netcode.Messages;
using MultiplayerFramework.Runtime.Netcode.Messages.Event;
using MultiplayerFramework.Runtime.NetCode.Objects;
using MultiplayerFramework.Samples;
using PlasticPipe.PlasticProtocol.Client;
using System;
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

    [Header("Input")]
    private readonly InputBuffer _playerAInputBuffer = new();
    private PlayerInputCommand _lastConsumedCommandA;
    [SerializeField] private Transform _playerATransform;
    [SerializeField] private int _playerAHp = 100;
    [SerializeField] private float _playerAMoveSpeed = 5f;
    [SerializeField] private bool _isStartedA;
    private bool _tickBound;

    [Header("로그")]
    private RuntimeDiagnosticsCollector _diagnostics;
    public RuntimeDiagnosticsCollector Diagnostics => _diagnostics;

    [Header("Ping")]
    private float _pingInterval = 1f;
    private float _pingTimer;
    private int _pingSequence;


    private void Awake()
    {
        BindTick();
        _diagnostics = new RuntimeDiagnosticsCollector();
    }

    void Update()
    {   
        SendPing();
        _sessionA?.Poll();

        //_transportA?.Poll();
        //ConsumeEvent_PlayerA();
    }

    public void ConnectClient(string address, ushort port)
    {
        _transportA = new UnityTransportAdapter();
        _serializerA = new JsonMessageSerializer();
        _sessionA = new NetworkSession(_transportA, _serializerA);
        _loggerA = new SessionDiagnosticsLogger();
        PrefabMgr = new PrefabManager();

        //bool result = _transportA.StartClient(address, port);
        bool result = _transportA.ConnectNetwork(address, port, false);

        _sessionA.OnConnected += SendJoinMessage;
        _sessionA.OnMessageReceived += ReceivedNetworkEnvelope;

        _loggerA.LogError(result
               ? $"<color=cyan>[Player A]</color> Client start requested. Target={address} : {port}"
               : $"<color=cyan>[Player A]</color> Client start failed. Target={address} : {port}");

    }
    private void ConsumeEvent_PlayerA()
    {
        if (_transportA == null)
            return;

        while (_transportA.TryDequeueEvent(out NetworkTransportEvent e))
        {
            // HandleEvent_PlayerA(e);
        }
    }

    private void ReceivedNetworkEnvelope(NetworkEnvelope dataReceivedEnvelope)
    {
        switch (dataReceivedEnvelope.Type)
        {
            case NetworkMessageType.JoinResult:
                {
                    if (_serializerA.TryDeserializeT(dataReceivedEnvelope.Payload, out JoinResultMessage joinResult))
                    {
                        if (joinResult.Success)
                        {
                            _loggerA.LogError($"<color=cyan>[Player A]</color> Join 성공. PlayerID={joinResult.CallbackId.Value}, PlayerName={joinResult.CallbackName}");
                        }
                        else
                        {
                            _loggerA.LogError($"<color=cyan>[Player A]</color> Join 실패: {joinResult.ErrorReason}");
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
                    if (_serializerA.TryDeserializeT(dataReceivedEnvelope.Payload, out PlayerStateMessage stateMessage))
                    {
                        PlayerStateSnapshot temp = stateMessage.Snapshot;
                        _loggerA.LogError($"<color=cyan>[Player A]</color> Receive Snapshot {temp.NetworkId}, {temp.Hp}, {temp.Position}");
                    }
                }
                break;
            case NetworkMessageType.Event:
                {
                    if (_serializerA.TryDeserializeT(dataReceivedEnvelope.Payload, out GameplayEventMessage gameEventMessage))
                    {
                        switch (gameEventMessage.EventType)
                        {
                            case GameplayEventType.Jump:
                                {
                                    _loggerA.LogError($"<color=cyan>[Player A]</color> Receive Jump");
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
                        float rttMs = (Time.realtimeSinceStartup - pongMessage.SentTime) * 1000;
                       
                        _diagnostics.ReportRtt(rttMs);
                    }
                }
                break;
            default:
                break;
        }
    }

    private void HandleEvent_PlayerA(NetworkTransportEvent transportEvent)
    {
        switch (transportEvent.Type)
        {
            case NetworkTransportEventType.DataReceived:
                byte[] dataReceivedPayload = transportEvent.Payload;

                if (!_serializerA.TryDeserialize(dataReceivedPayload, out NetworkEnvelope dataReceivedEnvelope))
                    return;


                _loggerA.Log($"<color=cyan>[Player A]</color> 가 {dataReceivedEnvelope.SenderId} 로부터 데이터 받음 {transportEvent.Type.ToString()} 내역은 {dataReceivedEnvelope.Type.ToString()}");
                switch (dataReceivedEnvelope.Type)
                {
                    case NetworkMessageType.JoinResult:
                        {
                            if (_serializerA.TryDeserializeT(dataReceivedEnvelope.Payload, out JoinResultMessage joinResult))
                            {
                                if (joinResult.Success)
                                {
                                    _loggerA.Log($"<color=cyan>[Player A]</color> Join 성공. PlayerID={joinResult.CallbackId}, PlayerName={joinResult.CallbackName}");
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
                                _loggerA.Log($"<color=cyan>[Player A]</color> Receive Snapshot {temp.NetworkId}, {temp.Hp}, {temp.Rotation}, {temp.Position}");
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

    private void SendJoinMessage()
    {
       JoinMessage joinMessage = new JoinMessage(_playerA_Name);
        byte[] connectedPayload = _serializerA.SerializeT(joinMessage);

        NetworkId testSenderID = new NetworkId(9999);

        NetworkEnvelope connectedEnvelope = new NetworkEnvelope(NetworkMessageType.Join, testSenderID, 0, connectedPayload);
        if(_sessionA.Send(connectedEnvelope))
        {
            _loggerA.LogError($"<color=cyan>[Player A]</color> Send Join to Host");
        }
        else
        {
            _loggerA.LogError($"<color=cyan>[Player A]</color> Send Join Failed");
        }
    }

    private void SendPing()
    {
        _diagnostics.Update(Time.deltaTime);

        _pingTimer += Time.deltaTime;

        if (_pingTimer >= _pingInterval)
        {
            _pingTimer = 0f;

            if (_serializerA == null || _transportA == null)
                return;

            PingMessage pingMessage = new PingMessage();
            pingMessage.Sequence = _pingSequence++;
            pingMessage.SentTime = Time.realtimeSinceStartup;

            byte[] message = _serializerA.SerializeT(pingMessage);

            NetworkEnvelope connectedEnvelope = new NetworkEnvelope(NetworkMessageType.Ping, _playerANetworkId, -99, message);
            byte[] connectedData = _serializerA.Serialize(connectedEnvelope);

            _loggerA.Log($"<color=cyan>[Player A]</color>[Ping Send] sentTime={pingMessage.SentTime}");

            _transportA.Send(new System.ArraySegment<byte>(connectedData));

            _diagnostics.ReportPacketSent();

            _loggerA.Log($"<color=cyan>[Player A]</color> 가 ping 전송");
        }

        
    }


    #region 틱 관련
    private void BindTick()
    {
        if (_tickBound || _playerATickScheduler == null)
            return;

        _playerATickScheduler.OnTick += HandlePlayerATick;
        _tickBound = true;
        _playerATransform = GetComponent<Transform>();
    }
    #endregion

    private void HandlePlayerATick(TickContext context)
    {
        // 1
        _diagnostics.ReportTick(context.Tick);

        _lastConsumedCommandA = _playerAInputBuffer.GetOrDefault(context.Tick);


        // 2 
        if (_playerATransform == null)
            return;

        float x = 0f;
        float z = 0f;

        if (Input.GetKey(KeyCode.H)) x -= 1f;
        if (Input.GetKey(KeyCode.K)) x += 1f;
        if (Input.GetKey(KeyCode.U)) z += 1f;
        if (Input.GetKey(KeyCode.J)) z -= 1f;

        Vector2 move = new Vector2(x, z);
        Vector3 moveDir = new Vector3(x, 0f, z);
        _playerATransform.position += moveDir * 5 * context.DeltaTime;

        bool jumpPressed = false;
        bool attackPressed = Input.GetKeyDown(KeyCode.KeypadEnter);
        int targetTick = _playerATickScheduler.CurrentTick + 1;

        PlayerInputCommand command = new PlayerInputCommand(
            targetTick,
            move,
            jumpPressed,
            attackPressed
            );

        _playerAInputBuffer.Store(command);

        PlayerStateSnapshot snapshot = new PlayerStateSnapshot(
           context.Tick,
           1,
           _playerATransform.position,
           _playerATransform.rotation,
           _playerAHp
       );

        PlayerStateMessage message = new PlayerStateMessage(snapshot);

        byte[] payload = _serializerA.SerializeT(message);

        NetworkEnvelope resultEnvelope =
                new NetworkEnvelope(
                    NetworkMessageType.State,
                    senderId: _playerANetworkId,
                    tick: _playerATickScheduler.CurrentTick + 2,
                    payload
                );


        byte[] resultData = _serializerA.Serialize(resultEnvelope);
        if (_transportA.Send(new ArraySegment<byte>(resultData)))
        {
            _loggerA.LogError("<color=cyan>[Player A]</color> Input-State Message Send Successed");
        }
        else
        {
            _loggerA.LogError("<color=cyan>[Player A]</color> Input-State Message Send Failed");
        }
    }

}
