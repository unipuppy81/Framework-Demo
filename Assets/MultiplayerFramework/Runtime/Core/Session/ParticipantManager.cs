using MultiplayerFramework.Runtime.NetCode.Objects;
using System.Collections.Generic;

namespace MultiplayerFramework.Runtime.Core.Session
{
    public struct Participant
    {
        public int PlayerId;
        public int ConnectionId;
        public NetworkId ControlledNetworkId;
        public bool IsHost;
        public bool IsConnected;

        public Participant(
            int playerId,
            int connectionId,
            NetworkId controlledNetworkId,
            bool isHost,
            bool isConnected)
        {
            PlayerId = playerId;
            ConnectionId = connectionId;
            ControlledNetworkId = controlledNetworkId;
            IsHost = isHost;
            IsConnected = isConnected;
        }
    }

    /// <summary>
    /// 참가자 목록 관리.
    /// </summary>
    public class ParticipantManager
    {
        private List<Participant> _participants = new();

        public void Add(Participant participant)
        {
            _participants.Add(participant);
        }

        public List<Participant> GetAll()
        {
            return _participants;
        }
    }
}