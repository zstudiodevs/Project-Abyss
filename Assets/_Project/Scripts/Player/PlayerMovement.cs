using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controla el movimiento del personaje jugador en 8 direcciones
/// y comunica la velocidad al Animator para manejar las animaciones.
/// Requiere Rigidbody2D y Animator en el mismo GameObject.
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    /// <summary>
    /// Velocidad de movimiento en unidades por segundo.
    /// Modificable desde el Inspector.
    /// </summary>
    [SerializeField] private float moveSpeed = 3f;

    /// <summary>
    /// Referencia al Rigidbody2D para aplicar movimiento físico.
    /// </summary>
    private Rigidbody2D rigidBody2D;

    /// <summary>
    /// Referencia al Animator para controlar las animaciones
    /// según el estado del jugador.
    /// </summary>
    private Animator animator;

    /// <summary>
    /// Referencia al SpriteRenderer para hacer flip horizontal
    /// del sprite según la dirección de movimiento.
    /// </summary>
    private SpriteRenderer spriteRenderer;

    /// <summary>
    /// Vector que almacena la dirección de movimiento actual.
    /// X = horizontal, Y = vertical.
    /// </summary>
    private Vector2 moveInput;

    /// <summary>
    /// Awake se ejecuta una vez al inicializar el objeto.
    /// Obtenemos todas las referencias a componentes necesarios.
    /// </summary>
    private void Awake()
    {
        rigidBody2D = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Update se ejecuta una vez por frame.
    /// Lee el input del teclado y actualiza el Animator.
    /// </summary>
    private void Update()
    {
        float x = 0f;
        float y = 0f;

        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x = 1f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x = -1f;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y = 1f;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y = -1f;

        moveInput = new Vector2(x, y).normalized;

        // Enviamos la magnitud del vector al Animator.
        // Cuando el jugador está quieto, magnitude = 0 → Idle.
        // Cuando se mueve en cualquier dirección, magnitude = 1 → Walk.
        animator.SetFloat("Speed", moveInput.magnitude);
        Debug.Log($"Move Input: {moveInput}, Speed: {moveInput.magnitude}");

        // Flip horizontal: si se mueve a la izquierda invertimos el sprite.
        // Si se mueve a la derecha mostramos el sprite original.
        if (x < 0f) spriteRenderer.flipX = true;
        else if (x > 0f) spriteRenderer.flipX = false;
    }

    /// <summary>
    /// FixedUpdate se ejecuta a intervalos fijos.
    /// Aplicamos el movimiento físico al Rigidbody2D.
    /// </summary>
    private void FixedUpdate()
    {
        rigidBody2D.linearVelocity = moveInput * moveSpeed;
    }
}