using UnityEngine;

namespace MultiplayerFramework.Runtime.Gameplay.Combat
{
    public class AttackResolver : MonoBehaviour
    {
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private int damage = 25;
        [SerializeField] private LayerMask targetMask;

        public bool TryResolveAttack(Transform attacker, int instigatorId, int tick)
        {
            Collider[] hits = Physics.OverlapSphere(attacker.position, attackRange, targetMask);

            bool applied = false;

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].transform == attacker)
                    continue;

                Health health = hits[i].GetComponentInParent<Health>();
                if (health == null || health.IsDead)
                    continue;

                applied |= health.ApplyDamage(new DamageInfo(damage, instigatorId, tick));
            }

            return applied;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
#endif
    }
}