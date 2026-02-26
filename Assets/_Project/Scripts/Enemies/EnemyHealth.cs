using System.Collections;
using UnityEngine;

/// <summary>
/// Maneja los puntos de vida de un enemigo.
/// Controla el daño recibido, el hit flash y la lógica de muerte.
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    /// <summary>
    /// Puntos de vida máximos del enemigo. Configurable desde el Inspector.
    /// </summary>
    [SerializeField] private int maxHealth = 50;

    /// <summary>
    /// Duración del efecto de hit flash en segundos.
    /// </summary>
    [SerializeField] private float hitFlashDuration = 0.1f;

    /// <summary>
    /// Color del hit flash. Blanco puro por defecto.
    /// </summary>
    [SerializeField] private Color hitFlashColor = Color.white;

    /// <summary>
    /// Puntos de vida actuales del enemigo.
    /// </summary>
    private int currentHealth;

    /// <summary>
    /// Referencia al SpriteRenderer para el hit flash.
    /// </summary>
    private SpriteRenderer spriteRenderer;

    /// <summary>
    /// Color original del sprite para restaurarlo después del hit flash.
    /// </summary>
    private Color originalColor;

    /// <summary>
    /// Evento que se dispara cuando el enemigo muere.
    /// Útil para el sistema de loot en Fase 4.
    /// </summary>
    public event System.Action OnEnemyDied;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
    }

    private void Start()
    {
        currentHealth = maxHealth;
    }

    /// <summary>
    /// Aplica daño al enemigo y activa el hit flash.
    /// Si la vida llega a 0, llama a Die().
    /// </summary>
    /// <param name="damage">Cantidad de daño a recibir.</param>
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log($"{gameObject.name} recibió {damage} de daño. Vida: {currentHealth}/{maxHealth}");

        StartCoroutine(HitFlashCoroutine());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Corrutina que aplica el efecto de hit flash:
    /// cambia el color del sprite a blanco por un instante
    /// y luego lo restaura al color original.
    /// </summary>
    private IEnumerator HitFlashCoroutine()
    {
        spriteRenderer.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashDuration);
        spriteRenderer.color = originalColor;
    }

    /// <summary>
    /// Lógica de muerte del enemigo.
    /// Notifica al sistema y destruye el GameObject.
    /// </summary>
    private void Die()
    {
        Debug.Log($"{gameObject.name} ha muerto.");
        OnEnemyDied?.Invoke();
        // TODO: Fase 4 — Spawn de loot antes de destruir
        Destroy(gameObject);
    }
}