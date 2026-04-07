using MultiplayerFramework.Runtime.Core.Diagnostics;
using MultiplayerFramework.Runtime.Core.Session;
using MultiplayerFramework.Runtime.Core.Tick;
using MultiplayerFramework.Runtime.Core.Transport;
using MultiplayerFramework.Runtime.Gameplay.Input;
using MultiplayerFramework.Runtime.Netcode.Messages;
using MultiplayerFramework.Runtime.Netcode.Messages.Event;
using MultiplayerFramework.Runtime.NetCode.Objects;
using MultiplayerFramework.Runtime.Sample.Player;
using MultiplayerFramework.Samples;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.UIElements;

public class Host : MonoBehaviour
{
    [Header("Game")]
    public PrefabManager PrefabMgr;
    public GameManager GameMgr;
    private readonly Dictionary<int, ConnectedClientInfo> _clients = new();


    [Header("Host")]
    private UnityTransportAdapter _hostTransport;
    private JsonMessageSerializer _hostSerializer;
    private NetworkSession _hostSession;
    private SessionDiagnosticsLogger _hostLogger;
    [SerializeField] private FixedTickScheduler _hostTickScheduler;
    private readonly InputBuffer _hostInputBuffer = new();


    [Header("Input")]
    private PlayerInputCommand _lastConsumedCommand;
    [SerializeField] private Transform _hostPlayerTransform;
    [SerializeField] private int _hostHp = 100;
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private bool _isStarted;

    private bool _tickBound;
    
    [Space(30)]

    [SerializeField] private GameObject _hostGameObject;
    [SerializeField] private NetworkIdGenerator _networkIdGenerator;
    [SerializeField] private NetworkObjectRegistry _networkObjectRegistry;
    [SerializeField] private NetworkConnectionGenerator _networkConnectionGenerator;

    [SerializeField] private string _host_Name = "NAMEHOST";

    [SerializeField] private NetworkId _hostNetworkId;
    [SerializeField] private NetworkObject _hostNetObject;


    [Header("클라이언트 매핑")]
    private Dictionary<NetworkId, int> _networkToConnectionId = new(); // ObjectId , playerId
    private Dictionary<int, PlayerStateSnapshot> _playerStates; // networkId, playerstatesnapshot

    [Header("로그")]
    private RuntimeDiagnosticsCollector _diagnostics;
    public RuntimeDiagnosticsCollector Diagnostics => _diagnostics;


    private void Awake()
    {
        BindTick();
        _diagnostics = new RuntimeDiagnosticsCollector();
        _playerStates = new Dictionary<int, PlayerStateSnapshot>();
    }
    private void Update()
    {
        _diagnostics.Update(Time.deltaTime);

        CollectHostInput();
        _hostSession?.Poll();

        //_hostTransport?.Poll();
        //ConsumeEvent_Host();

    }

    private void OnDestroy()
    {
        _hostTransport?.Dispose();

        if (_tickBound && _hostTickScheduler != null)
        {
            _hostTickScheduler.OnTick -= HandleHostTick;
            _tickBound = false;
        }
    }

    public void ConnectHost(GameManager gm, string address, ushort port)
    {
        _hostTransport = new UnityTransportAdapter();
        _hostSerializer = new JsonMessageSerializer();
        _hostSession = new NetworkSession(_hostTransport, _hostSerializer);
        _hostLogger = new SessionDiagnosticsLogger();
        _networkIdGenerator = new NetworkIdGenerator();
        _networkObjectRegistry = new NetworkObjectRegistry();
        _networkConnectionGenerator = new NetworkConnectionGenerator();
        PrefabMgr = new PrefabManager();
        GameMgr = gm;

        //bool result = _hostTransport.StartHost(port);
        bool result = _hostSession.ConnectNetwork(address, port, true);

        _hostSession.OnMessageReceived += ReceivedNetworkEnvelope;


        _hostLogger.Log(result
            ? $"<color=red>[Host]</color> Host started on port {port}"
            : $"<color=red>[Host]</color> Host start failed on port {port}");

        _hostNetworkId = _networkIdGenerator.Create();
        _hostNetObject = GetComponent<NetworkObject>();
        _hostNetObject.AssignNetworkId(_hostNetworkId);
        _networkObjectRegistry.Register(_hostNetObject);


        if (result)
            _isStarted = false;
    }

