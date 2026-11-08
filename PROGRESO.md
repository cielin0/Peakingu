# PROGRESO - Peakingu

## 1. Ya implementado
- Inicialización y verificación del proyecto base en Unity 6 (6000.5.10f1) con plantilla Universal 2D (URP).
- Estructura de carpetas completa en `Assets/`.
- Escenas operativas (`Menu.unity`, `Nivel1.unity`, `Cueva.unity`) en Build Settings.
- Tags, Layers y Sorting Layers configurados con matriz de física 2D.
- **Iluminación URP 2D (Corrección de pantalla oscura / siluetas negras)**:
  - `Global Light 2D` actualizada en todas las escenas para iluminar todas las Sorting Layers del proyecto (`Default`, `Background`, `Environment`, `Collectibles`, `Characters`, `Foreground`).
  - Colores y tonalidades de sprites y tilemaps 100% visibles (Pingu en celeste cyborg, suelo y plataformas en verde, checkpoint en dorado/verde y pinchos en rojo).
- **PlayerController (`Assets/Scripts/PlayerController.cs`)**:
  - Movimiento horizontal responsivo en Unity 6 (`linearVelocity`).
  - Salto variable con Espacio, Ground Check rectangular (`Physics2D.OverlapBox`), Coyote Time, Jump Buffer, Fall Multiplier y Sprite Flip.
  - Dash horizontal con tecla F (gravedad desactivada durante el dash, duración y cooldown configurables, variables públicas para mejora Hidrodash).
  - Método `Respawn(Vector3)` que resetea velocidades, estado de dash y recoloca a Pingu.
- **GameManager (`Assets/Scripts/GameManager.cs`)**:
  - Singleton persistente entre escenas (`DontDestroyOnLoad`).
  - Contador de 5 vidas configurables (`maxLives = 5`, `currentLives = 5`).
  - Método público `TakeDamage(int damage = 1)` que descuenta vidas, dispara `OnLivesChanged`, genera respawn o dispara el evento `OnGameOver` al llegar a 0.
  - Gestión y almacenamiento de la posición del último Checkpoint activo.
- **KillZone (`Assets/Scripts/KillZone.cs`)**:
  - Trigger ubicado bajo el nivel (Y = -7) que detecta a Pingu, resta 1 vida y lo reaparece en el último checkpoint.
- **Checkpoint (`Assets/Scripts/Checkpoint.cs`)**:
  - Trigger en Layer y Tag `Checkpoint` que registra su posición en el `GameManager` al contacto con el jugador.
  - Retroalimentación visual interactiva (cambia de color dorado a verde al activarse).
- **Hazard (`Assets/Scripts/Hazard.cs`)**:
  - Componente reutilizable para pinchos y obstáculos que llama a `TakeDamage()` por trigger o colisión física.
- **Prefabs generados**:
  - `Assets/Prefabs/Pingu.prefab`
  - `Assets/Prefabs/GameManager.prefab`
  - `Assets/Prefabs/Checkpoint.prefab`
- **Tilemap y Cinemachine 2D en Nivel 1**:
  - Suelo continuo con `CompositeCollider2D`.
  - Cámara de seguimiento con Dead Zone, Look-ahead vertical y Confiner 2D.

## 2. Decisiones tomadas
- **Iluminación 2D en URP:** En el renderizador 2D de URP, los materiales de sprite por defecto (`Sprite-Lit-Default`) requieren explícitamente que la luz 2D tenga marcadas las Sorting Layers donde se ubican los objetos. Se incluyeron todas las capas del proyecto (`Default`, `Background`, `Environment`, `Collectibles`, `Characters`, `Foreground`) en el campo `targetSortingLayers` de la `Global Light 2D`.

## 3. Pendiente
- Configuración de Input Actions (WASD, Espacio, E, F, R, ESC) con el nuevo Input System.
- Temporizador configurable de 20 minutos (pescado quemándose).
- Mecánica de gancho (tecla R).
- Sistema de monedas y tienda/mercader interactiva (tecla E).
