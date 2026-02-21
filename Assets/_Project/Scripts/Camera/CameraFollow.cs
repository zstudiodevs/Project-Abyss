using UnityEngine;

/// <summary>
/// Hace que la cámara siga al jugador suavemente.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    /// <summary>
    /// Referencia al Transform del jugador que la cámara va a seguir.
    /// Se asigna desde el Inspector arrastrando el objeto Player.
    /// </summary>
    [SerializeField] private Transform playerTransform;

    /// <summary>
    /// Qué tan suavemente sigue la cámara al jugador.
    /// 0 = sin suavidad (movimiento instantáneo)
    /// 1 = suavidad máxima (nunca llega)
    /// Valores entre 0.05 y 0.15 se sienten bien para un ARPG.
    /// </summary>
    [SerializeField] private float smoothSpeed = 0.1f;

    /// <summary>
    /// Offset es el desplazamiento fijo entre el jugador y la cámara.
    /// Z debe mantenerse negativo para que la cámara quede por encima en 2D.
    /// </summary>
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

    /// <summary>
    /// LateUpdate se ejecuta después de todos los Update() del frame.
    /// Lo usamos aquí para asegurarnos de que el jugador ya se movió
    /// antes de que la cámara actualice su posición. Evita el efecto de temblor.
    /// </summary>
    private void LateUpdate()
    {
        if (playerTransform == null) return;

        // Calculamos la posición destino: donde está el jugador más el offset
        Vector3 targetPosition = playerTransform.position + offset;

        // Lerp (Linear Interpolation) interpola suavemente entre la posición
        // actual de la cámara y la posición destino según el smoothSpeed.
        // Cuanto menor el valor, más suave y lento. Cuanto mayor, más directo.
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed);
    }
}