    private void ReceivedNetworkEnvelope(int connectionId, NetworkEnvelope envelope)
    {
        _diagnostics.ReportPacketReceived();

        switch (envelope.Type)
        {
            case NetworkMessageType.Join:
                {
                    SendJoinResult(connectionId, envelope.Payload);
                }
                break;
            case NetworkMessageType.Ping:
                {
                    _hostLogger.Log($"<color=red>[Host </color> Ping Raw Payload] {System.Text.Encoding.UTF8.GetString(envelope.Payload)}");
                    if (_hostSerializer.TryDeserializeT(envelope.Payload, out PingMessage pingMessage))
                    {
                        SendPong(connectionId, pingMessage);
                    }
                }
                break;
            case NetworkMessageType.Input:
                {
                    if (_hostSerializer.TryDeserializeT(envelope.Payload, out PlayerInputMessage stateMessage))
                    {
                        _hostLogger.Log($"<color=red>[Host]</color> Receive Snapshot {stateMessage.NetworkId}, {stateMessage.Move}");
                        SendStateCalculate(stateMessage, envelope.SenderId);
                    }
                }
                break;
        }
    }

    #region 전송
    private void SendJoinResult(int connectionId, byte[] _payload)
    {
        if (_hostSerializer == null)
            return;

        if (_hostSerializer.TryDeserializeT(_payload, out JoinMessage joinMessage))
        {
            _hostLogger.Log($"<color=red>[Host]</color> Join 요청 수신: {joinMessage.PlayerName}");

            //NetworkId newClient = GenerateClientId();
            NetworkId newClient = _networkIdGenerator.Create();
            NetworkObject no = GameMgr.ClientObj.GetComponent<NetworkObject>();
            no.AssignNetworkId(newClient);
            _networkObjectRegistry.Register(no);


            // 1. Join 결과 생성
            JoinResultMessage resultMessage = new JoinResultMessage(
                success: true,
                callbackId: newClient,
                callbackName: joinMessage.PlayerName,
                errorReason: ""
                );

            // 2. 직렬화
            byte[] payload = _hostSerializer.SerializeT(resultMessage);

            NetworkEnvelope resultEnvelope =
                new NetworkEnvelope(
                    NetworkMessageType.JoinResult,
                    senderId: _hostNetworkId,
                    tick: 0,
                    payload
                );

            // 3. 응답 전송
            byte[] resultData = _hostSerializer.Serialize(resultEnvelope);
            if (_hostTransport.SendTo(connectionId, new ArraySegment<byte>(resultData)))
            {
                _hostLogger.Log("<color=red>[Host]</color> Join 승인 응답 전송");

                if (resultMessage.Success)
                    SendSpawn(connectionId, newClient);
            }
            else
            {
                _hostLogger.Log("<color=red>[Host]</color> Join 승인 응답 전송 실패");
            }
        }
    }

    private void SendSpawn(int connectionId, NetworkId _networkID)
    {
        _hostTickScheduler.StartTick();
        Vector3 pos = new Vector3(0, 10, 0);

        SpawnMessage spawnMessage = new SpawnMessage(
                messageType: SpawnMessageType.Spawn,
                spawnPos: pos,
                networkId: _networkID,
                prefabTypeId: 0
            );

        byte[] payload = _hostSerializer.SerializeT(spawnMessage);

        NetworkEnvelope resultEnvelope =
                new NetworkEnvelope(
                    NetworkMessageType.Spawn,
                    senderId: _hostNetworkId,
                    tick: _hostTickScheduler.CurrentTick + 2,
                    payload
                );

        byte[] resultData = _hostSerializer.Serialize(resultEnvelope);
        if (_hostTransport.SendTo(connectionId, new ArraySegment<byte>(resultData)))
        {
            _hostLogger.Log("<color=red>[Host]</color> SpawnMessage 전송 성공");
        }
        else
        {
            _hostLogger.Log("<color=red>[Host]</color> SpawnMessage 전송 실패");
        }

        _networkToConnectionId[_networkID] = connectionId;

        PlayerStateSnapshot _playerStateSnapshot = new PlayerStateSnapshot();
        _playerStateSnapshot.Tick = _hostTickScheduler.CurrentTick + 2;
        _playerStateSnapshot.SenderNetworkId = _networkID.Value;
        _playerStateSnapshot.Position = pos;
        _playerStateSnapshot.Rotation = Quaternion.identity;
        _playerStateSnapshot.Hp = 100;

        _playerStates.Add(_networkID.Value, _playerStateSnapshot);
    }

