using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Componente reutilizable de movimiento basado en pathfinding A*.
/// Cualquier personaje (jugador, enemigo, NPC) puede usar este componente
/// para moverse por el mundo evitando obstáculos.
///
/// Responsabilidades:
/// - Solicitar caminos a AStarPathfinder cuando recibe un nuevo destino.
/// - Recorrer el camino suavemente usando Vector2.MoveTowards.
/// - Actualizar animaciones y flip del sprite según la dirección de movimiento.
/// - Notificar cuando el personaje llega al destino.
///
/// No sabe quién lo controla ni por qué se mueve.
/// El jugador lo controla por input. Los enemigos lo controlan por IA.
/// </summary>
public class MovementAgent : MonoBehaviour
{
    // ─── Configuración Inspector ──────────────────────────────────────────

    /// <summary>
    /// Velocidad de movimiento en unidades de Unity por segundo.
    /// </summary>
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 5f;

    /// <summary>
    /// LayerMask de obstáculos. Se pasa a SmoothPath para que el line-of-sight
    /// pueda detectar si hay obstáculos entre dos waypoints.
    /// Debe coincidir con la obstacleMask de PathfindingGrid.
    /// </summary>
    [SerializeField] private LayerMask obstacleMask;

    /// <summary>
    /// Distancia mínima al waypoint actual para considerarlo alcanzado
    /// y avanzar al siguiente. Valor pequeño para movimiento preciso.
    /// </summary>
    [SerializeField] private float waypointReachedThreshold = 0.05f;

    // ─── Referencias Opcionales ───────────────────────────────────────────

    /// <summary>
    /// Referencia opcional al Animator para controlar las transiciones Idle/Walk.
    /// Si es null, el agente se mueve sin actualizar animaciones.
    /// Permite usar MovementAgent en objetos sin sprite (triggers, puntos de patrulla).
    /// </summary>
    [Header("Visuals (Opcionales)")]
    [SerializeField] private Animator animator;

    /// <summary>
    /// Referencia opcional al SpriteRenderer para hacer flip horizontal
    /// según la dirección de movimiento.
    /// </summary>
    [SerializeField] private SpriteRenderer spriteRenderer;

    // ─── Estado Interno ───────────────────────────────────────────────────

    /// <summary>
    /// Lista de waypoints del camino actual calculado por A*.
    /// El agente avanza hacia _path[_currentWaypointIndex] cada frame.
    /// </summary>
    private List<Vector2> _path;

    /// <summary>
    /// Índice del waypoint actual dentro de _path hacia el que se está moviendo.
    /// </summary>
    private int _currentWaypointIndex;

    /// <summary>
    /// Dirección de movimiento del frame actual en espacio 2D.
    /// Se usa para actualizar las animaciones y el flip del sprite.
    /// </summary>
    private Vector2 _moveDirection;

    // ─── Propiedades Públicas ─────────────────────────────────────────────

    /// <summary>
    /// True si el agente tiene un camino activo y está moviéndose hacia el destino.
    /// PlayerCombat y EnemyAI consultan esta propiedad para saber el estado del agente.
    /// </summary>
    public bool IsMoving { get; private set; }

    /// <summary>
    /// Evento que se dispara cuando el agente llega al último waypoint del camino.
    /// EnemyAI se suscribe para saber cuándo ejecutar el ataque al llegar al rango.
    /// </summary>
    public event System.Action OnDestinationReached;

    /// <summary>
    /// Velocidad de movimiento. PlayerMovement la usa para el ajuste
    /// directo de last mile con la misma velocidad del agente.
    /// </summary>
    public float MoveSpeed => moveSpeed;

    // ────────────────────────────────────────────────────────────────────
    // Unity Lifecycle
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Cada frame: si hay un camino activo, avanza hacia el waypoint actual.
    /// Cuando alcanza el waypoint, avanza al siguiente.
    /// Cuando alcanza el último waypoint, detiene el movimiento y notifica.
    /// </summary>
    private void Update()
    {
        if (!IsMoving || _path == null || _path.Count == 0)
        {
            UpdateAnimator(Vector2.zero);
            return;
        }

        FollowPath();
    }

