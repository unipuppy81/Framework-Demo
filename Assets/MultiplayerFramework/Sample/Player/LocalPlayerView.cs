using UnityEngine;

namespace MultiplayerFramework.Runtime.Sample.Player
{
    public sealed class LocalPlayerView : MonoBehaviour
    {
        [SerializeField] private LocalPlayerController controller;
        [SerializeField] private bool rotateToFacing = true;
        [SerializeField] private float rotationLerpSpeed = 20f;

        private void LateUpdate()
        {
            if (controller == null)
                return;

            PlayerState state = controller.CurrentState;
            transform.position = state.Position;

            if (!rotateToFacing)
                return;

            Vector3 facing = state.Facing;
            facing.y = 0f;

            if (facing.sqrMagnitude <= 0.0001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(facing.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationLerpSpeed * Time.deltaTime);
        }
    }
}