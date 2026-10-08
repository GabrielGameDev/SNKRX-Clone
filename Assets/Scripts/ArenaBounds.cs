using UnityEngine;

/// <summary>
/// Utilitário estático e componente configurável para consultar os limites da arena de jogo.
/// Utiliza a câmera ortográfica principal ou limites manuais.
/// </summary>
public static class ArenaBounds
{
    private static Camera cachedCamera;

    /// <summary>
    /// Retorna a caixa de limites (minX, maxX, minY, maxY) da arena com uma margem de segurança opcional.
    /// </summary>
    public static Bounds2D GetBounds(float margin = 0.5f)
    {
        if (cachedCamera == null)
        {
            cachedCamera = Camera.main;
        }

        if (cachedCamera != null && cachedCamera.orthographic)
        {
            float halfH = cachedCamera.orthographicSize;
            float halfW = halfH * cachedCamera.aspect;
            Vector2 camPos = cachedCamera.transform.position;

            return new Bounds2D(
                camPos.x - halfW + margin,
                camPos.x + halfW - margin,
                camPos.y - halfH + margin,
                camPos.y + halfH - margin
            );
        }

        // Limites padrão de fallback para aspect ratio 16:9
        return new Bounds2D(-16f + margin, 16f - margin, -9f + margin, 9f - margin);
    }
}

/// <summary>
/// Estrutura auxiliar de limites 2D.
/// </summary>
public struct Bounds2D
{
    public float minX;
    public float maxX;
    public float minY;
    public float maxY;

    public Bounds2D(float minX, float maxX, float minY, float maxY)
    {
        this.minX = minX;
        this.maxX = maxX;
        this.minY = minY;
        this.maxY = maxY;
    }

    public Vector2 Clamp(Vector2 position)
    {
        return new Vector2(
            Mathf.Clamp(position.x, minX, maxX),
            Mathf.Clamp(position.y, minY, maxY)
        );
    }

    public bool IsInside(Vector2 position)
    {
        return position.x >= minX && position.x <= maxX && position.y >= minY && position.y <= maxY;
    }
}
