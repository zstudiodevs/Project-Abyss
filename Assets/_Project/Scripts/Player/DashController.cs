using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Maneja el dash del jugador: movimiento rápido en una dirección con
/// invulnerabilidad durante toda su duración.
/// Lee PendingDash de PlayerMovement cada frame y ejecuta el dash
/// cuando las condiciones se cumplen.
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(MovementAgent))]
public class DashController : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Configuración — editable desde el Inspector
    // -------------------------------------------------------------------------

    [Header("Dash")]

    /// <summary>Distancia total recorrida durante el dash en unidades de mundo.</summary>
    [SerializeField] private float _dashDistance = 3f;

    /// <summary>Duración del dash en segundos. Controla la velocidad implícita.</summary>
    [SerializeField] private float _dashDuration = 0.2f;

    /// <summary>Cooldown entre dashes en segundos.</summary>
    [SerializeField] private float _dashCooldown = 1f;

    // -------------------------------------------------------------------------
    // Referencias internas
    // -------------------------------------------------------------------------

    /// <summary>Fuente de verdad del destino y flags de input.</summary>
    private PlayerMovement _playerMovement;

    /// <summary>Para activar la invulnerabilidad durante el dash.</summary>
    private PlayerHealth _playerHealth;

    /// <summary>Para detener y retomar el movimiento antes y después del dash.</summary>
    private MovementAgent _movementAgent;

    // -------------------------------------------------------------------------
    // Estado interno
    // -------------------------------------------------------------------------

    /// <summary>True mientras el dash está en ejecución. Bloquea nuevos dashes.</summary>
    private bool _isDashing;

    /// <summary>Timestamp del último dash para calcular el cooldown.</summary>
    private float _lastDashTime = -999f;

    /// <summary>
    /// Indica si el jugador estaba moviéndose antes del dash.
    /// Si es true, retomará el movimiento al destino original al terminar.
    /// </summary>
    private bool _wasMovingBeforeDash;

    // -------------------------------------------------------------------------
    // Propiedades Públicas
    // -------------------------------------------------------------------------

    /// <summary>
    /// True mientras el dash está en ejecución.
    /// Puede usarse para bloquear otros sistemas durante el dash.
    /// </summary>
    public bool IsDashing => _isDashing;

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    /// <summary>Obtiene referencias a los componentes necesarios.</summary>
    private void Awake()
    {
        _playerMovement = GetComponent<PlayerMovement>();
        _playerHealth = GetComponent<PlayerHealth>();
        _movementAgent = GetComponent<MovementAgent>();
    }

    /// <summary>
    /// Evalúa cada frame si se cumplen las condiciones para ejecutar un dash.
    /// </summary>
    private void Update()
    {
        HandleDashInput();
    }

    // -------------------------------------------------------------------------
    // Lógica de Dash
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifica si el jugador presionó Space este frame y si las condiciones
    /// permiten ejecutar un dash. Lee el input directamente para evitar
    /// dependencias de orden de ejecución con PlayerMovement.
    /// </summary>
    private void HandleDashInput()
    {
        if (!Keyboard.current.spaceKey.wasPressedThisFrame) return;
        if (_isDashing) return;
        if (Time.time - _lastDashTime < _dashCooldown) return;

        Vector2 dashDirection = GetDashDirection();
        StartCoroutine(ExecuteDash(dashDirection));
    }

    /// <summary>
    /// Determina la dirección del dash según el estado actual del jugador.
    /// Si se está moviendo usa la dirección del movimiento actual.
    /// Si está quieto usa la dirección hacia el cursor.
    /// </summary>
    /// <returns>Vector2 normalizado con la dirección del dash.</returns>
    private Vector2 GetDashDirection()
    {
        // Si el agente se está moviendo, dash en la dirección actual
        if (_movementAgent.IsMoving)
        {
            Vector2 toDestination = _playerMovement.CurrentDestination - (Vector2)transform.position;

            // Si el destino está muy cerca no hay dirección útil, usar cursor como fallback
            if (toDestination.magnitude > 0.1f)
                return toDestination.normalized;
        }

        // Quieto: dash hacia el cursor
        return GetDirectionToCursor();
    }

    /// <summary>
    /// Calcula el vector normalizado desde el jugador hacia la posición del cursor.
    /// </summary>
    /// <returns>Vector2 normalizado hacia el cursor, o Vector2.right como fallback.</returns>
    private Vector2 GetDirectionToCursor()
    {
        Vector2 mouseScreenPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        Vector2 direction = mouseWorldPos - (Vector2)transform.position;

        return direction.magnitude > 0.01f ? direction.normalized : Vector2.right;
    }

    /// <summary>
    /// Ejecuta el dash completo: interrumpe el movimiento, activa la
    /// invulnerabilidad, desplaza al jugador y retoma el movimiento anterior.
    /// </summary>
    /// <param name="direction">Dirección normalizada del dash.</param>
    private IEnumerator ExecuteDash(Vector2 direction)
    {
        _isDashing = true;
        _lastDashTime = Time.time;

        // Guardar si se estaba moviendo para retomar después del dash
        _wasMovingBeforeDash = _movementAgent.IsMoving;

        // Interrumpir el movimiento del agente durante el dash
        _movementAgent.StopMovement();

        // Activar invulnerabilidad durante toda la duración del dash
        _playerHealth.ActivateInvulnerability();

        // Calcular posición destino del dash
        Vector2 startPosition = transform.position;
        Vector2 targetPosition = startPosition + direction * _dashDistance;

        // Desplazar al jugador suavemente durante _dashDuration segundos
        float elapsed = 0f;
        while (elapsed < _dashDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _dashDuration);

            transform.position = Vector2.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        // Asegurar que llegue exactamente al destino del dash
        transform.position = targetPosition;

        _isDashing = false;

        // Retomar el movimiento al destino original desde la nueva posición
        if (_wasMovingBeforeDash)
        {
            _movementAgent.SetDestination(_playerMovement.CurrentDestination);
        }
    }
}