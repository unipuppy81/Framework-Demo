using MultiplayerFramework.Runtime.NetCode.Objects;
using System;

namespace MultiplayerFramework.Runtime.Netcode.Messages
{    /// <summary>
     /// 네트워크 메시지의 분류
     /// </summary>
    public enum NetworkMessageType : byte
    {
        None = 0,

        // 입력 명령
        Input = 1,

        // 지속 상태 스냅샷 (위치, 체력, 점수 등)
        State = 2,

        // 일회성 이벤트 (피격 알림, 리스폰 알림 등)
        Event = 3,

        // 오브젝트 생성/제거 수명주기 관련
        Spawn = 4,
        Despawn = 5,

        // 접속 관련
        Join = 6,
        Leave = 7,
        JoinResult = 8,

        Ping,
        Pong,

        StateCallback,
        PlayerDeath,
        // 디버그
        Diagnostic
    }
    public enum SpawnMessageType
    {
        Spawn = 0,
        Despawn = 1
    }

    [Serializable]
    public struct SpawnMessage
    {
        public SpawnMessageType MessageType;
        public NetworkId NetworkId;
        public int PrefabTypeId;
        public UnityEngine.Vector3 SpawnPos;

        public SpawnMessage(SpawnMessageType messageType, UnityEngine.Vector3 spawnPos, NetworkId networkId, int prefabTypeId)
        {
            MessageType = messageType;
            SpawnPos = spawnPos;
            NetworkId = networkId;
            PrefabTypeId = prefabTypeId;
        }
    }

    [Serializable]
    public struct InputMessage
    {
        public int Tick;
        public int PlayerId;
        public float MoveX;
        public float MoveY;
        public bool DashPressed;
        public bool AttackPressed;
    }

    /// <summary>
    /// 클라이언트가 세션 참가를 요청할 때 사용하는 메시지
    /// </summary>
    [Serializable]
    public struct JoinMessage
    {
        public string PlayerName;

        public JoinMessage(string playerName)
        {
            PlayerName = playerName;
        }
    }

    [Serializable]
    public struct JoinResultMessage
    {
        public bool Success;
        public NetworkId CallbackId;
        public string CallbackName;
        public string ErrorReason;

        public JoinResultMessage(bool success, NetworkId callbackId, string callbackName, string errorReason)
        {
            Success = success;
            CallbackId = callbackId;
            CallbackName = callbackName;
            ErrorReason = errorReason;
        }
    }

    /// <summary>
    /// 클라이언트가 세션 이탈 의사를 알릴 때 사용하는 메시지
    /// </summary>
    [Serializable]
    public readonly struct LeaveMessage
    {
        public readonly string PlayerId;

        public LeaveMessage(string playerId)
        {
            PlayerId = playerId;
        }
    }

    [Serializable]
    public struct StateMessage
    {
        public int Tick;
        public NetworkId NetworkId;
    }


    [Serializable]
    public struct PlayerStateMessage
    {
        public PlayerStateSnapshot Snapshot;

        public PlayerStateMessage(PlayerStateSnapshot snapshot)
        {
            Snapshot = snapshot;
        }
    }

    [Serializable]
    public struct PlayerInputMessage
    {
        public int Tick;
        public int NetworkId;
        public UnityEngine.Vector3 Move;
        public bool JumpPressed;
        public bool AttackPressed;

        public PlayerInputMessage(int tick, int networkId, UnityEngine.Vector3 move, bool jumpPressed, bool attackPressed)
        {
            Tick = tick;
            NetworkId = networkId;
            Move = move;
            JumpPressed = jumpPressed;
            AttackPressed = attackPressed;
        }
    }


    [Serializable]
    public struct PlayerStateCallbackMessage
    {
        public PlayerStateSnapshot Snapshot;
        public bool IsMove;

        public PlayerStateCallbackMessage(PlayerStateSnapshot snapshot, bool isMove)
        {
            Snapshot = snapshot;
            IsMove = isMove;
        }
    }

    [Serializable]
    public struct PlayerDeathMessage
    {
        public int TargetId;
        public int CurrentTick;

        public PlayerDeathMessage(int targetId, int currentTick)
        {
            TargetId = targetId;
            CurrentTick = currentTick;
        }
    }
}