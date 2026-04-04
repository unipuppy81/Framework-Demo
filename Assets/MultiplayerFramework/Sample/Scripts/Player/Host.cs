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
using System.Collections;
using System.Collections.Generic;
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

    [SerializeField] private string _host_Name = "NAMEHOST";

    [SerializeField] private NetworkId _hostNetworkId;
    [SerializeField] private NetworkObject _hostNetObject;

    private void Awake()
    {
        BindTick();
    }
    private void Update()
    {
        CollectHostInput();

        _hostTransport?.Poll();
        ConsumeEvent_Host();
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

    public void ConnectHost(GameManager gm, ushort port)
    {
        _hostTransport = new UnityTransportAdapter();
        _hostSerializer = new JsonMessageSerializer();
        _hostSession = new NetworkSession(_hostTransport, _hostSerializer);
        _hostLogger = new SessionDiagnosticsLogger();
        _networkIdGenerator = new NetworkIdGenerator();
        _networkObjectRegistry = new NetworkObjectRegistry();
        PrefabMgr = new PrefabManager();
        GameMgr = gm;

        bool result = _hostTransport.StartHost(port);

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

    private void ConsumeEvent_Host()
    {
        if (_hostTransport == null)
            return;

        while (_hostTransport.TryDequeueEvent(out NetworkTransportEvent transportEvent))
        {
            HandleEvent_Host(transportEvent);
        }
    }

    private void HandleEvent_Host(NetworkTransportEvent transportEvent)
    {
        switch (transportEvent.Type)
        {
            case NetworkTransportEventType.DataReceived:
                {
                    byte[] data = transportEvent.Payload;

                    if (!_hostSerializer.TryDeserialize(data, out NetworkEnvelope envelope))
                        return;

                    Debug.Log($"<color=red>[Host]</color> {transportEvent.Type.ToString()} - {envelope.Type.ToString()}");

                    switch (envelope.Type)
                    {
                        case NetworkMessageType.Join:
                            {
                                SendJoinResult(envelope.Payload);
                            }
                            break;
                    }
                }
                break;
        }
    }

    #region 전송
    private void SendJoinResult(byte[] _payload)
    {
        if (_hostSerializer == null)
            return;

        if (_hostSerializer.TryDeserializeT(_payload, out JoinMessage joinMessage))
        {
            Debug.Log($"<color=red>[Host]</color> Join 요청 수신: {joinMessage.PlayerName}");

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
            if (_hostTransport.SendTo(1, new ArraySegment<byte>(resultData)))
            {
                Debug.Log("<color=red>[Host]</color> Join 승인 응답 전송");

                if (resultMessage.Success)
                    SendSpawn(newClient);
            }
            else
            {
                Debug.Log("<color=red>[Host]</color> Join 승인 응답 전송 실패");
            }
        }
    }

    private void SendSpawn(NetworkId _networkID)
    {
        _hostTickScheduler.StartTick();

        SpawnMessage spawnMessage = new SpawnMessage(
                messageType: SpawnMessageType.Spawn,
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

        Debug.Log("<color=red>[Host]</color> SpawnMessage 완성");

        byte[] resultData = _hostSerializer.Serialize(resultEnvelope);
        if (_hostTransport.SendTo(1, new ArraySegment<byte>(resultData)))
        {
            Debug.Log("<color=red>[Host]</color> SpawnMessage 전송 성공");
        }
        else
        {
            Debug.Log("<color=red>[Host]</color> SpawnMessage 전송 실패");
        }
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
            tick: message.Tick,
            payload: payload
        );

        byte[] data = _hostSerializer.Serialize(envelope);

        if (_hostTransport.SendTo(1, new ArraySegment<byte>(data)))
        {
            Debug.Log("<color=red>[Host]</color> Event Message Send Succeeded");
        }
        else
        {
            Debug.Log("<color=red>[Host]</color> Event Message Send Failed");
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
        // Debug.Log($"<color=red>[Host]</color> Tick={context.Tick}");

        // 1. host 입력/명령 소모
        ConsumeHostInput(context);

        // 2. host authoritative 시뮬레이션 진행
        SimulateHostWorld(context);

        // 3. 상태 스냅샷 전송
        SendStateSnapshot(context.Tick);
    }

    private void ConsumeHostInput(TickContext context)
    {
        Debug.Log($"<color=red>[Host]</color> ConsumeHostInput={context.Tick}");
        _lastConsumedCommand = _hostInputBuffer.GetOrDefault(context.Tick);
    }

    private void SimulateHostWorld(TickContext context)
    {
        Debug.Log($"<color=red>[Host]</color> SimulateHostWorld={context.Tick}");

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

        Debug.Log($"<color=red>[Host]</color> SimulateHostWorld Finished={context.Tick}");
    }

    private void SendStateSnapshot(int tick)
    {
        Debug.Log($"<color=red>[Host]</color> SendStateSnapshot");
        // client 로 state 전송
        if (_hostPlayerTransform == null)
            return;

        PlayerStateSnapshot snapshot = new PlayerStateSnapshot(
            tick,
            1, // host player networkId
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
        if (_hostTransport.SendTo(1, new ArraySegment<byte>(resultData)))
        {
            Debug.Log("<color=red>[Host]</color> Input-State Message Send Successed");
        }
        else
        {
            Debug.Log("<color=red>[Host]</color> Input-State Message Send Failed");
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

        int targetTick = _hostTickScheduler.CurrentTick + 1;

        PlayerInputCommand command = new PlayerInputCommand(
            targetTick,
            move,
            jumpPressed,
            attackPressed
        );

        _hostInputBuffer.Store(command);
    }
    #endregion
}