    private void SendJumpEvent(int _tick)
    {
        GameplayEventMessage message = new GameplayEventMessage(
            tick: _tick,
            networkId: 1,
            eventType: GameplayEventType.Jump,
            value: 0
        );

        byte[] payload = _hostSerializer.SerializeT(message);

        NetworkEnvelope envelope = new NetworkEnvelope(
            NetworkMessageType.Event,
            senderId: _hostNetworkId,
            tick: _hostTickScheduler.CurrentTick + 2,
            payload: payload
        );

        byte[] data = _hostSerializer.Serialize(envelope);

        if (_hostTransport.Broadcast(new ArraySegment<byte>(data)))
        {
            _hostLogger.Log("<color=red>[Host]</color> Event Message Send Succeeded");
        }
        else
        {
            _hostLogger.Log("<color=red>[Host]</color> Event Message Send Failed");
        }
    }

    private void SendPong(int connectionID, PingMessage pingMessage)
    {
        if (_hostSerializer == null || _hostTransport == null)
            return;

 
        PongMessage pongMessage = new PongMessage();
        pongMessage.Sequence = pingMessage.Sequence;
        pongMessage.SentTime = pingMessage.SentTime;

        byte[] payload = _hostSerializer.SerializeT(pongMessage);

        NetworkEnvelope envelope = new NetworkEnvelope(
            NetworkMessageType.Pong,
            senderId: _hostNetworkId,
            tick: _hostTickScheduler.CurrentTick + 2,
            payload: payload
        );

        byte[] data = _hostSerializer.Serialize(envelope);
        _diagnostics.ReportPacketSent();

        if (_hostTransport.SendTo(connectionID, new ArraySegment<byte>(data)))
        {
            _hostLogger.Log("<color=red>[Host]</color> Pong Message Send Succeeded");
        }
        else
        {
            _hostLogger.Log("<color=red>[Host]</color> Pong Message Send Failed");
        }
    }

    private void SendStateSnapshot(int tick)
    {
        // client 로 state 전송
        if (_hostPlayerTransform == null)
            return;

        PlayerStateSnapshot snapshot = new PlayerStateSnapshot(
            tick,
            _hostNetworkId.Value,
            _hostPlayerTransform.position,
            _hostPlayerTransform.rotation,
            _hostHp
        );

        PlayerStateMessage message = new PlayerStateMessage(snapshot);

        byte[] payload = _hostSerializer.SerializeT(message);

        NetworkEnvelope resultEnvelope =
                new NetworkEnvelope(
                    NetworkMessageType.State,
                    senderId: _hostNetworkId,
                    tick: _hostTickScheduler.CurrentTick + 2,
                    payload
                );

        _hostLogger.Log($"<color=red>[Host]</color> Input-State Snapshot {_hostNetworkId.Value} {_hostPlayerTransform.position}");
        byte[] resultData = _hostSerializer.Serialize(resultEnvelope);
        if (_hostTransport.Broadcast(new ArraySegment<byte>(resultData)))
        {
            _hostLogger.Log("<color=red>[Host]</color> Input-State Snapshot Message Send Successed");
        }
        else
        {
            _hostLogger.Log("<color=red>[Host]</color> Input-State Snapshot Message Send Failed");
        }
    }

