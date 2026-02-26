using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Implementa el algoritmo A* para calcular el camino más corto entre dos puntos
/// del mundo, evitando obstáculos detectados por PathfindingGrid.
///
/// A* mantiene dos estructuras:
/// - Open List: nodos encontrados pero no evaluados aún. Siempre se evalúa primero
///   el nodo con menor FCost (GCost + HCost).
/// - Closed List: nodos ya evaluados. Un nodo en la closed list no se vuelve a procesar.
///
/// Al llegar al destino, reconstruye el camino siguiendo la cadena de Parent
/// desde el nodo destino hasta el nodo origen.
/// </summary>
public class AStarPathfinder : MonoBehaviour
{
    // ─── Constantes de Costo ──────────────────────────────────────────────

    /// <summary>
    /// Costo de moverse a un nodo adyacente en dirección ortogonal (arriba, abajo, izquierda, derecha).
    /// Se usa 10 en lugar de 1 para poder representar el costo diagonal como entero (14 ≈ 10√2).
    /// </summary>
    private const int COST_STRAIGHT = 10;

    /// <summary>
    /// Costo de moverse a un nodo adyacente en dirección diagonal.
    /// 14 es la aproximación entera de 10 × √2 = 14.14.
    /// Usar enteros evita errores de precisión flotante en comparaciones de costos.
    /// </summary>
    private const int COST_DIAGONAL = 14;

    /// <summary>
    /// Instancia única de AStarPathfinder accesible globalmente.
    /// </summary>
    public static AStarPathfinder Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ────────────────────────────────────────────────────────────────────
    // API Pública
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Calcula el camino más corto entre dos posiciones del mundo usando A*.
    /// Retorna una lista de posiciones en orden desde el origen hasta el destino.
    /// Si no existe camino posible, retorna una lista vacía.
    /// </summary>
    /// <param name="startPos">Posición de inicio en coordenadas del mundo 2D.</param>
    /// <param name="targetPos">Posición de destino en coordenadas del mundo 2D.</param>
    /// <returns>
    /// Lista de Vector2 con las posiciones de los nodos del camino, en orden
    /// desde startPos hasta targetPos. Lista vacía si no hay camino.
    /// </returns>
    public List<Vector2> FindPath(Vector2 startPos, Vector2 targetPos)
    {
        Node startNode = PathfindingGrid.Instance.NodeFromWorldPoint(startPos);
        Node targetNode = PathfindingGrid.Instance.NodeFromWorldPoint(targetPos);

        if (!targetNode.Walkable)
            targetNode = GetNearestWalkableNode(targetNode);

        if (targetNode == null)
            return new List<Vector2>();

        // Open list: nodos encontrados pendientes de evaluación.
        // Usamos List con búsqueda manual por ahora. En una optimización futura
        // se puede reemplazar por una MinHeap para mejor rendimiento en mapas grandes.
        List<Node> openList = new List<Node>();

        // Closed list: nodos ya evaluados. HashSet para búsqueda O(1).
        HashSet<Node> closedList = new HashSet<Node>();

        // Resetear costos del nodo de inicio y agregarlo a la open list
        startNode.GCost = 0;
        startNode.HCost = GetHeuristicCost(startNode, targetNode);
        startNode.Parent = null;
        openList.Add(startNode);

        while (openList.Count > 0)
        {
            // Tomar el nodo con menor FCost de la open list.
            // En caso de empate, el menor HCost tiene prioridad (está más cerca del destino).
            Node currentNode = GetLowestFCostNode(openList);

            // Si llegamos al destino, reconstruir y retornar el camino
            if (currentNode == targetNode)
                return RetracePath(startNode, targetNode);

            openList.Remove(currentNode);
            closedList.Add(currentNode);

            // Evaluar cada vecino del nodo actual
            foreach (Node neighbour in PathfindingGrid.Instance.GetNeighbours(currentNode))
            {
                // Saltar nodos no transitables o ya evaluados
                if (!neighbour.Walkable || closedList.Contains(neighbour))
                    continue;

                // Calcular el costo tentativo de llegar a este vecino a través del nodo actual
                int tentativeGCost = currentNode.GCost + GetMovementCost(currentNode, neighbour);

                bool neighbourInOpenList = openList.Contains(neighbour);

                // Actualizar el vecino si encontramos un camino más barato hacia él,
                // o si todavía no lo habíamos encontrado
                if (tentativeGCost < neighbour.GCost || !neighbourInOpenList)
                {
                    neighbour.GCost = tentativeGCost;
                    neighbour.HCost = GetHeuristicCost(neighbour, targetNode);
                    neighbour.Parent = currentNode;

                    if (!neighbourInOpenList)
                        openList.Add(neighbour);
                }
            }
        }

        // Si la open list se vació sin llegar al destino, no hay camino posible
        return new List<Vector2>();
    }

    // ────────────────────────────────────────────────────────────────────
    // Métodos Privados — Núcleo del Algoritmo
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Retorna el nodo con menor FCost de la open list.
    /// En caso de empate en FCost, retorna el que tenga menor HCost,
    /// ya que está estimativamente más cerca del destino.
    /// </summary>
    /// <param name="openList">Lista de nodos pendientes de evaluación.</param>
    /// <returns>El nodo con menor FCost (y menor HCost en caso de empate).</returns>
    private Node GetLowestFCostNode(List<Node> openList)
    {
        Node lowest = openList[0];

        for (int i = 1; i < openList.Count; i++)
        {
            if (openList[i].FCost < lowest.FCost ||
               (openList[i].FCost == lowest.FCost && openList[i].HCost < lowest.HCost))
            {
                lowest = openList[i];
            }
        }

        return lowest;
    }

