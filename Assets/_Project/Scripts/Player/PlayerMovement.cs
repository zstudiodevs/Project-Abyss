using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controla el movimiento del jugador en 8 direcciones.
/// Requiere un RigidBody2D en el mismo GameObject.
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    /// <summary>
    /// Velocidad de movimiento del jugador en unidades por segundo.
    /// Modificable desde el Inspector de Unity.
    /// </summary>
    [SerializeField] private float moveSpeed = 5f;

    /// <summary>
    /// Referencia al RigidBody2D del jugador.
    /// Se utiliza para mover el personaje respetando el sistema de fisica de Unity.
    /// </summary>
    private Rigidbody2D rigidBody2D;

    /// <summary>
    /// Vector de movimiento actual del jugador.
    /// x = movimiento horizontal, y = movimiento vertical.
    /// </summary>
    private Vector2 moveInput;

    /// <summary>
    /// Awake se ejecuta una sola vez cuando el objeto se inicializa,
    /// antes que Start. Lo usamos para obtener referencias a componentes.
    /// </summary>
    private void Awake()
    {
        rigidBody2D = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// Update se ejecuta una vez por frame.
    /// Aquí leemos el input del teclado con el New Input System.
    /// </summary>
    private void Update()
    {
        // Keyboard.current accede al teclado activo en este momento
        // wasPressedThisFrame devuelve true/false según si la tecla está presionada
        float x = 0f;
        float y = 0f;

        // Horizontal: D/flecha derecha = +1, A/flecha izquierda = -1
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x = 1f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x = -1f;

        // Vertical: W/flecha arriba = +1, S/flecha abajo = -1
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y = 1f;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y = -1f;

        // .normalized garantiza velocidad constante en diagonales.
        // Sin esto, moverse en diagonal sería ~40% más rápido que en línea recta.
        moveInput = new Vector2(x, y).normalized;
    }

    /// <summary>
    /// FixedUpdate se ejecuta a intervalos fijos de tiempo (por defecto 50 veces por segundo).
    /// Todo lo relacionado con física debe ir aquí, nunca en Update.
    /// </summary>
    private void FixedUpdate()
    {
        // Aplicamos la velocidad al Rigidbody2D multiplicando dirección por velocidad
        rigidBody2D.linearVelocity = moveInput * moveSpeed;
    }
}
