using System;
using UnityEngine;
using MultiplayerFramework.Runtime.Gameplay.Combat;

namespace MultiplayerFramework.Runtime.Gameplay.Respawn
{
    public class RespawnController : MonoBehaviour
    {
        [SerializeField] private float respawnDelay = 3f;
        [SerializeField] private GameObject visualRoot;

        private Health _health;
        private float _remaining;
        private bool _respawning;

        public bool IsRespawning => _respawning;
        public float Remaining => _remaining;

        public event Action OnRespawnStarted;
        public event Action OnRespawnCompleted;

        private void Awake()
        {
            _health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            if (_health != null)
                _health.OnDied += HandleDied;
        }

        private void OnDisable()
        {
            if (_health != null)
                _health.OnDied -= HandleDied;
        }

        private void Update()
        {
            if (!_respawning)
                return;

            _remaining -= Time.deltaTime;
            if (_remaining > 0f)
                return;

            _respawning = false;
            OnRespawnCompleted?.Invoke();
        }

        private void HandleDied(DamageInfo damageInfo)
        {
            _respawning = true;
            _remaining = respawnDelay;

            if (visualRoot != null)
                visualRoot.SetActive(false);

            OnRespawnStarted?.Invoke();
        }

        public void CompleteRespawn(Vector3 position, Quaternion rotation)
        {
            Debug.Log("ComleteRespawn");
            transform.SetPositionAndRotation(position, rotation);

            if (_health != null)
                _health.ResetHealth();

            if (visualRoot != null)
                visualRoot.SetActive(true);
        }
    }
}