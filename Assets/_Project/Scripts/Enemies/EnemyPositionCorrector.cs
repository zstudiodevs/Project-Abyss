using UnityEngine;

/// <summary>
/// Componente que garantiza que el enemigo nunca ocupe una posición inválida
/// en la grilla de pathfinding al iniciar la escena.
/// 
/// En Start(), verifica si alguna parte del collider del enemigo se superpone
/// con nodos no caminables. Si detecta una posición inválida, usa
/// GridPositionValidator para encontrar la posición válida más cercana
/// y reposiciona al enemigo automáticamente.
/// 
/// Agregar este componente a todos los prefabs de enemigos.
/// Cuando el SpawnManager (Fase 4) instancie enemigos en runtime,
/// este componente corregirá automáticamente cualquier posición inválida.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class EnemyPositionCorrector : MonoBehaviour
{
    // ─── Configuración Inspector ──────────────────────────────────────────

    /// <summary>
    /// Si true, muestra en la consola información sobre las correcciones aplicadas.
    /// Útil durante desarrollo para detectar enemigos mal posicionados en el editor.
    /// Desactivar en builds de producción.
    /// </summary>
    [Header("Debug")]
    [SerializeField] private bool logCorrections = true;

    // ─── Referencias ──────────────────────────────────────────────────────

    /// <summary>
    /// Collider2D del enemigo. Se usa para samplear los 9 puntos de verificación.
    /// </summary>
    private Collider2D _collider;

    // ────────────────────────────────────────────────────────────────────
    // Unity Lifecycle
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Obtiene la referencia al Collider2D en el mismo GameObject.
    /// </summary>
    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
    }

    /// <summary>
    /// Verifica y corrige la posición del enemigo al iniciar.
    /// Se ejecuta en Start() en lugar de Awake() para garantizar que
    /// PathfindingGrid ya generó la grilla (que también ocurre en Awake()).
    /// </summary>
    private void Start()
    {
        CorrectPosition();
    }

    // ────────────────────────────────────────────────────────────────────
    // Lógica de Corrección
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifica si la posición actual es válida en la grilla.
    /// Si no lo es, busca la posición válida más cercana y reposiciona al enemigo.
    /// </summary>
    private void CorrectPosition()
    {
        // Verificar que PathfindingGrid esté disponible
        if (PathfindingGrid.Instance == null)
        {
            Debug.LogWarning($"EnemyPositionCorrector en {gameObject.name}: PathfindingGrid no encontrado. Asegurate de que el GameObject Pathfinding esté en la escena.");
            return;
        }

        // Si la posición ya es válida, no hay nada que corregir
        if (GridPositionValidator.IsPositionValid(_collider))
        {
            if (logCorrections)
                Debug.Log($"EnemyPositionCorrector: {gameObject.name} en posición válida {(Vector2)transform.position}.");
            return;
        }

        // Posición inválida — buscar la más cercana válida
        Vector2 originalPosition = transform.position;
        Vector2 validPosition = GridPositionValidator.FindNearestValidPosition(_collider);

        // Calcular el offset entre el centro del bounds y el transform
        // El bounds puede no coincidir exactamente con el transform si hay offset en el collider
        Vector2 boundsToTransformOffset = (Vector2)transform.position - (Vector2)_collider.bounds.center;

        // Aplicar la corrección manteniendo el offset entre transform y bounds
        transform.position = validPosition + boundsToTransformOffset;

        if (logCorrections)
            Debug.Log($"EnemyPositionCorrector: {gameObject.name} reposicionado de {originalPosition} a {(Vector2)transform.position}.");
    }

    /// <summary>
    /// Método público para forzar una corrección de posición en cualquier momento.
    /// El SpawnManager (Fase 4) puede llamarlo después de instanciar un enemigo
    /// en una posición específica para garantizar que es válida.
    /// </summary>
    public void ForceCorrection()
    {
        CorrectPosition();
    }
}