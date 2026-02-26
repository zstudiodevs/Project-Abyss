using System.Collections;
using UnityEngine;

/// <summary>
/// Maneja los puntos de vida del jugador.
/// Controla el daño recibido, los frames de invulnerabilidad,
/// el feedback visual de parpadeo y la lógica de muerte.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    /// <summary>
    /// Puntos de vida máximos del jugador. Configurable desde el Inspector.
    /// </summary>
    [SerializeField] private int maxHealth = 100;

    /// <summary>
    /// Duración en segundos de los frames de invulnerabilidad
    /// después de recibir daño.
    /// </summary>
    [SerializeField] private float invulnerabilityDuration = 0.5f;

    /// <summary>
    /// Velocidad del parpadeo visual durante la invulnerabilidad.
    /// Cuanto mayor el valor, más rápido parpadea.
    /// </summary>
    [SerializeField] private float blinkFrequency = 10f;

    /// <summary>
    /// Puntos de vida actuales del jugador.
    /// </summary>
    private int currentHealth;

    /// <summary>
    /// Indica si el jugador está en estado de invulnerabilidad.
    /// Cuando es true, TakeDamage no aplica daño.
    /// </summary>
    private bool isInvulnerable = false;

    /// <summary>
    /// Referencia al SpriteRenderer para el efecto de parpadeo.
    /// </summary>
    private SpriteRenderer spriteRenderer;

    /// <summary>
    /// Propiedad de solo lectura para consultar la vida actual
    /// desde otros sistemas como la UI.
    /// </summary>
    public int CurrentHealth => currentHealth;

    /// <summary>
    /// Propiedad de solo lectura para consultar la vida máxima.
    /// </summary>
    public int MaxHealth => maxHealth;

    /// <summary>
    /// Evento que se dispara cada vez que la vida cambia.
    /// La UI se suscribe a este evento para actualizarse automáticamente.
    /// Recibe vida actual y vida máxima como parámetros.
    /// </summary>
    public event System.Action<int, int> OnHealthChanged;

    /// <summary>
    /// Evento que se dispara cuando el jugador muere.
    /// </summary>
    public event System.Action OnPlayerDied;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Aplica daño al jugador si no está en estado de invulnerabilidad.
    /// Activa los iFrames y el efecto visual de parpadeo.
    /// </summary>
    /// <param name="damage">Cantidad de daño a recibir.</param>
    public void TakeDamage(int damage)
    {
        if (isInvulnerable) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log($"Jugador recibió {damage} de daño. Vida: {currentHealth}/{maxHealth}");

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        StartCoroutine(InvulnerabilityCoroutine());
    }

    /// <summary>
    /// Activa la invulnerabilidad desde un sistema externo, como el dash.
    /// Útil para que DashController pueda activar los iFrames directamente.
    /// </summary>
    public void ActivateInvulnerability()
    {
        StartCoroutine(InvulnerabilityCoroutine());
    }

    /// <summary>
    /// Corrutina que maneja los frames de invulnerabilidad y el parpadeo visual.
    /// </summary>
    private IEnumerator InvulnerabilityCoroutine()
    {
        isInvulnerable = true;

        float elapsed = 0f;
        while (elapsed < invulnerabilityDuration)
        {
            // Alterna la visibilidad del sprite para crear el efecto de parpadeo
            float blinkState = Mathf.Sin(elapsed * blinkFrequency * Mathf.PI);
            spriteRenderer.enabled = blinkState >= 0;

            elapsed += Time.deltaTime;
            yield return null;
        }

        spriteRenderer.enabled = true;
        isInvulnerable = false;
    }

    /// <summary>
    /// Lógica de muerte del jugador.
    /// Desactiva el input y notifica al resto del sistema.
    /// </summary>
    private void Die()
    {
        Debug.Log("El jugador ha muerto.");
        isInvulnerable = true; // Evita daño adicional durante la animación de muerte
        OnPlayerDied?.Invoke();
        // TODO: Activar animación de muerte y Game Over screen
    }
}