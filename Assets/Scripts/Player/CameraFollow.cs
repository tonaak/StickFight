using System.Collections;
using UnityEngine;

namespace StickFight.Player
{
    /// <summary>
    /// Smoothly follows a target transform on the X/Y plane using SmoothDamp,
    /// keeping the camera's own Z position (2D setup). Supports a screen shake
    /// that offsets the final position without disturbing the follow smoothing.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float smoothTime = 0.15f;
        [SerializeField] private Vector2 offset = Vector2.zero;

        private Vector3 followVelocity;
        private Vector3 smoothedPosition;
        private Vector3 shakeOffset;
        private Coroutine shakeCoroutine;

        private void Awake()
        {
            smoothedPosition = transform.position;
        }

        private void LateUpdate()
        {
            if (target != null)
            {
                Vector3 targetPosition = new Vector3(
                    target.position.x + offset.x,
                    target.position.y + offset.y,
                    smoothedPosition.z);

                smoothedPosition = Vector3.SmoothDamp(smoothedPosition, targetPosition, ref followVelocity, smoothTime);
            }

            transform.position = smoothedPosition + shakeOffset;
        }

        // Randomly offsets the camera around its followed position for the given duration.
        // Defaults to a light micro-shake suited to fast, frequent hits.
        public void Shake(float duration = 0.04f, float magnitude = 0.05f)
        {
            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
            }
            shakeCoroutine = StartCoroutine(ShakeRoutine(duration, magnitude));
        }

        private IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                shakeOffset = (Vector3)Random.insideUnitCircle * magnitude;
                elapsed += Time.deltaTime;
                yield return null;
            }
            shakeOffset = Vector3.zero;
            shakeCoroutine = null;
        }
    }
}
