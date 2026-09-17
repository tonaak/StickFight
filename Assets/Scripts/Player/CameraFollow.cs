using UnityEngine;

namespace StickFight.Player
{
    /// <summary>
    /// Smoothly follows a target transform on the X/Y plane using SmoothDamp,
    /// keeping the camera's own Z position (2D setup).
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float smoothTime = 0.15f;
        [SerializeField] private Vector2 offset = Vector2.zero;

        private Vector3 velocity;

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 targetPosition = new Vector3(
                target.position.x + offset.x,
                target.position.y + offset.y,
                transform.position.z);

            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        }
    }
}
