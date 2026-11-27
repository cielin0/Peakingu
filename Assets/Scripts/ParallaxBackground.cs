using UnityEngine;

namespace Peakingu
{
    /// <summary>
    /// Hace que un fondo siga a la cámara con efecto parallax en X e Y.
    /// Se ejecuta en LateUpdate para evitar vibraciones respecto a Cinemachine.
    /// </summary>
    public class ParallaxBackground : MonoBehaviour
    {
        [Header("── Referencias ──")]
        [Tooltip("Transform de la cámara principal. Si se deja vacío, usa Camera.main.")]
        [SerializeField] private Transform cameraTransform;

        [Header("── Factores de Parallax ──")]
        [Tooltip("0 = estático en el mundo, 1 = pegado a la cámara. Valores bajos (ej. 0.15) dan sensación de lejanía.")]
        [Range(0f, 1f)]
        [SerializeField] private float parallaxFactorX = 0.15f;

        [Tooltip("Factor de seguimiento vertical. En niveles muy altos, un valor alto (ej. 0.85) mantiene el fondo visible durante todo el ascenso.")]
        [Range(0f, 1f)]
        [SerializeField] private float parallaxFactorY = 0.85f;

        private Vector3 startPosition;
        private Vector3 cameraStartPosition;
        private bool initialized = false;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            if (cameraTransform != null)
            {
                startPosition = transform.position;
                cameraStartPosition = cameraTransform.position;
                initialized = true;
            }
        }

        private void LateUpdate()
        {
            if (!initialized)
            {
                Initialize();
                if (!initialized) return;
            }

            if (cameraTransform == null) return;

            Vector3 camDelta = cameraTransform.position - cameraStartPosition;
            transform.position = new Vector3(
                startPosition.x + camDelta.x * parallaxFactorX,
                startPosition.y + camDelta.y * parallaxFactorY,
                startPosition.z
            );
        }

#if UNITY_EDITOR
        public void Configure(float factorX, float factorY, Transform cam = null)
        {
            parallaxFactorX = Mathf.Clamp01(factorX);
            parallaxFactorY = Mathf.Clamp01(factorY);
            cameraTransform = cam;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
