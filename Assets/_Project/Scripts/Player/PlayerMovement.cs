using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Maneja el input del jugador y lo traduce en instrucciones para MovementAgent.
/// Implementa el sistema click-to-move definido en el GDD:
///
/// - Click izquierdo en terreno    → moverse al punto
/// - Click izquierdo en enemigo   → perseguir y atacar al llegar al rango
/// - Click derecho en enemigo     → perseguir y ejecutar ataque de área al llegar
/// - Ctrl + Click                 → moverse al punto ignorando enemigos
/// - Shift + Click                → atacar sin moverse (notifica a PlayerCombat)
///
/// No sabe cómo moverse. Delega esa responsabilidad a MovementAgent.
/// No sabe cómo atacar. Delega esa responsabilidad a PlayerCombat (Fase 2).
/// </summary>
[RequireComponent(typeof(MovementAgent))]
public class PlayerMovement : MonoBehaviour
{
    // ─── Configuración Inspector ──────────────────────────────────────────

    /// <summary>
    /// Distancia al target en la que el jugador se detiene y ataca.
    /// Debe coincidir con el alcance del ataque primario del GDD (3 unidades).
    /// </summary>
    [Header("Combate")]
    [SerializeField] private float attackRange = 3f;

    /// <summary>
    /// LayerMask del layer Enemy. El Raycast 2D usa este layer para detectar
    /// si el jugador clickeó sobre un enemigo.
    /// </summary>
    [Header("Detección")]
    [SerializeField] private LayerMask enemyLayer;

    /// <summary>
    /// Distancia mínima que debe moverse el target para que el jugador
    /// recalcule el camino de persecución. Evita recalcular cada frame.
    /// </summary>
    [SerializeField] private float chaseRecalculateThreshold = 0.2f;

    // ─── Referencias ──────────────────────────────────────────────────────

    /// <summary>
    /// Referencia al MovementAgent del jugador.
    /// Recibe las instrucciones de movimiento y ejecuta el pathfinding.
    /// </summary>
    private MovementAgent _agent;

    // ─── Estado del Target ────────────────────────────────────────────────

    /// <summary>
    /// Transform del enemigo actualmente seleccionado como objetivo.
    /// El jugador actualiza su destino hacia este objeto mientras lo persigue.
    /// Null si no hay target activo.
    /// </summary>
    private Transform _currentTarget;

    /// <summary>
    /// Componente EnemyHealth del target activo.
    /// Se usa para suscribirse al evento OnEnemyDied.
    /// </summary>
    private EnemyHealth _currentTargetHealth;

    /// <summary>
    /// Última posición conocida del target. Se usa para detectar si el target
    /// se movió lo suficiente como para recalcular el camino de persecución.
    /// </summary>
    private Vector2 _lastTargetPosition;

    // ─── Estado de Combate ────────────────────────────────────────────────

    /// <summary>
    /// True si el jugador está en estado CHASE persiguiendo un enemigo.
    /// </summary>
    private bool _isChasingEnemy;

    /// <summary>
    /// True si el próximo ataque al llegar al rango será el ataque de área.
    /// False = ataque primario (click izquierdo). True = ataque de área (click derecho).
    /// PlayerCombat consume este flag para decidir qué ataque ejecutar.
    /// </summary>
    private bool _pendingAreaAttack;

    // ─── Propiedades Públicas ─────────────────────────────────────────────

    /// <summary>
    /// True cuando el jugador está dentro del rango de ataque del target activo.
    /// PlayerCombat consulta esta propiedad cada frame para saber cuándo atacar.
    /// </summary>
    public bool IsInAttackRange { get; private set; }

    /// <summary>
    /// Último destino solicitado al MovementAgent.
    /// DashController lo usa para retomar el movimiento después del dash.
    /// </summary>
    public Vector2 CurrentDestination { get; private set; }

    /// <summary>
    /// Referencia pública al target activo.
    /// PlayerCombat la usa para saber a quién dirigir el ataque.
    /// </summary>
    public Transform CurrentTarget => _currentTarget;

    /// <summary>
    /// Expone si el ataque pendiente es el ataque de área.
    /// PlayerCombat lo consume al momento de ejecutar el ataque.
    /// </summary>
    public bool PendingAreaAttack => _pendingAreaAttack;

    /// <summary>
    /// True mientras el jugador ejecuta el ajuste final de last mile hacia el target.
    /// Suspende UpdateChase para evitar que sobreescriba el destino ajustado.
    /// </summary>
    private bool _isAdjusting;

    // ────────────────────────────────────────────────────────────────────
    // Unity Lifecycle
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Obtiene la referencia al MovementAgent en el mismo GameObject.
    /// </summary>
    private void Awake()
    {
        _agent = GetComponent<MovementAgent>();
        _agent.OnDestinationReached += HandleDestinationReached;
    }

