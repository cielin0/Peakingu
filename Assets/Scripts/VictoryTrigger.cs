using UnityEngine;

namespace Peakingu
{
    /// <summary>
    /// Componente que detecta cuando Pingu llega a la meta / cumbre de la montaña y dispara la pantalla de victoria.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class VictoryTrigger : MonoBehaviour
    {
        [Header("Configuración de Victoria")]
        [Tooltip("Clip de sonido opcional al cruzar la meta.")]
        [SerializeField] private AudioClip victorySfx;
        private bool hasTriggered = false;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (hasTriggered) return;

            if (other.CompareTag("Player"))
            {
                hasTriggered = true;
                Debug.Log("[VictoryTrigger] ¡Pingu ha llegado a la meta!");

                if (victorySfx != null)
                {
                    AudioSource.PlayClipAtPoint(victorySfx, transform.position);
                }

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TriggerVictory();
                }
                else if (PauseManager.Instance != null)
                {
                    PauseManager.Instance.ShowVictory();
                }
            }
        }
    }
}
