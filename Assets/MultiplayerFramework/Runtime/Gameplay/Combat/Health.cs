using System;
using UnityEngine;

namespace MultiplayerFramework.Runtime.Gameplay.Combat
{
    public class Health : MonoBehaviour
    {
        [SerializeField] private int maxHp = 100;
        [SerializeField] private int _currentHp;
        [SerializeField] private bool _isDead;

        public int MaxHp => maxHp;
        public int CurrentHp => _currentHp;
        public bool IsDead => _isDead;

        public event Action<int, int> OnHpChanged;
        public event Action<DamageInfo> OnDamaged;
        public event Action<DamageInfo> OnDied;

        private void Awake()
        {
            ResetHealth();
        }

        public void ResetHealth()
        {
            _currentHp = maxHp;
            _isDead = false;
            OnHpChanged?.Invoke(_currentHp, maxHp);
        }

        public bool ApplyDamage(DamageInfo damageInfo)
        {
            if (_isDead)
                return false;

            _currentHp -= damageInfo.Amount;
            if (_currentHp < 0)
                _currentHp = 0;

            OnDamaged?.Invoke(damageInfo);
            OnHpChanged?.Invoke(_currentHp, maxHp);

            if (_currentHp == 0)
            {
                _isDead = true;
                OnDied?.Invoke(damageInfo);
            }

            return true;
        }
    }
}