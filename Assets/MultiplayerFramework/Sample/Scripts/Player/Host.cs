using MultiplayerFramework.Runtime.AOI;
using MultiplayerFramework.Runtime.Core.Diagnostics;
using MultiplayerFramework.Runtime.Core.Session;
using MultiplayerFramework.Runtime.Core.Tick;
using MultiplayerFramework.Runtime.Core.Transport;
using MultiplayerFramework.Runtime.Gameplay.Input;
using MultiplayerFramework.Runtime.Netcode.Messages;
using MultiplayerFramework.Runtime.Netcode.Messages.Event;
using MultiplayerFramework.Runtime.NetCode.Objects;
using MultiplayerFramework.Samples;
using System;
using System.Collections.Generic;
using UnityEditor.MemoryProfiler;
using UnityEngine;


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


    [Header("Tick")]
    // NetworkID, Tick, PlayerInputMessage
    private Dictionary<int, Dictionary<int, PlayerInputMessage>> _inputMessageStore;

    [Header("Input")]
    private PlayerInputCommand _lastConsumedCommand;
    [SerializeField] private Transform _hostPlayerTransform;
    [SerializeField] private float _hostVerticalVelocity;
    [SerializeField] private bool _hostIsGrounded;
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

    [Header("Simulation Transport")]
    [SerializeField] private NetworkSimulationSettings simulationSettings;
    private SimulatedTransportAdapter _simulatedTransport;
    private Dictionary<int, int> _lastProcessedInputTickByConnection = new();



    [Header("AOI")]
    private AOISystem _aoiSystem;
    private readonly Dictionary<int, NetworkId> _observerByConnection = new();
    private List<NetworkId> _entered = new();
    private List<NetworkId> _exited = new();

    [Header("로그")]
    private RuntimeDiagnosticsCollector _diagnostics;
    public RuntimeDiagnosticsCollector Diagnostics => _diagnostics;


    private void Awake()
    {
        BindTick();
        _diagnostics = new RuntimeDiagnosticsCollector();
        _aoiSystem = new AOISystem(enterRadius: 12f, exitRadius: 15f);
        _playerStates = new Dictionary<int, PlayerStateSnapshot>();
        _inputMessageStore = new Dictionary<int, Dictionary<int, PlayerInputMessage>>();
    }

    private void Update()
    {
        _diagnostics.Update(Time.deltaTime);

        CollectHostInput();
        _hostSession?.Poll();
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

    public void ConnectHost(GameManager gm, string address, ushort port, NetworkSimulationSettings set)
    {
        _hostTransport = new UnityTransportAdapter();
        _hostSerializer = new JsonMessageSerializer();
        _hostLogger = new SessionDiagnosticsLogger();
        _networkIdGenerator = new NetworkIdGenerator();
        _networkObjectRegistry = new NetworkObjectRegistry();
        _networkConnectionGenerator = new NetworkConnectionGenerator();
        PrefabMgr = new PrefabManager();
        GameMgr = gm;
        simulationSettings = set;
        _simulatedTransport = new SimulatedTransportAdapter(_hostTransport, simulationSettings);
        _hostSession = new NetworkSession(_simulatedTransport, _hostSerializer);
        _lastProcessedInputTickByConnection = new Dictionary<int, int>();
        //_hostSession = new NetworkSession(_hostTransport, _hostSerializer);



        bool result = _hostSession.ConnectNetwork(address, port, true);

        _hostSession.OnMessageReceived += ReceivedNetworkEnvelope;


        _hostLogger.Log(result
            ? $"<color=red>[Host]</color> Host started on port {port}"
            : $"<color=red>[Host]</color> Host start failed on port {port}");



        _hostNetworkId = _networkIdGenerator.Create();
        _hostNetObject = GetComponent<NetworkObject>();
        _hostNetObject.AssignNetworkId(_hostNetworkId);
        _networkObjectRegistry.Register(_hostNetObject);
        _observerByConnection[_hostNetworkId.Value] = _hostNetworkId;
        _hostLogger.LogError($"<color=red>[Host]</color> 현재 연결된 사람 수: {_observerByConnection.Count} " +
            $"\n호스트 네트워크 아이디 : {_hostNetworkId.Value}");

        if (result)
            _isStarted = false;
    }

    private void ReceivedNetworkEnvelope(int connectionId, NetworkEnvelope envelope)
    {
        _diagnostics.ReportRemoteTick(envelope.Tick);
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
                    if (_hostSerializer.TryDeserializeT(envelope.Payload, out PlayerInputMessage inputMessage))
                    {
                        _hostLogger.Log(
                            $"<color=red>[Host]</color> Receive Input " +
                            $"sender={envelope.SenderId}, netId={inputMessage.NetworkId}, tick={inputMessage.Tick}, move={inputMessage.Move}"
                        );

                        StoreInputMessage(envelope.SenderId.Value, inputMessage);
                    }
                    else
                    {
                        _hostLogger.Log("<color=red>[Host]</color> Failed to deserialize PlayerInputMessage");
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

            NetworkId newClient = _networkIdGenerator.Create();
            NetworkObject no = GameMgr.ClientObj.GetComponentInChildren<NetworkObject>();
            no.AssignNetworkId(newClient);
            _networkObjectRegistry.Register(no);
            _observerByConnection[newClient.Value] = newClient;

            // 현재 연결 인원 수 출력
            Debug.LogError($"<color=red>[Host]</color> 현재 연결된 사람 수: {_observerByConnection.Count} / newClient ID = {newClient.Value}");
            _diagnostics.ReportVisibleCount(_observerByConnection.Count);

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
            if (_hostSession.SendTo(connectionId, new ArraySegment<byte>(resultData)))
            {
                _hostLogger.Log("<color=red>[Host]</color> Join 승인 응답 전송");
                _diagnostics.ReportPacketSent();

                if (resultMessage.Success)
                {
                    _diagnostics.ReportPacketSent();
                    SendSpawn(connectionId, newClient);
                }

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

        if (_hostSession.SendTo(connectionId, new ArraySegment<byte>(resultData)))
        {
            _diagnostics.ReportPacketSent();
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
            eventType: GameplayEventType.Hit,
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

        //if (_hostTransport.Broadcast(new ArraySegment<byte>(data)))
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

        if (_hostSession.SendTo(connectionID, new ArraySegment<byte>(data)))
        {
            _diagnostics.ReportPacketSent();
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
            _hostVerticalVelocity,
            _hostIsGrounded,
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

        byte[] resultData = _hostSerializer.Serialize(resultEnvelope);
        _hostLogger.Log($"<color=red>[Host]</color> Input-State Snapshot Network ID : {_hostNetworkId.Value} / Position : {_hostPlayerTransform.position}");

        foreach (var pair in _observerByConnection)
        {
            int connectionId = pair.Key;
            NetworkId observerNetworkId = pair.Value;

            if (observerNetworkId.Equals(_hostNetworkId))
                continue;

            // host가 visible 한지 검사
            if (_aoiSystem.IsVisible(connectionId, _hostNetworkId) == false)
                continue;

            if (_hostSession.SendTo(connectionId, new ArraySegment<byte>(resultData)))
            {
                _diagnostics.ReportPacketSent();
                _hostLogger.Log($"<color=red>[Host]</color> State Send Success conn={connectionId}");
            }
            else
            {
                _hostLogger.Log($"<color=red>[Host]</color> State Send Failed conn={connectionId}");
            }
        }
    }

    private void SendStateCalculate(PlayerInputMessage msg, NetworkId senderId)
    {
        if (!_playerStates.TryGetValue(senderId.Value, out PlayerStateSnapshot state))
        {
            _hostLogger.Log("<color=red>[Host]</color> client SendState is null");
            return;
        }

        // 1. 수평 입력
        Vector3 moveDir = msg.Move;
        moveDir.y = 0f;

        if (moveDir.sqrMagnitude > 1f)
            moveDir.Normalize();

        float dt = _hostTickScheduler.TickInterval;
        float moveSpeed = _moveSpeed;
        float gravity = -20f;
        float groundY = 0f;

        // 2. 수평 이동
        state.Position += moveDir * moveSpeed * dt;

        // 3. 접지 상태 갱신
        state.IsGrounded = state.Position.y <= groundY + 0.001f;

        // 4. 공중 판정 및 중력 적용
        if(!state.IsGrounded)
        {
            state.VerticalVelocity += gravity * dt;
        }
        else
        {
            state.VerticalVelocity = 0f;
        }

        // 5. 수직 이동
        state.Position.y += state.VerticalVelocity * dt;

        // 6. 바닥 보정
        float groundEpsilon = 1f;

        if(state.Position.y <= groundY + groundEpsilon)
        {
            state.Position.y = groundY;
            state.VerticalVelocity = 0f;
            state.IsGrounded = true;
        }
        else
        {
            state.IsGrounded = false;
        }

        state.Tick = msg.Tick;
        _playerStates[senderId.Value] = state;

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

        if (_hostSession.SendTo(connect, new ArraySegment<byte>(resultData)))
        {
            _diagnostics.ReportPacketSent();
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

        // 1. host 입력 소모
        ConsumeHostInput(context);


        // 2. host authoritative 시뮬레이션
        SimulateHostWorld(context);

        // 3. remote Client 입력 소모
        SimulateRemotePlayers(context);

        // 4. AOI 갱신
        UpdateAOI();

        // 5. 상태 스냅샷 전송
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
        if (_lastConsumedCommand.AttackPressed)
        {
            SendJumpEvent(context.Tick);
        }
    }

    private void SimulateRemotePlayers(TickContext curTick)
    {
        var currentTick = curTick.Tick;
        foreach (KeyValuePair<int, NetworkId> pair in _observerByConnection)
        {
            int connectionId = pair.Key;
            NetworkId networkId = pair.Value;

            if (networkId.Equals(_hostNetworkId))
                continue;

            if (!TryGetNextConsumableInputMessage(connectionId, currentTick, out PlayerInputMessage inputMessage))
                continue;

            SendStateCalculate(inputMessage, networkId);

            // 마지막 처리 tick 기록
            _lastProcessedInputTickByConnection[connectionId] = inputMessage.Tick;
            CleanupProcessedInputMessages(connectionId);

            _hostLogger.Log(
                $"<color=red>[Host]</color> Simulate Remote " +
                $"conn={connectionId}, netId={networkId.Value}, tick={currentTick}"
            );
        }
    }

    // Client 입력 저장
    private void StoreInputMessage(int connectionId, PlayerInputMessage inputMessage)
    {
        // connectionId에 해당하는 입력 저장소가 없으면 생성
        if (!_inputMessageStore.TryGetValue(connectionId, out Dictionary<int, PlayerInputMessage> tickStore))
        {
            tickStore = new Dictionary<int, PlayerInputMessage>();
            _inputMessageStore.Add(connectionId, tickStore);
        }

        // 같은 tick 입력이 다시 오면 최신값으로 덮어쓰기
        tickStore[inputMessage.Tick] = inputMessage;

        _hostLogger.Log(
            $"<color=red>[Host]</color> Input Stored " +
            $"conn={connectionId}, netId={inputMessage.NetworkId}, tick={inputMessage.Tick}, move={inputMessage.Move}"
        );
    }

    private bool TryGetInputMessage(int connectionId, int tick, out PlayerInputMessage inputMessage)
    {
        inputMessage = default;

        if (!_inputMessageStore.TryGetValue(connectionId, out Dictionary<int, PlayerInputMessage> tickStore))
            return false;

        return tickStore.TryGetValue(tick, out inputMessage);
    }

    private bool TryGetNextConsumableInputMessage(int connectionId, int currentTick, out PlayerInputMessage inputMessage)
    {
        inputMessage = default;

        if (_inputMessageStore.TryGetValue(connectionId, out Dictionary<int, PlayerInputMessage> tickStore) == false)
            return false;

        // 마지막 처리 tick. 없으면 -1부터 시작
        int lastProcessedTick = -1;
        _lastProcessedInputTickByConnection.TryGetValue(connectionId, out lastProcessedTick);

        int bestTick = int.MaxValue;
        bool found = false;

        foreach (KeyValuePair<int, PlayerInputMessage> pair in tickStore)
        {
            int tick = pair.Key;

            // 아직 처리 안 했고, 현재 host tick 이하인 입력만 대상
            if (tick <= lastProcessedTick)
                continue;

            if (tick > currentTick)
                continue;

            // 처리 가능한 입력 중 가장 오래된 tick부터 소비
            if (tick < bestTick)
            {
                bestTick = tick;
                inputMessage = pair.Value;
                found = true;
            }
        }

        return found;
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

        bool attackPressed = Input.GetKeyDown(KeyCode.Space); 
        bool jumpPressed = Input.GetMouseButtonDown(0);

        int targetTick = _hostTickScheduler.CurrentTick + 1;

        PlayerInputCommand command = new PlayerInputCommand(
            targetTick,
            move,
            jumpPressed,
            attackPressed
        );

        _hostInputBuffer.Store(command);

        if (attackPressed)
        {
            TryAttack();
        }
    }

    private void TryAttack()
    {
        List<NetworkId> targets = FindTargetsInRange();

        if (targets.Count == 0)
            return;

        NetworkId targetId = targets[0];
        ApplyDamageAndSendPlayerState(targetId);
    }

    private List<NetworkId> FindTargetsInRange()
    {
        List<NetworkId> result = new List<NetworkId>();
        
        Vector3 hostPos = _hostPlayerTransform.position;
        float attackRange = 5f;

        foreach(KeyValuePair<int, PlayerStateSnapshot> pair in _playerStates)
        {
            PlayerStateSnapshot targetState = pair.Value;

            // Host 자기 자신은 제외
            if (targetState.SenderNetworkId == _hostNetworkId.Value)
                continue;

            float distance = Vector3.Distance(hostPos, targetState.Position);
            if (distance <= attackRange)
            {
                result.Add(new NetworkId(targetState.SenderNetworkId));
            }
        }

        return result;
    }

    private void ApplyDamageAndSendPlayerState(NetworkId targetId)
    {
        if (_playerStates.TryGetValue(targetId.Value, out PlayerStateSnapshot targetState) == false)
        {
            _hostLogger.LogError($"<color=red>[Host]</color> ApplyDamage failed. target not found: {targetId.Value}");
            return;
        }

        int damage = 10;
        targetState.Hp = Mathf.Max(0, targetState.Hp - damage);
        targetState.Tick = _hostTickScheduler.CurrentTick;

        bool isDead = targetState.Hp <= 0;

        _playerStates[targetId.Value] = targetState;

        _hostLogger.Log($"<color=red>[Host]</color> Damage Applied Target={targetId.Value}, Hp={targetState.Hp}");

        // 피해받은 대상에게 최신 상태 전송
        PlayerStateCallbackMessage message = new PlayerStateCallbackMessage(targetState, true);
        byte[] payload = _hostSerializer.SerializeT(message);

        NetworkEnvelope resultEnvelope = new NetworkEnvelope(
            NetworkMessageType.StateCallback,
            senderId: _hostNetworkId,
            tick: _hostTickScheduler.CurrentTick,
            payload
        );

        byte[] resultData = _hostSerializer.Serialize(resultEnvelope);

        if (_networkToConnectionId.TryGetValue(targetId, out int connectionId))
        {
            if (_hostSession.SendTo(connectionId, new ArraySegment<byte>(resultData)))
            {
                _diagnostics.ReportPacketSent();
                _hostLogger.Log($"<color=red>[Host]</color> Damage State Send Success Target={targetId.Value}");
            }
            else
            {
                _hostLogger.Log($"<color=red>[Host]</color> Damage State Send Failed Target={targetId.Value}");
            }
        }


        if (isDead)
            HandleDeath(targetId, targetState);
    }

    private void HandleDeath(NetworkId targetId, PlayerStateSnapshot targetState)
    {
        _hostLogger.Log($"<color=red>[Host]</color> Target Dead = {targetId.Value}");

        PlayerDeathMessage deathMessage = new PlayerDeathMessage(
            targetId.Value,
            _hostTickScheduler.CurrentTick
        );

        byte[] payload = _hostSerializer.SerializeT(deathMessage);

        NetworkEnvelope envelope = new NetworkEnvelope(
            NetworkMessageType.PlayerDeath,
            senderId: _hostNetworkId,
            tick: _hostTickScheduler.CurrentTick,
            payload
        );

        byte[] data = _hostSerializer.Serialize(envelope);

        if (_networkToConnectionId.TryGetValue(targetId, out int connectionId))
        {
            _diagnostics.ReportPacketSent();
            _hostSession.SendTo(connectionId, new ArraySegment<byte>(data));
        }
    }
    #endregion

    #region AOI

    private void UpdateAOI()
    {
        foreach (KeyValuePair<int, NetworkId> pair in _observerByConnection)
        {
            int connectionId = pair.Key;
            NetworkId observerNetworkId = pair.Value;

            if (_networkObjectRegistry.TryGet(observerNetworkId, out NetworkObject observerObject) == false)
                continue;

            _aoiSystem.UpdateObserver(
                connectionId,
                observerNetworkId.Value,
                observerObject.transform.position,
                _networkObjectRegistry,
                _entered,
                _exited
            );

            for (int i = 0; i < _entered.Count; i++)
            {
                _hostLogger.LogWarning($"<color=red>[Host AOI]</color> Enter conn={connectionId}, target={_entered[i]}");
            }

            for (int i = 0; i < _exited.Count; i++)
            {
                _hostLogger.LogWarning($"<color=red>[Host AOI]</color> Exit conn={connectionId}, target={_exited[i]}");
            }
        }
    }

    private void OnDrawGizmos()
    {
        Vector3 center = transform.position;

        // enter 반경
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(center, 12);

        // exit 반경
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, 15);
    }
    #endregion

    private void CleanupProcessedInputMessages(int connectionId)
    {
        if (_inputMessageStore.TryGetValue(connectionId, out Dictionary<int, PlayerInputMessage> tickStore) == false)
            return;

        if (_lastProcessedInputTickByConnection.TryGetValue(connectionId, out int lastProcessedTick) == false)
            return;

        List<int> removeKeys = null;

        foreach (KeyValuePair<int, PlayerInputMessage> pair in tickStore)
        {
            if (pair.Key > lastProcessedTick)
                continue;

            if (removeKeys == null)
                removeKeys = new List<int>();

            removeKeys.Add(pair.Key);
        }

        if (removeKeys == null)
            return;

        for (int i = 0; i < removeKeys.Count; i++)
        {
            tickStore.Remove(removeKeys[i]);
        }
    }
}
