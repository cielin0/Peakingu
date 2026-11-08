using UnityEngine;
using UnityEngine.InputSystem;

namespace Peakingu
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movimiento Horizontal")]
        [Tooltip("Velocidad máxima horizontal.")]
        [SerializeField] private float moveSpeed = 8f;
        [Tooltip("Fuerza de aceleración al empezar a moverse.")]
        [SerializeField] private float acceleration = 60f;
        [Tooltip("Fuerza de desaceleración al soltar la tecla.")]
        [SerializeField] private float deceleration = 70f;

        [Header("Salto")]
        [Tooltip("Impulso inicial del salto.")]
        [SerializeField] private float jumpForce = 13f;
        [Tooltip("Multiplicador de gravedad al caer para un salto más ágil.")]
        [SerializeField] private float fallMultiplier = 2.5f;
        [Tooltip("Multiplicador al cortar el salto soltando Espacio.")]
        [SerializeField] private float jumpCutMultiplier = 0.5f;
        [Tooltip("Margen de tiempo para saltar tras salir de una plataforma (segundos).")]
        [SerializeField] private float coyoteTime = 0.15f;
        [Tooltip("Margen de tiempo que recuerda el input de salto antes de tocar suelo (segundos).")]
        [SerializeField] private float jumpBufferTime = 0.15f;

        [Header("Dash (Modificable por mejoras como Hidrodash)")]
        [Tooltip("Velocidad del impulso horizontal durante el dash.")]
        public float dashSpeed = 18f;
        [Tooltip("Duración en segundos del dash.")]
        public float dashDuration = 0.2f;
        [Tooltip("Tiempo de enfriamiento (cooldown) en segundos antes de volver a dashear.")]
        public float dashCooldown = 1.0f;
        [Tooltip("Habilita o deshabilita la capacidad de hacer dash.")]
        public bool canDash = true;

        [Header("Detección de Suelo")]
        [Tooltip("Capa que define el suelo sólido.")]
        [SerializeField] private LayerMask groundLayer;
        [Tooltip("Desplazamiento local de la caja de comprobación de suelo.")]
        [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.55f);
        [Tooltip("Tamaño de la caja de comprobación de suelo.")]
        [SerializeField] private Vector2 groundCheckSize = new Vector2(0.8f, 0.15f);

        [Header("Visuales")]
        [Tooltip("SpriteRenderer del personaje para alternar la orientación (flip).")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Input Actions (Opcionales)")]
        [Tooltip("Acción de movimiento (WASD/stick). Si no se asigna, lee teclado directamente.")]
        [SerializeField] private InputActionReference moveAction;
        [Tooltip("Acción de salto (Espacio/botón sur). Si no se asigna, lee teclado directamente.")]
        [SerializeField] private InputActionReference jumpAction;
        [Tooltip("Acción de dash (F/botón oeste). Si no se asigna, lee teclado directamente.")]
        [SerializeField] private InputActionReference dashAction;

        // Propiedades públicas de estado
        public bool IsDashing => isDashing;
        public bool IsFacingRight => isFacingRight;

        // Referencias internas
        private Rigidbody2D rb;
        private BoxCollider2D boxCollider;

        // Variables de movimiento y salto
        private float horizontalInput;
        private bool isGrounded;
        private float coyoteTimeCounter;
        private float jumpBufferCounter;
        private bool isJumping;
        private bool jumpHeld;
        private bool isFacingRight = true;

        // Variables de Dash
        private bool isDashing;
        private float dashTimeLeft;
        private float dashCooldownTimer;
        private float dashDirection;
        private float defaultGravityScale;

        private void Reset()
        {
            groundLayer = LayerMask.GetMask("Ground");
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            boxCollider = GetComponent<BoxCollider2D>();
            defaultGravityScale = rb.gravityScale;

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (groundLayer == 0)
            {
                groundLayer = LayerMask.GetMask("Ground");
            }
        }

        private void OnEnable()
        {
            if (moveAction != null) moveAction.action.Enable();
            if (jumpAction != null) jumpAction.action.Enable();
            if (dashAction != null) dashAction.action.Enable();
        }

        private void OnDisable()
        {
            if (moveAction != null) moveAction.action.Disable();
            if (jumpAction != null) jumpAction.action.Disable();
            if (dashAction != null) dashAction.action.Disable();
        }

        private void Update()
        {
            HandleTimers();
            ReadInput();
            HandleGroundCheck();
            HandleFlip();
        }

        private void FixedUpdate()
        {
            if (isDashing)
            {
                ApplyDashMovement();
                return;
            }

            ApplyHorizontalMovement();
            ApplyJumpPhysics();
        }

        private void ReadInput()
        {
            // Lectura horizontal
            if (moveAction != null)
            {
                horizontalInput = moveAction.action.ReadValue<Vector2>().x;
            }
            else
            {
                horizontalInput = 0f;
                var keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontalInput -= 1f;
                    if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontalInput += 1f;
                }

                var gamepad = Gamepad.current;
                if (gamepad != null && Mathf.Abs(horizontalInput) < 0.01f)
                {
                    horizontalInput = gamepad.leftStick.x.ReadValue();
                }
            }

            // Lectura de Dash (Tecla F o botón gamepad)
            bool dashPressedThisFrame = false;
            if (dashAction != null)
            {
                dashPressedThisFrame = dashAction.action.WasPressedThisFrame();
            }
            else
            {
                var keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    dashPressedThisFrame = keyboard.fKey.wasPressedThisFrame;
                }

                var gamepad = Gamepad.current;
                if (gamepad != null)
                {
                    dashPressedThisFrame |= gamepad.buttonWest.wasPressedThisFrame || gamepad.rightTrigger.wasPressedThisFrame;
                }
            }

            if (dashPressedThisFrame && canDash && !isDashing && dashCooldownTimer <= 0f)
            {
                StartDash();
                return;
            }

            // Lectura de Salto (solo si no está dasheando)
            if (!isDashing)
            {
                bool jumpPressedThisFrame = false;
                bool jumpReleasedThisFrame = false;

                if (jumpAction != null)
                {
                    jumpPressedThisFrame = jumpAction.action.WasPressedThisFrame();
                    jumpReleasedThisFrame = jumpAction.action.WasReleasedThisFrame();
                    jumpHeld = jumpAction.action.IsPressed();
                }
                else
                {
                    var keyboard = Keyboard.current;
                    if (keyboard != null)
                    {
                        jumpPressedThisFrame = keyboard.spaceKey.wasPressedThisFrame;
                        jumpReleasedThisFrame = keyboard.spaceKey.wasReleasedThisFrame;
                        jumpHeld = keyboard.spaceKey.isPressed;
                    }

                    var gamepad = Gamepad.current;
                    if (gamepad != null)
                    {
                        jumpPressedThisFrame |= gamepad.buttonSouth.wasPressedThisFrame;
                        jumpReleasedThisFrame |= gamepad.buttonSouth.wasReleasedThisFrame;
                        jumpHeld |= gamepad.buttonSouth.isPressed;
                    }
                }

                if (jumpPressedThisFrame)
                {
                    jumpBufferCounter = jumpBufferTime;
                }

                // Salto de altura variable: soltar el botón corta velocidad ascendente
                if (jumpReleasedThisFrame && rb.linearVelocity.y > 0f)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
                    coyoteTimeCounter = 0f;
                }
            }
        }

        private void StartDash()
        {
            isDashing = true;
            dashTimeLeft = dashDuration;
            dashCooldownTimer = dashCooldown;

            // Determina dirección: tecla activa o hacia donde mira el personaje
            if (Mathf.Abs(horizontalInput) > 0.05f)
            {
                dashDirection = Mathf.Sign(horizontalInput);
            }
            else
            {
                dashDirection = isFacingRight ? 1f : -1f;
            }

            // Desactiva gravedad durante el dash
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(dashDirection * dashSpeed, 0f);
        }

        private void ApplyDashMovement()
        {
            // Mantiene impulso horizontal puro con gravedad 0
            rb.linearVelocity = new Vector2(dashDirection * dashSpeed, 0f);

            dashTimeLeft -= Time.fixedDeltaTime;
            if (dashTimeLeft <= 0f)
            {
                StopDash();
            }
        }

        private void StopDash()
        {
            isDashing = false;
            rb.gravityScale = defaultGravityScale;
            // Deja una velocidad residual controlada hacia la dirección de avance
            rb.linearVelocity = new Vector2(dashDirection * moveSpeed, 0f);
        }

        private void HandleGroundCheck()
        {
            Vector2 checkPosition = (Vector2)transform.position + groundCheckOffset;
            isGrounded = Physics2D.OverlapBox(checkPosition, groundCheckSize, 0f, groundLayer);

            if (isGrounded)
            {
                coyoteTimeCounter = coyoteTime;
            }
        }

        private void HandleTimers()
        {
            if (dashCooldownTimer > 0f)
            {
                dashCooldownTimer -= Time.deltaTime;
            }

            if (!isGrounded)
            {
                coyoteTimeCounter -= Time.deltaTime;
            }

            jumpBufferCounter -= Time.deltaTime;

            // Intento de salto cuando buffer y coyote son válidos (no durante dash)
            if (!isDashing && jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
            {
                isJumping = true;
                jumpBufferCounter = 0f;
                coyoteTimeCounter = 0f;
            }
        }

        private void ApplyHorizontalMovement()
        {
            float targetSpeed = horizontalInput * moveSpeed;
            float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;

            // API de Unity 6: linearVelocity
            float newVelocityX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accelRate * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(newVelocityX, rb.linearVelocity.y);
        }

        private void ApplyJumpPhysics()
        {
            if (isJumping)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                isJumping = false;
            }

            // Gravedad aumentada al caer para mayor agilidad
            if (rb.linearVelocity.y < 0f)
            {
                rb.linearVelocity += Vector2.up * (Physics2D.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime);
            }
        }

        private void HandleFlip()
        {
            if (horizontalInput > 0.05f)
            {
                isFacingRight = true;
                if (spriteRenderer != null)
                {
                    spriteRenderer.flipX = false;
                }
                else
                {
                    Vector3 scale = transform.localScale;
                    scale.x = Mathf.Abs(scale.x);
                    transform.localScale = scale;
                }
            }
            else if (horizontalInput < -0.05f)
            {
                isFacingRight = false;
                if (spriteRenderer != null)
                {
                    spriteRenderer.flipX = true;
                }
                else
                {
                    Vector3 scale = transform.localScale;
                    scale.x = -Mathf.Abs(scale.x);
                    transform.localScale = scale;
                }
            }
        }

        public void Respawn(Vector3 targetPosition)
        {
            isDashing = false;
            rb.gravityScale = defaultGravityScale;
            rb.linearVelocity = Vector2.zero;
            transform.position = targetPosition;
            coyoteTimeCounter = 0f;
            jumpBufferCounter = 0f;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Vector2 checkPosition = (Vector2)transform.position + groundCheckOffset;
            Gizmos.DrawWireCube(checkPosition, groundCheckSize);
        }
    }
}
