using UnityEngine;

namespace Peakingu
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class Checkpoint : MonoBehaviour
    {
        [Header("Configuración")]
        [Tooltip("Punto exacto de reaparición. Si es nulo, se usa la posición del Checkpoint.")]
        [SerializeField] private Transform respawnPoint;

        [Header("Visuales")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color inactiveColor = new Color(0.85f, 0.75f, 0.2f); // Dorado inactivo
        [SerializeField] private Color activeColor = new Color(0.2f, 1f, 0.4f);       // Verde activo

        private bool isActivated;

        private void Reset()
        {
            var col = GetComponent<BoxCollider2D>();
            if (col != null) col.isTrigger = true;
            gameObject.tag = "Checkpoint";
            gameObject.layer = LayerMask.NameToLayer("Checkpoint");
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void Awake()
        {
            var col = GetComponent<BoxCollider2D>();
            if (col != null) col.isTrigger = true;

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            UpdateVisuals();
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (isActivated) return;

            if (collision.CompareTag("Player"))
            {
                ActivateCheckpoint();
            }
        }

        public void ActivateCheckpoint()
        {
            isActivated = true;
            Vector3 spawnPos = (respawnPoint != null) ? respawnPoint.position : transform.position;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetCheckpoint(spawnPos);
            }

            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = isActivated ? activeColor : inactiveColor;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = isActivated ? Color.green : Color.yellow;
            Vector3 pos = (respawnPoint != null) ? respawnPoint.position : transform.position;
            Gizmos.DrawWireSphere(pos, 0.4f);
        }
    }
}