    // ────────────────────────────────────────────────────────────────────
    // API Pública
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Solicita un nuevo camino hacia el destino dado y comienza a recorrerlo.
    /// Si el agente ya se estaba moviendo, cancela el camino anterior.
    /// Si no hay camino posible hacia el destino, el agente no se mueve.
    /// </summary>
    /// <param name="destination">Posición de destino en coordenadas del mundo 2D.</param>
    public void SetDestination(Vector2 destination)
    {
        List<Vector2> newPath = AStarPathfinder.Instance.FindPath(transform.position, destination);
        Debug.Log($"FindPath retornó {newPath.Count} waypoints hacia {destination}");
        // Si no hay camino posible, no hacer nada
        if (newPath.Count == 0)
        {
            StopMovement();
            return;
        }

        // Aplicar suavizado para eliminar waypoints redundantes
        _path = AStarPathfinder.Instance.SmoothPath(newPath, obstacleMask);
        Debug.Log($"SmoothPath redujo a {_path.Count} waypoints");
        _path[0] = (Vector2)transform.position;
        if (AStarPathfinder.Instance.IsPositionWalkable(destination))
            _path[_path.Count - 1] = destination;
        _currentWaypointIndex = 0;
        IsMoving = true;
    }

    /// <summary>
    /// Detiene el movimiento inmediatamente y limpia el camino activo.
    /// Se llama cuando el jugador hace click en otro lugar, cuando el target muere,
    /// o cuando el enemigo entra en rango de ataque.
    /// </summary>
    public void StopMovement()
    {
        IsMoving = false;
        _path = null;
        _moveDirection = Vector2.zero;
        UpdateAnimator(Vector2.zero);
    }

    // ────────────────────────────────────────────────────────────────────
    // Movimiento
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Avanza hacia el waypoint actual del camino.
    /// Cuando lo alcanza, avanza al índice siguiente.
    /// Cuando alcanza el último waypoint, detiene el movimiento y dispara OnDestinationReached.
    /// </summary>
    private void FollowPath()
    {
        Vector2 currentPos = transform.position;
        Vector2 targetWaypoint = _path[_currentWaypointIndex];

        // DEBUG: dibujar el camino completo en la Scene view
        for (int i = _currentWaypointIndex; i < _path.Count - 1; i++)
            Debug.DrawLine(
                new Vector3(_path[i].x, _path[i].y, 0),
                new Vector3(_path[i + 1].x, _path[i + 1].y, 0),
                Color.yellow,
                10f
            );

        // Calcular dirección y actualizar visuals antes de moverse
        _moveDirection = (targetWaypoint - currentPos).normalized;
        UpdateAnimator(_moveDirection);
        UpdateFlip(_moveDirection);

        // Mover hacia el waypoint actual
        transform.position = Vector2.MoveTowards(
            currentPos,
            targetWaypoint,
            moveSpeed * Time.deltaTime
        );

        // Verificar si alcanzamos el waypoint actual
        float distanceToWaypoint = Vector2.Distance(transform.position, targetWaypoint);
        if (distanceToWaypoint <= waypointReachedThreshold)
        {
            _currentWaypointIndex++;

            // Si no hay más waypoints, llegamos al destino final
            if (_currentWaypointIndex >= _path.Count)
            {
                StopMovement();
                OnDestinationReached?.Invoke();
            }
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // Visuals
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Actualiza el parámetro Speed del Animator para controlar la transición
    /// entre las animaciones Idle y Walk del Blend Tree.
    /// Si no hay Animator asignado, no hace nada (el componente es opcional).
    /// </summary>
    /// <param name="direction">Dirección de movimiento actual. Vector2.zero cuando está quieto.</param>
    private void UpdateAnimator(Vector2 direction)
    {
        if (animator == null)
            return;

        animator.SetFloat("Speed", direction.magnitude);
    }

    /// <summary>
    /// Hace flip horizontal del sprite según la dirección de movimiento.
    /// flipX = true cuando se mueve a la izquierda (direction.x negativo).
    /// flipX = false cuando se mueve a la derecha (direction.x positivo).
    /// No cambia el flip cuando el movimiento es puramente vertical,
    /// para mantener la última orientación horizontal.
    /// Si no hay SpriteRenderer asignado, no hace nada.
    /// </summary>
    /// <param name="direction">Dirección de movimiento actual.</param>
    private void UpdateFlip(Vector2 direction)
    {
        if (spriteRenderer == null)
            return;

        if (direction.x > 0.1f)
            spriteRenderer.flipX = false;
        else if (direction.x < -0.1f)
            spriteRenderer.flipX = true;
    }
}