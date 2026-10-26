using System;
using UnityEngine;

/// <summary>
/// Punto procesado de un gesto de disparo.
///
/// A diferencia de ShotGestureSample,
/// este punto ya pertenece a una representación
/// limpia y normalizada del gesto.
///
/// ShotGestureSample:
///     RAW, directamente del mouse.
///
/// ShotGesturePoint:
///     procesado y apto para gameplay.
/// </summary>
[Serializable]
public struct ShotGesturePoint
{
    /// <summary>
    /// Tiempo normalizado dentro del gesto.
    ///
    /// 0 = comienzo
    /// 1 = final
    /// </summary>
    [Range(0f, 1f)]
    public float normalizedTime;

    /// <summary>
    /// Posición procesada del gesto.
    ///
    /// X = componente lateral.
    /// Y = componente vertical.
    ///
    /// Todavía NO está expresada en metros
    /// del mundo del juego.
    /// </summary>
    public Vector2 position;

    /// <summary>
    /// Velocidad aproximada del gesto
    /// en este punto.
    /// </summary>
    public Vector2 velocity;

    public ShotGesturePoint(
        float normalizedTime,
        Vector2 position,
        Vector2 velocity
    )
    {
        this.normalizedTime = normalizedTime;
        this.position = position;
        this.velocity = velocity;
    }
}