    private void OnDestroy()
    {
        _agent.OnDestinationReached -= HandleDestinationReached;
    }

    /// <summary>
    /// Cada frame: procesa el input del jugador y actualiza la persecución
    /// si hay un target activo.
    /// </summary>
    private void Update()
    {
        HandleInput();
        UpdateChase();
    }

    private void LateUpdate()
    {
        // Resetear los flags de ataque al final del frame para que solo duren un frame.
        _pendingAreaAttack = false;
    }

    // ────────────────────────────────────────────────────────────────────
    // Input
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lee el input del mouse en este frame usando el New Input System.
    /// Evalúa los modificadores Shift y Ctrl antes de procesar el click.
    /// Ignora clicks sobre elementos de la UI para evitar mover al personaje
    /// cuando el jugador interactúa con menús o barras de vida.
    /// </summary>
    private void HandleInput()
    {
        bool leftClickDown = Mouse.current.leftButton.wasPressedThisFrame;
        bool rightClickDown = Mouse.current.rightButton.wasPressedThisFrame;

        // Movimiento continuo: recalcular destino si el mouse se mueve con el botón apretado
        bool leftHeld = Mouse.current.leftButton.isPressed;
        if (leftHeld && !leftClickDown)
        {
            // Solo recalcular si el mouse se movió para evitar pathfinding cada frame
            if (Mouse.current.delta.ReadValue().magnitude > 2f)
                leftClickDown = true;
        }

        if (!leftClickDown && !rightClickDown)
            return;

        // Ignorar clicks sobre la UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        bool shiftHeld = Keyboard.current.shiftKey.isPressed;
        bool ctrlHeld = Keyboard.current.ctrlKey.isPressed;

        // Shift activo: atacar desde el lugar sin moverse
        if (shiftHeld)
        {
            // TODO: Fase 2 - PlayerCombat leerá PendingAreaAttack y la dirección
            // al cursor para ejecutar el ataque sin mover al agente.
            _pendingAreaAttack = rightClickDown;
            return;
        }

        if (leftClickDown)
            ProcessClick(forceMove: ctrlHeld, areaAttack: false);
        else if (rightClickDown)
            ProcessClick(forceMove: false, areaAttack: true);
    }

    /// <summary>
    /// Procesa un click del mouse. Lanza un Raycast 2D para detectar si clickeó
    /// sobre un enemigo. Si no hay enemigo, convierte la posición de pantalla
    /// a coordenadas del mundo y ordena al MovementAgent moverse allí.
    /// </summary>
    /// <param name="forceMove">
    /// Si true (Ctrl activo), ignora enemigos y mueve al punto clickeado.
    /// </param>
    /// <param name="areaAttack">
    /// Si true, el ataque al llegar al rango será el ataque de área.
    /// </param>
    private void ProcessClick(bool forceMove, bool areaAttack)
    {
        Vector2 screenPos = Mouse.current.position.ReadValue();
        Vector2 worldPos = Camera.main.ScreenToWorldPoint(screenPos);

        // Ctrl activo: mover al punto ignorando enemigos
        if (forceMove)
        {
            ClearTarget();
            _agent.SetDestination(worldPos);
            CurrentDestination = worldPos;
            return;
        }

        // Raycast 2D para detectar si clickeó sobre un enemigo
        Collider2D hit = Physics2D.OverlapPoint(worldPos, enemyLayer);
        if (hit != null)
        {
            EnemyHealth enemyHealth = hit.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                SetTarget(hit.transform, enemyHealth, areaAttack);
                return;
            }
        }

