using UnityEngine;

namespace Peakingu
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class KillZone : MonoBehaviour
    {
        [Header("Configuración")]
        [Tooltip("Daño infligido al jugador al entrar en la KillZone.")]
        [SerializeField] private int damage = 1;

        private void Reset()
        {
            var col = GetComponent<BoxCollider2D>();
            if (col != null) col.isTrigger = true;
            gameObject.tag = "Hazard";
            gameObject.layer = LayerMask.NameToLayer("Hazard");
        }

        private void Awake()
        {
            var col = GetComponent<BoxCollider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TakeDamage(damage);
                }
                else
                {
                    var controller = collision.GetComponent<PlayerController>();
                    if (controller != null)
                    {
                        controller.Respawn(Vector3.zero);
                    }
                }
            }
        }
    }
}
