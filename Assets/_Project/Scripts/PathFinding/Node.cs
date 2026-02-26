/// <summary>
/// Representa un nodo individual dentro de la grilla de pathfinding.
/// Cada nodo corresponde a una celda del mundo y almacena toda la información
/// que el algoritmo A* necesita para calcular caminos óptimos.
/// </summary>
public class Node
{
    // ─── Propiedades del Mundo ────────────────────────────────────────────

    /// <summary>
    /// Indica si este nodo es transitable. False si hay un obstáculo.
    /// El algoritmo A* nunca incluye nodos no transitables en un camino.
    /// </summary>
    public bool Walkable { get; set; }

    /// <summary>
    /// Posición del centro de este nodo en coordenadas del mundo 2D.
    /// Se usa para mover al personaje hacia este punto durante el recorrido del camino.
    /// </summary>
    public UnityEngine.Vector2 WorldPosition { get; }

    // ─── Coordenadas en la Grilla ─────────────────────────────────────────

    /// <summary>
    /// Índice de columna de este nodo dentro de la grilla (eje X).
    /// </summary>
    public int GridX { get; }

    /// <summary>
    /// Índice de fila de este nodo dentro de la grilla (eje Y).
    /// </summary>
    public int GridY { get; }

    // ─── Costos para A* ───────────────────────────────────────────────────

    /// <summary>
    /// Costo acumulado desde el nodo de origen hasta este nodo.
    /// Se calcula sumando el costo de cada paso a lo largo del camino recorrido.
    /// Movimiento ortogonal cuesta 10, diagonal cuesta 14.
    /// </summary>
    public int GCost { get; set; }

    /// <summary>
    /// Costo heurístico estimado desde este nodo hasta el nodo destino.
    /// Se calcula con la distancia octile y nunca sobreestima el costo real,
    /// lo que garantiza que A* siempre encuentra el camino óptimo.
    /// </summary>
    public int HCost { get; set; }

    /// <summary>
    /// Costo total del nodo. Es la suma de GCost + HCost.
    /// A* siempre expande primero el nodo con menor FCost.
    /// </summary>
    public int FCost => GCost + HCost;

    // ─── Referencia al Padre ──────────────────────────────────────────────

    /// <summary>
    /// Referencia al nodo anterior en el camino calculado.
    /// Al llegar al destino, A* sigue la cadena de Parent hacia atrás
    /// para reconstruir el camino completo desde el origen.
    /// </summary>
    public Node Parent { get; set; }

    // ─── Constructor ──────────────────────────────────────────────────────

    /// <summary>
    /// Crea un nuevo nodo con su posición y coordenadas en la grilla.
    /// </summary>
    /// <param name="walkable">True si el nodo es transitable.</param>
    /// <param name="worldPosition">Posición del centro del nodo en el mundo.</param>
    /// <param name="gridX">Índice de columna en la grilla.</param>
    /// <param name="gridY">Índice de fila en la grilla.</param>
    public Node(bool walkable, UnityEngine.Vector2 worldPosition, int gridX, int gridY)
    {
        Walkable = walkable;
        WorldPosition = worldPosition;
        GridX = gridX;
        GridY = gridY;
    }
}