        // No clickeó sobre un enemigo: mover al punto del mundo
        ClearTarget();
        _agent.SetDestination(worldPos);
        CurrentDestination = worldPos;
    }

    // ────────────────────────────────────────────────────────────────────
    // Sistema de Target
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Establece un enemigo como target activo y comienza la persecución.
    /// Se suscribe al evento OnEnemyDied para limpiar el target automáticamente
    /// cuando el enemigo muera, sin necesidad de verificarlo cada frame.
    /// </summary>
    /// <param name="enemyTransform">Transform del enemigo seleccionado.</param>
    /// <param name="enemyHealth">EnemyHealth del enemigo para suscribirse a su muerte.</param>
    /// <param name="areaAttack">Si el ataque pendiente es el ataque de área.</param>
    private void SetTarget(Transform enemyTransform, EnemyHealth enemyHealth, bool areaAttack)
    {
        if (_currentTargetHealth != null)
            _currentTargetHealth.OnEnemyDied -= HandleTargetDied;

        _currentTarget = enemyTransform;
        _currentTargetHealth = enemyHealth;
        _pendingAreaAttack = areaAttack;
        _lastTargetPosition = enemyTransform.position;

        _currentTargetHealth.OnEnemyDied += HandleTargetDied;

        // Si ya estamos en rango, no hace falta moverse
        float distance = Vector2.Distance(transform.position, enemyTransform.position);
        if (distance <= attackRange)
        {
            _isChasingEnemy = false;
            IsInAttackRange = true;
            // TODO: Fase 2 - PlayerCombat atacará inmediatamente
            return;
        }

        // Fuera de rango: iniciar persecución
        _isChasingEnemy = true;
        IsInAttackRange = false;
        _agent.SetDestination(_currentTarget.position);
        CurrentDestination = _currentTarget.position;
    }

    /// <summary>
    /// Limpia el target activo y cancela la persecución.
    /// Se llama cuando el jugador clickea en terreno vacío o usa Ctrl.
    /// </summary>
    private void ClearTarget()
    {
        if (_currentTargetHealth != null)
            _currentTargetHealth.OnEnemyDied -= HandleTargetDied;

        _currentTarget = null;
        _currentTargetHealth = null;
        _isChasingEnemy = false;
        _pendingAreaAttack = false;
        IsInAttackRange = false;
        _isAdjusting = false;
    }

    /// <summary>
    /// Callback que se ejecuta automáticamente cuando el target activo muere.
    /// Limpia el target y detiene el movimiento del agente.
    /// </summary>
    private void HandleTargetDied()
    {
        ClearTarget();
        _agent.StopMovement();
    }

    /// <summary>
    /// Si hay un target activo, actualiza la persecución cada frame.
    /// Recalcula el camino solo si el target se movió más de chaseRecalculateThreshold
    /// para evitar llamadas excesivas al pathfinding.
    /// Detiene al agente automáticamente cuando entra en rango de ataque.
    /// </summary>
    private void UpdateChase()
    {
        if (!_isChasingEnemy || _currentTarget == null)
            return;
        if (_isAdjusting)
            return;

        float distanceToTarget = Vector2.Distance(transform.position, _currentTarget.position);

        // Dentro del rango de ataque: detenerse y notificar a PlayerCombat
        if (distanceToTarget <= attackRange)
        {
            if (!IsInAttackRange)
            {
                IsInAttackRange = true;
                _agent.StopMovement();
                // TODO: Fase 2 - PlayerCombat leerá IsInAttackRange para atacar
            }
            return;
        }

        // Fuera del rango: seguir persiguiendo
        IsInAttackRange = false;

        // Recalcular camino solo si el target se movió lo suficiente
        float targetMovement = Vector2.Distance(_currentTarget.position, _lastTargetPosition);
        if (targetMovement >= chaseRecalculateThreshold)
        {
            _lastTargetPosition = _currentTarget.position;
            _agent.SetDestination(_currentTarget.position);
            CurrentDestination = _currentTarget.position;
        }
    }

    /// <summary>
    /// Callback que se ejecuta cuando el MovementAgent completa el camino.
    /// Si hay un target activo y el jugador no está en rango, busca un punto
    /// walkable dentro del attackRange y usa pathfinding para llegar ahí.
    /// Nunca mueve directamente para respetar obstáculos.
    /// </summary>
    private void HandleDestinationReached()
    {
        if (_currentTarget == null || !_isChasingEnemy) return;

        float distance = Vector2.Distance(transform.position, _currentTarget.position);

        if (distance <= attackRange)
        {
            _isAdjusting = false;
            IsInAttackRange = true;
            _agent.StopMovement();
            return;
        }

        // Solo intentar ajuste si no estamos ya ajustando
        if (!_isAdjusting)
        {
            Vector2 adjustedDestination = FindWalkablePositionInRange(_currentTarget.position);
            if (adjustedDestination != Vector2.zero)
            {
                _isAdjusting = true;
                _agent.SetDestination(adjustedDestination);
                CurrentDestination = adjustedDestination;
            }
        }
    }

    /// <summary>
    /// Busca el punto walkable más cercano al jugador dentro del attackRange
    /// del target. Samplea 8 puntos en círculo a attackRange * 0.85 de distancia
    /// del target para asegurar que el punto está dentro del rango de ataque.
    /// </summary>
    /// <param name="targetPos">Posición del target.</param>
    /// <returns>
    /// El punto walkable más cercano al jugador dentro del rango,
    /// o Vector2.zero si no hay ninguno disponible.
    /// </returns>
    private Vector2 FindWalkablePositionInRange(Vector2 targetPos)
    {
        float sampleRadius = attackRange * 0.85f;
        int sampleCount = 8;
        Vector2 bestPosition = Vector2.zero;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < sampleCount; i++)
        {
            float angle = i * (360f / sampleCount) * Mathf.Deg2Rad;
            Vector2 candidate = targetPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * sampleRadius;

            if (!AStarPathfinder.Instance.IsPositionWalkable(candidate)) continue;

            float distToPlayer = Vector2.Distance(transform.position, candidate);
            if (distToPlayer < bestDistance)
            {
                bestDistance = distToPlayer;
                bestPosition = candidate;
            }
        }

        return bestPosition;
    }
}