using UnityEngine;

namespace MultiplayerFramework.Sample.Combat
{
    public sealed class SampleDamageable : MonoBehaviour
    {
        [SerializeField] private int maxHp = 30;

        private int _currentHp;

        private void Awake()
        {
            _currentHp = maxHp;
        }

        public void ApplyDamage(int damage)
        {
            _currentHp -= damage;
            Debug.LogError($"[{name}] Damage={damage}, HP={_currentHp}");

            if (_currentHp <= 0)
            {
                Destroy(gameObject);
            }
        }
    }
}