using UnityEngine;

/// <summary>
/// Colisionador temporal de daño. Se instancia en cada ataque, detecta todos los
/// EnemyHealth dentro de su radio, aplica daño y se destruye automáticamente.
/// No tiene conocimiento de quién lo instanció — solo sabe cuánto daño hacer.
/// </summary>
public class AttackHitbox : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Campos privados — configurados por PlayerCombat antes de activarse
    // -------------------------------------------------------------------------

    /// <summary>Daño a aplicar a cada enemigo impactado.</summary>
    private int _damage;

    /// <summary>Radio del overlap circular de detección.</summary>
    private float _radius;

    /// <summary>LayerMask de los enemigos detectables.</summary>
    private LayerMask _enemyLayer;

    // -------------------------------------------------------------------------
    // API Pública
    // -------------------------------------------------------------------------

    /// <summary>
    /// Inicializa el hitbox con sus parámetros de combate.
    /// Debe llamarse inmediatamente después de instanciar, antes del primer frame.
    /// </summary>
    /// <param name="damage">Puntos de daño a aplicar.</param>
    /// <param name="radius">Radio de detección en unidades de mundo.</param>
    /// <param name="enemyLayer">LayerMask que identifica a los enemigos.</param>
    /// <param name="lifetime">Tiempo en segundos antes de auto-destruirse.</param>
    public void Initialize(int damage, float radius, LayerMask enemyLayer, float lifetime = 0.1f)
    {
        _damage = damage;
        _radius = radius;
        _enemyLayer = enemyLayer;

        // La destrucción automática garantiza que nunca quede un hitbox huérfano
        Destroy(gameObject, lifetime);
    }

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    /// <summary>
    /// Al activarse en escena, detecta y daña a todos los enemigos en rango
    /// de forma instantánea. Un único frame de vida es suficiente para el ataque.
    /// </summary>
    private void Start()
    {
        ApplyDamageToTargetsInRange();
    }

    // -------------------------------------------------------------------------
    // Lógica de Daño
    // -------------------------------------------------------------------------

    /// <summary>
    /// Detecta todos los colliders enemigos dentro del radio y aplica daño
    /// a cada uno que tenga el componente EnemyHealth.
    /// </summary>
    private void ApplyDamageToTargetsInRange()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _radius, _enemyLayer);

        foreach (Collider2D hit in hits)
        {
            EnemyHealth enemyHealth = hit.GetComponent<EnemyHealth>();

            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(_damage);
            }
        }
    }

    // -------------------------------------------------------------------------
    // Debug Visual
    // -------------------------------------------------------------------------

    /// <summary>
    /// Dibuja el radio de detección en el Editor para facilitar el tuning
    /// de los valores de combate. Solo visible en Scene View.
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}