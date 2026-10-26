using System;
using UnityEngine;

/// <summary>
/// Una muestra individual del movimiento del mouse
/// durante la preparación de un remate.
///
/// IMPORTANTE:
/// Esto NO representa un frame del juego.
/// Representa un evento real de movimiento recibido
/// desde el dispositivo de entrada.
///
/// Por eso podemos tener muchas muestras dentro
/// de un mismo frame renderizado.
/// </summary>
[Serializable]
public struct ShotGestureSample
{
    /// <summary>
    /// Tiempo transcurrido desde que comenzó
    /// la carga del disparo.
    /// </summary>
    public float time;

    /// <summary>
    /// Movimiento del mouse correspondiente
    /// solamente a esta muestra.
    ///
    /// X:
    /// negativo = izquierda
    /// positivo = derecha
    ///
    /// Y:
    /// negativo = abajo
    /// positivo = arriba
    /// </summary>
    public Vector2 delta;

    /// <summary>
    /// Posición virtual acumulada del gesto.
    ///
    /// No es la posición real del cursor.
    /// Es la suma de todos los deltas registrados
    /// desde que comenzó el disparo.
    /// </summary>
    public Vector2 cumulativePosition;

    public ShotGestureSample(
        float time,
        Vector2 delta,
        Vector2 cumulativePosition
    )
    {
        this.time = time;
        this.delta = delta;
        this.cumulativePosition = cumulativePosition;
    }
}