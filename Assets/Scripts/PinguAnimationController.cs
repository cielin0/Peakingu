using UnityEngine;

namespace Peakingu
{
    /// <summary>
    /// Controlador de Animaciones para Pingu.
    /// Sincroniza en cada frame las variables físicas y de estado de PlayerController con el Animator:
    ///  - Speed (Float): Magnitud de movimiento horizontal para alternar entre Idle y Run.
    ///  - IsGrounded (Bool): Estado de contacto con el suelo.
    ///  - VerticalVelocity (Float): Velocidad en Y para diferenciar salto ascendente (Jump) y caída (Fall).
    ///  - IsDashing (Bool): Activa la animación especial de dash/torpedo cyborg.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(Animator))]
    public class PinguAnimationController : MonoBehaviour
    {
        [Header("── Referencias ──")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;

        // Hash IDs de parámetros del Animator para optimización máxima de rendimiento
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int VerticalVelocityHash = Animator.StringToHash("VerticalVelocity");
        private static readonly int IsDashingHash = Animator.StringToHash("IsDashing");

        private void Reset()
        {
            playerController = GetComponent<PlayerController>();
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void Awake()
        {
            if (playerController == null) playerController = GetComponent<PlayerController>();
            if (animator == null) animator = GetComponent<Animator>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void Update()
        {
            if (playerController == null || animator == null) return;

            UpdateAnimatorParameters();
        }

        /// <summary>
        /// Actualiza todos los parámetros del Animator en base al estado actual de PlayerController.
        /// </summary>
        private void UpdateAnimatorParameters()
        {
            // 1. Velocidad horizontal para alternar entre Idle y Run
            // Se calcula usando el valor absoluto de la entrada horizontal o la velocidad física
            float horizontalSpeed = Mathf.Max(
                Mathf.Abs(playerController.HorizontalInput),
                Mathf.Abs(playerController.Velocity.x) / 8.0f
            );
            animator.SetFloat(SpeedHash, horizontalSpeed);

            // 2. Estado de suelo (IsGrounded)
            animator.SetBool(IsGroundedHash, playerController.IsGrounded);

            // 3. Velocidad vertical (para Jump vs Fall)
            animator.SetFloat(VerticalVelocityHash, playerController.Velocity.y);

            // 4. Dash
            animator.SetBool(IsDashingHash, playerController.IsDashing);
        }
    }
}
