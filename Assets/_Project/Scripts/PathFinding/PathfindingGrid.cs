using UnityEngine;

/// <summary>
/// Genera y administra la grilla de nodos sobre la que opera el algoritmo A*.
/// Implementada como MonoBehaviour Singleton para que cualquier sistema del juego
/// pueda acceder a ella sin necesidad de guardar referencias explícitas.
///
/// La grilla divide el mundo en celdas iguales. Cada celda es un Node que sabe
/// si puede ser transitado o si está bloqueado por un obstáculo.
/// Esta grilla representa únicamente el terreno estático (paredes, rocas, etc.).
/// Los agentes móviles (jugador, enemigos) no modifican la grilla.
/// </summary>
public class PathfindingGrid : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────

    /// <summary>
    /// Instancia única de PathfindingGrid accesible globalmente.
    /// Cualquier script puede llamar PathfindingGrid.Instance sin buscar la referencia.
    /// </summary>
    public static PathfindingGrid Instance { get; private set; }

    // ─── Configuración Inspector ──────────────────────────────────────────

    /// <summary>
    /// Tamaño total del área del mundo que cubre la grilla, en unidades de Unity.
    /// Debe ser lo suficientemente grande para cubrir el área jugable completa.
    /// </summary>
    [Header("Tamaño del Mundo")]
    [SerializeField] private Vector2 gridWorldSize = new Vector2(20f, 20f);

    /// <summary>
    /// Radio de cada nodo en unidades de Unity. Determina el tamaño de cada celda.
    /// Un radio de 0.5 genera celdas de 1x1 unidad.
    /// Valores más pequeños = grilla más precisa pero más costosa en memoria y cálculo.
    /// </summary>
    [SerializeField] private float nodeRadius = 0.5f;

    /// <summary>
    /// LayerMask que define qué objetos se consideran obstáculos.
    /// Cualquier Collider2D en estas capas marcará el nodo correspondiente como no transitable.
    /// No incluir la capa del jugador ni la de los enemigos.
    /// </summary>
    [Header("Detección de Obstáculos")]
    [SerializeField] private LayerMask obstacleMask;

    // ─── Estado Interno ───────────────────────────────────────────────────

    /// <summary>
    /// Array bidimensional que contiene todos los nodos de la grilla.
    /// Se accede con _grid[x, y] donde x es la columna e y es la fila.
    /// </summary>
    private Node[,] _grid;

    /// <summary>
    /// Diámetro de cada nodo. Es nodeRadius * 2.
    /// Se precalcula para no repetir la multiplicación en cada frame.
    /// </summary>
    private float _nodeDiameter;

    /// <summary>
    /// Cantidad de nodos en el eje X (columnas).
    /// Se calcula dividiendo el ancho del mundo por el diámetro de cada nodo.
    /// </summary>
    private int _gridSizeX;

    /// <summary>
    /// Cantidad de nodos en el eje Y (filas).
    /// Se calcula dividiendo el alto del mundo por el diámetro de cada nodo.
    /// </summary>
    private int _gridSizeY;

    // ─── Propiedad Pública ────────────────────────────────────────────────

    /// <summary>
    /// Cantidad total de nodos en la grilla.
    /// AStarPathfinder la usa para dimensionar sus estructuras de datos internas.
    /// </summary>
    public int MaxSize => _gridSizeX * _gridSizeY;

    // ────────────────────────────────────────────────────────────────────
    // Unity Lifecycle
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Inicializa el Singleton y genera la grilla al arrancar la escena.
    /// Si ya existe una instancia, destruye este GameObject para mantener
    /// la unicidad del Singleton.
    /// </summary>
    private void Awake()
    {
        // Garantizar que solo exista una instancia
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Precalcular dimensiones
        _nodeDiameter = nodeRadius * 2f;
        _gridSizeX = Mathf.RoundToInt(gridWorldSize.x / _nodeDiameter);
        _gridSizeY = Mathf.RoundToInt(gridWorldSize.y / _nodeDiameter);

        CreateGrid();
    }

    // ────────────────────────────────────────────────────────────────────
    // Generación de la Grilla
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Genera el array bidimensional de nodos que representa el mundo navegable.
    /// Para cada nodo, determina si es transitable verificando si hay algún
    /// Collider2D del obstacleMask en su posición.
    /// </summary>
    private void CreateGrid()
    {
        _grid = new Node[_gridSizeX, _gridSizeY];

        // Esquina inferior izquierda del área de la grilla en coordenadas del mundo.
        // Desde aquí calculamos la posición de cada nodo.
        Vector2 worldBottomLeft = (Vector2)transform.position
            - Vector2.right * gridWorldSize.x / 2f
            - Vector2.up * gridWorldSize.y / 2f;

        for (int x = 0; x < _gridSizeX; x++)
        {
            for (int y = 0; y < _gridSizeY; y++)
            {
                // Posición del centro de este nodo en el mundo
                Vector2 worldPoint = worldBottomLeft
                    + Vector2.right * (x * _nodeDiameter + nodeRadius)
                    + Vector2.up * (y * _nodeDiameter + nodeRadius);

                // Un nodo es transitable si no hay ningún obstáculo en su área
                bool walkable = Physics2D.OverlapCircle(worldPoint, nodeRadius, obstacleMask) == null;

                _grid[x, y] = new Node(walkable, worldPoint, x, y);
            }
        }

        Node testCenter = NodeFromWorldPoint(Vector2.zero);
        Debug.Log($"Nodo en (0,0): GridX={testCenter.GridX}, GridY={testCenter.GridY}, Walkable={testCenter.Walkable}");

        var neighbours = GetNeighbours(testCenter);
        Debug.Log($"Vecinos del nodo central: {neighbours.Count}");
    }

    // ────────────────────────────────────────────────────────────────────
    // Consultas de la Grilla
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Retorna el nodo de la grilla más cercano a una posición del mundo.
    /// Si la posición está fuera de la grilla, retorna el nodo del borde más cercano
    /// en lugar de lanzar una excepción. Esto evita crashes cuando un agente
    /// se encuentra momentáneamente fuera del área navegable.
    /// </summary>
    /// <param name="worldPosition">Posición en coordenadas del mundo 2D.</param>
    /// <returns>El nodo más cercano a esa posición.</returns>
    public Node NodeFromWorldPoint(Vector2 worldPosition)
    {
        // Calcular qué fracción del área de la grilla corresponde a esta posición.
        // 0 = borde izquierdo/inferior, 1 = borde derecho/superior.
        float percentX = (worldPosition.x - transform.position.x + gridWorldSize.x / 2f) / gridWorldSize.x;
        float percentY = (worldPosition.y - transform.position.y + gridWorldSize.y / 2f) / gridWorldSize.y;

        // Clampear para que posiciones fuera de la grilla no generen índices inválidos
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.RoundToInt((_gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((_gridSizeY - 1) * percentY);

        return _grid[x, y];
    }

    /// <summary>
    /// Retorna todos los nodos adyacentes (hasta 8) al nodo dado.
    /// Incluye diagonales. Excluye nodos fuera de los límites de la grilla.
    /// El algoritmo A* usa este método para explorar los posibles pasos desde cada nodo.
    /// </summary>
    /// <param name="node">El nodo del que se quieren obtener los vecinos.</param>
    /// <returns>Lista de nodos adyacentes válidos.</returns>
    public System.Collections.Generic.List<Node> GetNeighbours(Node node)
    {
        var neighbours = new System.Collections.Generic.List<Node>();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                // Saltar el nodo central (es el propio nodo)
                if (x == 0 && y == 0)
                    continue;

                int checkX = node.GridX + x;
                int checkY = node.GridY + y;

                // Solo agregar si está dentro de los límites de la grilla
                if (checkX >= 0 && checkX < _gridSizeX &&
                    checkY >= 0 && checkY < _gridSizeY)
                {
                    neighbours.Add(_grid[checkX, checkY]);
                }
            }
        }

        return neighbours;
    }

    // ────────────────────────────────────────────────────────────────────
    // Visualización (Solo Editor)
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Dibuja la grilla en la Scene view del Editor usando Gizmos.
    /// Verde = nodo transitable. Rojo = nodo bloqueado por obstáculo.
    /// Estos Gizmos son invisibles en Game view y en builds del juego.
    /// Son una herramienta de debugging que facilita verificar que los
    /// obstáculos están siendo detectados correctamente.
    /// </summary>
    private void OnDrawGizmos()
    {
        // Dibujar el contorno del área total de la grilla
        Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.3f);
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, gridWorldSize.y, 0f));

        // Dibujar cada nodo solo si la grilla ya fue generada
        if (_grid == null)
            return;

        foreach (Node node in _grid)
        {
            Gizmos.color = node.Walkable
                ? new Color(0f, 1f, 0f, 0.15f)   // Verde semitransparente = transitable
                : new Color(1f, 0f, 0f, 0.4f);    // Rojo semitransparente = bloqueado

            Gizmos.DrawCube(
                new Vector3(node.WorldPosition.x, node.WorldPosition.y, 0f),
                Vector3.one * (_nodeDiameter - 0.05f) // Pequeño margen para ver los bordes
            );
        }
    }
}