    /// <summary>
    /// Calcula la heurística de distancia entre dos nodos usando la distancia octile.
    /// La distancia octile es admisible (nunca sobreestima el costo real) y consistente,
    /// lo que garantiza que A* siempre encuentra el camino óptimo.
    ///
    /// Fórmula: h = STRAIGHT * (dx + dy) + (DIAGONAL - 2 * STRAIGHT) * min(dx, dy)
    /// Donde dx = diferencia en columnas, dy = diferencia en filas.
    /// </summary>
    /// <param name="nodeA">Nodo desde el que se calcula la heurística.</param>
    /// <param name="nodeB">Nodo destino.</param>
    /// <returns>Costo heurístico estimado entre los dos nodos.</returns>
    private int GetHeuristicCost(Node nodeA, Node nodeB)
    {
        int dx = Mathf.Abs(nodeA.GridX - nodeB.GridX);
        int dy = Mathf.Abs(nodeA.GridY - nodeB.GridY);

        return COST_STRAIGHT * (dx + dy) + (COST_DIAGONAL - 2 * COST_STRAIGHT) * Mathf.Min(dx, dy);
    }

    /// <summary>
    /// Retorna el costo de moverse desde un nodo al vecino dado.
    /// Si los nodos están en la misma fila o columna (movimiento ortogonal), cuesta 10.
    /// Si están en diagonal, cuesta 14.
    /// </summary>
    /// <param name="from">Nodo de origen.</param>
    /// <param name="to">Nodo destino adyacente.</param>
    /// <returns>Costo del movimiento entre los dos nodos.</returns>
    private int GetMovementCost(Node from, Node to)
    {
        bool isDiagonal = (from.GridX != to.GridX) && (from.GridY != to.GridY);
        return isDiagonal ? COST_DIAGONAL : COST_STRAIGHT;
    }

    /// <summary>
    /// Reconstruye el camino completo siguiendo la cadena de Parent desde el nodo
    /// destino hasta el nodo origen. El camino se invierte al final para que
    /// el resultado esté en orden origen → destino.
    /// </summary>
    /// <param name="startNode">Nodo de inicio del camino.</param>
    /// <param name="endNode">Nodo de destino donde comenzar la reconstrucción.</param>
    /// <returns>Lista de posiciones mundiales en orden desde el inicio hasta el destino.</returns>
    private List<Vector2> RetracePath(Node startNode, Node endNode)
    {
        List<Vector2> path = new List<Vector2>();
        Node currentNode = endNode;

        // Seguir la cadena de Parent hasta llegar al nodo de inicio
        while (currentNode != startNode)
        {
            path.Add(currentNode.WorldPosition);
            currentNode = currentNode.Parent;
        }

        // El camino está construido de destino a origen, hay que invertirlo
        path.Reverse();

        return path;
    }

    /// <summary>
    /// Simplifica el camino eliminando waypoints redundantes usando line-of-sight.
    /// Si desde el punto actual hay línea de visión directa hasta el punto N+2
    /// (sin obstáculos entre ellos), el punto N+1 se elimina porque el personaje
    /// puede ir directamente sin pasar por él.
    /// El resultado es un movimiento visualmente más fluido y natural.
    /// </summary>
    /// <param name="path">Camino calculado por FindPath.</param>
    /// <param name="obstacleMask">LayerMask de obstáculos para el linecast.</param>
    /// <returns>Camino simplificado con los waypoints mínimos necesarios.</returns>
    public List<Vector2> SmoothPath(List<Vector2> path, LayerMask obstacleMask)
    {
        if (path.Count <= 2)
            return path;

        List<Vector2> smoothed = new List<Vector2> { path[0] };
        int current = 0;

        while (current < path.Count - 1)
        {
            int next = current + 1;

            // Intentar saltar waypoints intermedios mientras haya line-of-sight
            while (next + 1 < path.Count)
            {
                bool hasLineOfSight = !Physics2D.Linecast(
                    path[current],
                    path[next + 1],
                    obstacleMask
                );

                if (hasLineOfSight)
                    next++;
                else
                    break;
            }

            smoothed.Add(path[next]);
            current = next;
        }

        return smoothed;
    }

    /// <summary>
    /// Busca el nodo transitable más cercano a un nodo no transitable.
    /// Se usa cuando el destino exacto está bloqueado, como cuando el jugador
    /// clickea sobre un enemigo cuyo nodo está ocupado.
    /// </summary>
    /// <param name="node">Nodo no transitable de referencia.</param>
    /// <returns>El nodo transitable más cercano, o null si no hay ninguno.</returns>
    private Node GetNearestWalkableNode(Node node)
    {
        List<Node> neighbours = PathfindingGrid.Instance.GetNeighbours(node);
        foreach (Node neighbour in neighbours)
        {
            if (neighbour.Walkable)
                return neighbour;
        }
        return null;
    }

    /// <summary>
    /// Busca el nodo correspondiente a la posicion dada y verifica si es transitable.
    /// </summary>
    /// <param name="worldPosition">Posición en el mundo 2D.</param>
    /// <returns>True si el nodo es transitable, false en caso contrario.</returns>
    public bool IsPositionWalkable(Vector2 worldPosition)
    {
        Node node = PathfindingGrid.Instance.NodeFromWorldPoint(worldPosition);
        return node != null && node.Walkable;
    }
}