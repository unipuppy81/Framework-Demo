using UnityEngine;
using MultiplayerFramework.Runtime.Gameplay.Combat;
using MultiplayerFramework.Runtime.Gameplay.Respawn;

namespace MultiplayerFramework.Runtime.Gameplay.Match
{
    public class PlayerMatchParticipant : MonoBehaviour
    {
        [SerializeField] private int playerId;

        private Health _health;
        private RespawnController _respawn;
        private SampleMatchController _match;

        public int PlayerId => playerId;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _respawn = GetComponent<RespawnController>();
        }

        private void OnEnable()
        {
            if (_health != null)
                _health.OnDied += HandleDied;

            if (_respawn != null)
                _respawn.OnRespawnCompleted += HandleRespawnCompleted;
        }

        private void OnDisable()
        {
            if (_health != null)
                _health.OnDied -= HandleDied;

            if (_respawn != null)
                _respawn.OnRespawnCompleted -= HandleRespawnCompleted;
        }

        public void BindMatch(SampleMatchController match)
        {
            _match = match;
        }

        public void Respawn(Vector3 position, Quaternion rotation)
        {
            _respawn.CompleteRespawn(position, rotation);
        }

        private void HandleDied(DamageInfo damageInfo)
        {
            if (_match == null)
                return;

            if (damageInfo.InstigatorId != playerId)
                _match.ReportKill(damageInfo.InstigatorId, this);
        }

        private void HandleRespawnCompleted()
        {
            if (_match == null)
                return;

            _match.HandleRespawnReady(this);
        }
    }
}