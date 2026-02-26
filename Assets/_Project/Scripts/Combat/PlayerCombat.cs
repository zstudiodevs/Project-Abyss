using UnityEngine;

/// <summary>
/// Maneja el sistema de combate del jugador.
/// Lee el estado de PlayerMovement para saber cuándo y hacia dónde atacar.
/// Instancia AttackHitbox para el ataque primario y usa OverlapCircleAll
/// directamente para el ataque secundario en área.
/// </summary>
public class PlayerCombat : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Configuración — editable desde el Inspector
    // -------------------------------------------------------------------------

    [Header("Ataque Primario")]

    /// <summary>Daño del ataque primario en HP.</summary>
    [SerializeField] private int _primaryDamage = 25;

    /// <summary>
    /// Radio del hitbox del ataque primario en unidades de mundo.
    /// Debe coincidir con attackRange de PlayerMovement (1.5 unidades).
    /// </summary>
    [SerializeField] private float _primaryRadius = 0.6f;

    /// <summary>Ataques por segundo. 3 = un ataque cada 0.33 segundos.</summary>
    [SerializeField] private float _attackSpeed = 3f;

    /// <summary>Prefab del AttackHitbox a instanciar en cada ataque primario.</summary>
    [SerializeField] private AttackHitbox _attackHitboxPrefab;

    [Header("Ataque Secundario")]

    /// <summary>Daño del ataque secundario en HP (25 con reducción del 15%).</summary>
    [SerializeField] private int _secondaryDamage = 21;

    /// <summary>Radio del ataque de área en unidades de mundo.</summary>
    [SerializeField] private float _secondaryRadius = 2.5f;

    /// <summary>Cooldown del ataque secundario en segundos.</summary>
    [SerializeField] private float _secondaryCooldown = 1f;

    [Header("Capas")]

    /// <summary>LayerMask que identifica a los enemigos. Debe ser la capa Enemy.</summary>
    [SerializeField] private LayerMask _enemyLayer;

    // -------------------------------------------------------------------------
    // Referencias internas
    // -------------------------------------------------------------------------

    /// <summary>Referencia a PlayerMovement para leer estado de target y flags.</summary>
    private PlayerMovement _playerMovement;

    // -------------------------------------------------------------------------
    // Estado interno
    // -------------------------------------------------------------------------

    /// <summary>Tiempo en que se ejecutó el último ataque primario.</summary>
    private float _lastPrimaryAttackTime = -999f;

    /// <summary>Tiempo en que se ejecutó el último ataque secundario.</summary>
    private float _lastSecondaryAttackTime = -999f;

    /// <summary>Intervalo entre ataques primarios calculado a partir de _attackSpeed.</summary>
    private float AttackInterval => 1f / _attackSpeed;

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    /// <summary>
    /// Obtiene la referencia a PlayerMovement en el mismo GameObject.
    /// </summary>
    private void Awake()
    {
        _playerMovement = GetComponent<PlayerMovement>();

        if (_playerMovement == null)
        {
            Debug.LogError("PlayerCombat: No se encontró PlayerMovement en el mismo GameObject.");
        }

        if (_attackHitboxPrefab == null)
        {
            Debug.LogError("PlayerCombat: AttackHitboxPrefab no está asignado en el Inspector.");
        }
    }

    /// <summary>
    /// Evalúa cada frame si se cumplen las condiciones para ejecutar un ataque.
    /// </summary>
    private void Update()
    {
        HandlePrimaryAttack();
        HandleSecondaryAttack();
    }

    // -------------------------------------------------------------------------
    // Ataque Primario
    // -------------------------------------------------------------------------

    /// <summary>
    /// Ejecuta el ataque primario si el jugador tiene un target activo,
    /// está dentro del rango de ataque y el cooldown de Attack Speed lo permite.
    /// </summary>
    private void HandlePrimaryAttack()
    {
        // Sin target no hay ataque primario automático
        if (_playerMovement.CurrentTarget == null) return;

        // Debe estar dentro del rango de ataque
        if (!_playerMovement.IsInAttackRange) return;

        // Respetar el cooldown de Attack Speed
        if (Time.time - _lastPrimaryAttackTime < AttackInterval) return;

        ExecutePrimaryAttack();
    }

    /// <summary>
    /// Instancia el AttackHitbox en la posición del target activo.
    /// El hitbox nace en la posición del enemigo para garantizar el impacto.
    /// </summary>
    private void ExecutePrimaryAttack()
    {
        _lastPrimaryAttackTime = Time.time;

        Vector2 hitboxPosition = _playerMovement.CurrentTarget.position;

        AttackHitbox hitbox = Instantiate(_attackHitboxPrefab, hitboxPosition, Quaternion.identity);
        hitbox.Initialize(_primaryDamage, _primaryRadius, _enemyLayer);
    }

    // -------------------------------------------------------------------------
    // Ataque Secundario
    // -------------------------------------------------------------------------

    /// <summary>
    /// Ejecuta el ataque secundario en área cuando PlayerMovement indica
    /// que se recibió un Shift + Click derecho, respetando el cooldown.
    /// </summary>
    private void HandleSecondaryAttack()
    {
        // PlayerMovement activa este flag con Shift + Click derecho
        if (!_playerMovement.PendingAreaAttack) return;

        // Respetar el cooldown del ataque secundario
        if (Time.time - _lastSecondaryAttackTime < _secondaryCooldown) return;

        ExecuteSecondaryAttack();
    }

    /// <summary>
    /// Aplica daño a todos los enemigos dentro del radio de área simultáneamente.
    /// No instancia ningún hitbox — usa OverlapCircleAll directamente.
    /// </summary>
    private void ExecuteSecondaryAttack()
    {
        _lastSecondaryAttackTime = Time.time;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _secondaryRadius, _enemyLayer);

        foreach (Collider2D hit in hits)
        {
            EnemyHealth enemyHealth = hit.GetComponent<EnemyHealth>();

            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(_secondaryDamage);
            }
        }

        Debug.Log($"PlayerCombat: Ataque secundario — {hits.Length} enemigos en rango.");
    }

    // -------------------------------------------------------------------------
    // Debug Visual
    // -------------------------------------------------------------------------

    /// <summary>
    /// Dibuja el radio del ataque secundario en el Editor para facilitar
    /// el tuning visual. Solo visible en Scene View.
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, _secondaryRadius);
    }
}