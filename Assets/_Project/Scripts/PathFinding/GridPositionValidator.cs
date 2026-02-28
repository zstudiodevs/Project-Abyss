using UnityEngine;

/// <summary>
/// Utilidad estática para validar y corregir posiciones en la grilla de pathfinding.
/// 
/// No es un MonoBehaviour — es una clase de servicio pura que cualquier sistema
/// puede invocar sin necesidad de estar en la escena.
/// 
/// Uso principal:
/// - EnemyPositionCorrector la llama en Start() para corregir la posición inicial del enemigo.
/// - SpawnManager la llamará en Fase 4 para validar posiciones antes de instanciar enemigos.
/// </summary>
public static class GridPositionValidator
{
    /// <summary>
    /// Número de anillos concéntricos a explorar en la búsqueda en espiral.
    /// Con nodeRadius = 0.5 (celdas de 1x1), 10 anillos cubren un área de 10 unidades.
    /// </summary>
    private const int MaxSearchRings = 10;

    /// <summary>
    /// Verifica si la posición actual es válida para el collider dado.
    /// Una posición es válida cuando todos los puntos de muestra del collider
    /// caen en nodos walkables de la grilla.
    /// </summary>
    /// <param name="collider">Collider2D del objeto a verificar.</param>
    /// <returns>True si la posición es completamente válida.</returns>
    public static bool IsPositionValid(Collider2D collider)
    {
        Vector2[] samplePoints = GetSamplePoints(collider, collider.bounds.center);
        return AllPointsWalkable(samplePoints);
    }

    /// <summary>
    /// Busca la posición válida más cercana a la posición actual del collider
    /// usando una búsqueda en espiral sobre la grilla de pathfinding.
    /// 
    /// El algoritmo expande anillos concéntricos desde la posición original,
    /// probando cada candidato con los 9 puntos de muestra del collider.
    /// Retorna la primera posición donde todos los puntos son walkables.
    /// </summary>
    /// <param name="collider">Collider2D del objeto a reposicionar.</param>
    /// <returns>
    /// La posición válida más cercana, o la posición original si no se
    /// encontró ninguna dentro del rango de búsqueda.
    /// </returns>
    public static Vector2 FindNearestValidPosition(Collider2D collider)
    {
        Vector2 originalCenter = collider.bounds.center;

        // Verificar si la posición actual ya es válida
        if (AllPointsWalkable(GetSamplePoints(collider, originalCenter)))
            return originalCenter;

        float nodeSize = 1f; // Tamaño de celda (nodeRadius * 2 = 0.5 * 2)

        // Buscar en anillos concéntricos expandiéndose desde el centro
        for (int ring = 1; ring <= MaxSearchRings; ring++)
        {
            // Iterar todos los candidatos en el anillo actual
            for (int x = -ring; x <= ring; x++)
            {
                for (int y = -ring; y <= ring; y++)
                {
                    // Solo procesar los candidatos en el borde del anillo actual
                    // Los anillos internos ya fueron evaluados en iteraciones anteriores
                    if (Mathf.Abs(x) != ring && Mathf.Abs(y) != ring)
                        continue;

                    Vector2 candidate = originalCenter + new Vector2(x * nodeSize, y * nodeSize);
                    Vector2[] samplePoints = GetSamplePoints(collider, candidate);

                    if (AllPointsWalkable(samplePoints))
                        return candidate;
                }
            }
        }

        // No se encontró posición válida en el rango de búsqueda
        Debug.LogWarning($"GridPositionValidator: No se encontró posición válida en {MaxSearchRings} anillos desde {originalCenter}");
        return originalCenter;
    }

    /// <summary>
    /// Genera 9 puntos de muestra distribuidos sobre el área del collider
    /// desplazados a la posición candidata dada.
    /// 
    /// Los 9 puntos son: centro, 4 esquinas y 4 puntos medios de los bordes.
    /// Esta distribución garantiza detectar cualquier solapamiento significativo
    /// con celdas no caminables, incluso cuando el centro está en zona válida.
    /// </summary>
    /// <param name="collider">Collider2D de referencia para obtener las dimensiones.</param>
    /// <param name="candidateCenter">Centro de la posición candidata a evaluar.</param>
    /// <returns>Array de 9 puntos en coordenadas del mundo.</returns>
    private static Vector2[] GetSamplePoints(Collider2D collider, Vector2 candidateCenter)
    {
        // Calcular el offset entre el centro candidato y el centro actual del bounds
        Vector2 offset = candidateCenter - (Vector2)collider.bounds.center;

        // Obtener las dimensiones del bounds
        float halfW = collider.bounds.extents.x;
        float halfH = collider.bounds.extents.y;

        // Centro del bounds desplazado a la posición candidata
        Vector2 center = (Vector2)collider.bounds.center + offset;

        // Pequeño margen interior para evitar falsos positivos en los bordes exactos
        float margin = 0.1f;
        float w = halfW - margin;
        float h = halfH - margin;

        return new Vector2[]
        {
            center,                                 // Centro
            center + new Vector2(-w, -h),           // Esquina inferior izquierda
            center + new Vector2( w, -h),           // Esquina inferior derecha
            center + new Vector2(-w,  h),           // Esquina superior izquierda
            center + new Vector2( w,  h),           // Esquina superior derecha
            center + new Vector2( 0, -h),           // Borde inferior centro
            center + new Vector2( 0,  h),           // Borde superior centro
            center + new Vector2(-w,  0),           // Borde izquierdo centro
            center + new Vector2( w,  0),           // Borde derecho centro
        };
    }

    /// <summary>
    /// Verifica que todos los puntos dados correspondan a nodos walkables en la grilla.
    /// En cuanto encuentra un punto no walkable, retorna false inmediatamente.
    /// </summary>
    /// <param name="points">Array de posiciones en coordenadas del mundo a verificar.</param>
    /// <returns>True si todos los puntos están en nodos walkables.</returns>
    private static bool AllPointsWalkable(Vector2[] points)
    {
        foreach (Vector2 point in points)
        {
            if (!AStarPathfinder.Instance.IsPositionWalkable(point))
                return false;
        }
        return true;
    }
}