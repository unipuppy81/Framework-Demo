using System.Collections.Generic;
using UnityEngine;
using MultiplayerFramework.Runtime.Gameplay.Spawn;
using MultiplayerFramework.Runtime.Gameplay.Respawn;
using MultiplayerFramework.Runtime.Gameplay.Combat;

namespace MultiplayerFramework.Runtime.Gameplay.Match
{
    public class SampleMatchController : MonoBehaviour
    {
        [SerializeField] private int targetScore = 3;
        [SerializeField] private List<SpawnPoint> spawnPoints = new();

        private readonly Dictionary<int, int> _scores = new();
        private readonly List<PlayerMatchParticipant> _participants = new();

        private MatchState _state = MatchState.None;

        public MatchState State => _state;

        private void Awake()
        {
            PlayerMatchParticipant[] foundParticipants = FindObjectsByType<PlayerMatchParticipant>(FindObjectsSortMode.None);

            for (int i = 0; i < foundParticipants.Length; i++)
            {
                Register(foundParticipants[i]);
            }
        }
        private void Start()
        {
            BeginMatch();
        }


        public void Register(PlayerMatchParticipant participant)
        {
            if (_participants.Contains(participant))
                return;

            _participants.Add(participant);

            if (!_scores.ContainsKey(participant.PlayerId))
                _scores.Add(participant.PlayerId, 0);

            participant.BindMatch(this);
        }

        public void BeginMatch()
        {
            _state = MatchState.Running;

            for (int i = 0; i < _participants.Count; i++)
            {
                RespawnParticipant(_participants[i]);
            }
        }

        public void ReportKill(int killerPlayerId, PlayerMatchParticipant victim)
        {
            if (_state != MatchState.Running)
                return;

            if (_scores.ContainsKey(killerPlayerId))
                _scores[killerPlayerId]++;

            if (_scores[killerPlayerId] >= targetScore)
            {
                _state = MatchState.Finished;
                Debug.Log($"[Match] Winner PlayerId={killerPlayerId}");
                return;
            }

            Debug.Log($"[Match] Score {killerPlayerId} = {_scores[killerPlayerId]}");
        }

        public void HandleRespawnReady(PlayerMatchParticipant participant)
        {
            if (_state != MatchState.Running)
                return;

            RespawnParticipant(participant);
        }

        private void RespawnParticipant(PlayerMatchParticipant participant)
        {
            if (spawnPoints.Count == 0)
            {
                Debug.LogError("[Match] No spawn points.");
                return;
            }

            int index = Mathf.Abs(participant.PlayerId) % spawnPoints.Count;
            SpawnPoint point = spawnPoints[index];

            participant.Respawn(point.Position, point.Rotation);
        }
    }
}