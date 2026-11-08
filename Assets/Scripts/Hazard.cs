using UnityEngine;

namespace Peakingu
{
    public class Hazard : MonoBehaviour
    {
        [Header("Configuración de Daño")]
        [Tooltip("Cantidad de vidas a restar al tocar este obstáculo.")]
        [SerializeField] private int damage = 1;

        private void Reset()
        {
            gameObject.tag = "Hazard";
            gameObject.layer = LayerMask.NameToLayer("Hazard");
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TakeDamage(damage);
                }
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.collider.CompareTag("Player"))
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TakeDamage(damage);
                }
            }
        }
    }
}