    private void SendStateCalculate(PlayerInputMessage msg, NetworkId senderId)
    {
        if (_playerStates.TryGetValue(senderId.Value, out PlayerStateSnapshot state))
        {
            Vector3 moveDir = msg.Move;
            if (moveDir.sqrMagnitude > 1f)
                moveDir.Normalize();

            state.Position += moveDir * 5 * _hostTickScheduler.TickInterval;
            state.Tick = msg.Tick;
            _playerStates[senderId.Value] = state;
        }
        else
        {
            _hostLogger.LogError("<color=red>[Host]</color> client SendState is null");
            return;
        }


        PlayerStateCallbackMessage message = new PlayerStateCallbackMessage(state, true);

        byte[] payload = _hostSerializer.SerializeT(message);

        NetworkEnvelope resultEnvelope =
                new NetworkEnvelope(
                    NetworkMessageType.StateCallback,
                    senderId: _hostNetworkId,
                    tick: _hostTickScheduler.CurrentTick,
                    payload
                );

        byte[] resultData = _hostSerializer.Serialize(resultEnvelope);
        int connect = _networkToConnectionId[senderId];
        if (_hostTransport.SendTo(connect, new ArraySegment<byte>(resultData)))
        {
            _hostLogger.Log("<color=red>[Host]</color> Snapshot Callback Message Send Successed");
        }
        else
        {
            _hostLogger.Log("<color=red>[Host]</color> Snapshot Callback Message Send Failed");
        }
    }
    #endregion

    #region 틱 관련
    private void BindTick()
    {
        if (_tickBound || _hostTickScheduler == null)
            return;

        _hostTickScheduler.OnTick += HandleHostTick;
        _tickBound = true;
        _hostPlayerTransform = GetComponent<Transform>();
    }

    private void HandleHostTick(TickContext context)
    {
        _diagnostics.ReportTick(context.Tick);

        // 1. host 입력/명령 소모
        ConsumeHostInput(context);

        // 2. host authoritative 시뮬레이션 진행
        SimulateHostWorld(context);

        // 3. 상태 스냅샷 전송
        SendStateSnapshot(context.Tick);
    }

    private void ConsumeHostInput(TickContext context)
    {
        _lastConsumedCommand = _hostInputBuffer.GetOrDefault(context.Tick);
    }

    private void SimulateHostWorld(TickContext context)
    {
        if (_hostPlayerTransform == null)
            return;

        // 1. 이동
        Vector2 move = _lastConsumedCommand.Move;
        Vector3 moveDir = new Vector3(move.x, 0f, move.y);
        _hostPlayerTransform.position += moveDir * _moveSpeed * context.DeltaTime;

        // 2. 회전
        if (moveDir.sqrMagnitude > 0.0001f)
        {
            _hostPlayerTransform.forward = moveDir.normalized;
        }

        // 3. 이벤트
        if (_lastConsumedCommand.JumpPressed)
        {
            SendJumpEvent(context.Tick);
        }
    }


    #endregion

    #region 입력
    private void CollectHostInput()
    {
        if (_hostTickScheduler == null)
            return;

        Vector2 move = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

        bool attackPressed = Input.GetMouseButtonDown(0);
        bool jumpPressed = Input.GetKeyDown(KeyCode.Space);

        if(jumpPressed)
        {
            TryAttack();
        }

        int targetTick = _hostTickScheduler.CurrentTick + 1;

        PlayerInputCommand command = new PlayerInputCommand(
            targetTick,
            move,
            jumpPressed,
            attackPressed
        );

        _hostInputBuffer.Store(command);
    }

    private void TryAttack()
    {
        List<NetworkId> targets = FindTargetsInRange();

        if (targets.Count == 0)
            return;

        NetworkId targetId = targets[0];
        ApplyDamage(targetId);
    }

    private List<NetworkId> FindTargetsInRange()
    {
        return new List<NetworkId>();
    }

    private void ApplyDamage(NetworkId targetId)
    {

    }
    #endregion
}
