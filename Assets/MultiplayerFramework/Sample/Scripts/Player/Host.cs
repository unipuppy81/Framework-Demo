using MultiplayerFramework.Runtime.Core.Diagnostics;
using MultiplayerFramework.Runtime.Core.Session;
using MultiplayerFramework.Runtime.Core.Tick;
using MultiplayerFramework.Runtime.Core.Transport;
using MultiplayerFramework.Runtime.Netcode.Messages;
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
    private FixedTickScheduler _hostTickScheduler;

    [SerializeField] private bool _isStarted;
    [Space(30)]

    [SerializeField] private GameObject _hostGameObject;
    [SerializeField] private NetworkIdGenerator _networkIdGenerator;
    [SerializeField] private NetworkObjectRegistry _networkObjectRegistry;

    [SerializeField] private string _host_Name = "NAMEHOST";

    [SerializeField] private NetworkId _hostNetworkId;
    [SerializeField] private NetworkObject _hostNetObject;

    void Update()
    {
        _hostTransport?.Poll();
        ConsumeEvent_Host();
    }

    private void OnDestroy()
    {
        _hostTransport?.Dispose();
    }

    public void ConnectHost(GameManager gm, ushort port)
    {
        _hostTransport = new UnityTransportAdapter();
        _hostSerializer = new JsonMessageSerializer();
        _hostSession = new NetworkSession(_hostTransport, _hostSerializer);
        _hostLogger = new SessionDiagnosticsLogger();
        _networkIdGenerator = new NetworkIdGenerator();
        _networkObjectRegistry = new NetworkObjectRegistry();
        _hostTickScheduler = new FixedTickScheduler();
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
    #endregion

}
