using UnityEngine;

namespace MultiplayerFramework.Runtime.Gameplay.Spawn
{
    public class SpawnPoint : MonoBehaviour
    {
        [SerializeField] private int teamId = -1;

        public int TeamId => teamId;
        public Vector3 Position => transform.position;
        public Quaternion Rotation => transform.rotation;
    }
}