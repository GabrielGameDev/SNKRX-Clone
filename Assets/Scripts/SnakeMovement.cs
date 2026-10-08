using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controla o movimento da cabeça da cobra e coordena todos os segmentos do corpo (estilo SNKRX).
/// Utiliza um buffer de histórico de posições baseado em distância percorrida, garantindo que
/// os segmentos sigam fielmente as curvas sem cortar caminho e sem jitter.
/// Inclui suporte completo ao movimento estilo Pac-Man (screen wrap toroidal), permitindo que a cobra
/// saia suavemente por uma borda da tela/arena e reapareça na borda oposta.
/// </summary>
[DisallowMultipleComponent]
public class SnakeMovement : MonoBehaviour
{
    [System.Serializable]
    public struct PathPoint
    {
        public Vector3 position;
        public Quaternion rotation;

        public PathPoint(Vector3 pos, Quaternion rot)
        {
            position = pos;
            rotation = rot;
        }
    }

    [Header("Movement Settings")]
    [Tooltip("Velocidade constante de avanço para frente.")]
    [SerializeField] private float moveSpeed = 6f;

    [Tooltip("Velocidade de rotação/giro suave (graus por segundo).")]
    [SerializeField] private float steerSpeed = 220f;

    [Header("Segment Settings")]
    [Tooltip("Prefab do segmento que compõe o corpo da cobra.")]
    [SerializeField] private SnakeSegment segmentPrefab;

    [Tooltip("Quantidade de segmentos criados automaticamente no início.")]
    [SerializeField] private int initialSegments = 4;

    [Tooltip("Distância fixa mantida entre cada segmento consecutivo.")]
    [SerializeField] private float segmentSpacing = 0.8f;

    [Tooltip("Distância mínima percorrida pela cabeça para registrar um novo ponto na trilha.")]
    [SerializeField] private float minRecordDistance = 0.05f;

    [Header("Screen / Arena Wrap (Pac-Man Style)")]
    [Tooltip("Ativa o movimento estilo Pac-Man: ao sair de um lado da tela/arena, reaparece do lado oposto.")]
    [SerializeField] private bool enableWrap = true;

    [Tooltip("Se verdadeiro, detecta automaticamente os limites da tela pela câmera ortográfica.")]
    [SerializeField] private bool useCameraBounds = true;

    [Tooltip("Câmera de referência para os limites (se vazia, utiliza a Camera.main).")]
    [SerializeField] private Camera targetCamera;

    [Tooltip("Centro da arena de jogo (usado para limites customizados ou como offset da câmera).")]
    [SerializeField] private Vector2 boundsCenter = Vector2.zero;

    [Tooltip("Dimensões manuais da arena (Largura X, Altura Y) caso não use a câmera ortográfica.")]
    [SerializeField] private Vector2 customBoundsSize = new Vector2(35.55f, 20f);

    [Tooltip("Habilita o wrap horizontal (Esquerda <-> Direita).")]
    [SerializeField] private bool wrapHorizontal = true;

    [Tooltip("Habilita o wrap vertical (Cima <-> Baixo).")]
    [SerializeField] private bool wrapVertical = true;

    [Tooltip("Margem além da borda antes de teleportar (permite que o sprite saia suavemente da tela antes de surgir do outro lado).")]
    [SerializeField] private float wrapPadding = 0.5f;

    [Header("Input")]
    [Tooltip("Referência ao InputController (se não definido, busca no mesmo GameObject ou usa Input clássico).")]
    [SerializeField] private InputController inputController;

    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;

    // Lista de segmentos ativos
    private readonly List<SnakeSegment> segments = new List<SnakeSegment>();

    // Histórico de pontos percorridos pela cabeça
    private readonly List<PathPoint> pathHistory = new List<PathPoint>();

    // Cache interno de limites
    private Vector2 currentBoundsCenter;
    private Vector2 currentEffectiveHalfSize;
    private Vector2 currentEffectiveSize;
    private Rigidbody2D rb2d;

    public static SnakeMovement Instance { get; private set; }

    public IReadOnlyList<SnakeSegment> Segments => segments;
    public Vector3 HeadPosition => transform.position;
    public float MoveSpeed { get => moveSpeed; set => moveSpeed = value; }
    public float SteerSpeed { get => steerSpeed; set => steerSpeed = value; }
    public float SegmentSpacing { get => segmentSpacing; set => segmentSpacing = Mathf.Max(0.1f, value); }
    public bool EnableWrap { get => enableWrap; set => enableWrap = value; }
    public bool UseCameraBounds { get => useCameraBounds; set => useCameraBounds = value; }
    public Vector2 CustomBoundsSize { get => customBoundsSize; set => customBoundsSize = value; }
    public Vector2 BoundsCenter { get => boundsCenter; set => boundsCenter = value; }
    public float WrapPadding { get => wrapPadding; set => wrapPadding = Mathf.Max(0f, value); }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (inputController == null)
        {
            inputController = GetComponent<InputController>();
        }

        rb2d = GetComponent<Rigidbody2D>();
        if (rb2d != null)
        {
            rb2d.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        UpdateBoundsData();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        UpdateBoundsData();
        InitializePathHistory();
        SpawnInitialSegments();
    }

    private void FixedUpdate()
    {
        UpdateBoundsData();
        HandleSteering();
        HandleForwardMovement();
        UpdatePathHistory();
        UpdateSegmentsPositions();
    }

    /// <summary>
    /// Recalcula as dimensões ativas da tela/arena levando em conta câmera e padding.
    /// </summary>
    public void UpdateBoundsData()
    {
        if (!enableWrap) return;

        if (useCameraBounds)
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera != null && targetCamera.orthographic)
            {
                float halfH = targetCamera.orthographicSize;
                float halfW = halfH * targetCamera.aspect;
                currentBoundsCenter = (Vector2)targetCamera.transform.position + boundsCenter;
                currentEffectiveHalfSize = new Vector2(halfW + wrapPadding, halfH + wrapPadding);
            }
            else
            {
                currentBoundsCenter = boundsCenter;
                currentEffectiveHalfSize = (customBoundsSize * 0.5f) + new Vector2(wrapPadding, wrapPadding);
            }
        }
        else
        {
            currentBoundsCenter = boundsCenter;
            currentEffectiveHalfSize = (customBoundsSize * 0.5f) + new Vector2(wrapPadding, wrapPadding);
        }

        currentEffectiveSize = currentEffectiveHalfSize * 2f;
    }

    /// <summary>
    /// Aplica a lógica toroidal (Pac-Man) a uma coordenada dentro de [min, max].
    /// </summary>
    public Vector3 WrapPosition(Vector3 pos)
    {
        if (!enableWrap) return pos;

        float x = pos.x;
        float y = pos.y;

        if (wrapHorizontal && currentEffectiveSize.x > 0.001f)
        {
            float minX = currentBoundsCenter.x - currentEffectiveHalfSize.x;
            float maxX = currentBoundsCenter.x + currentEffectiveHalfSize.x;
            float width = currentEffectiveSize.x;

            while (x > maxX) x -= width;
            while (x < minX) x += width;
        }

        if (wrapVertical && currentEffectiveSize.y > 0.001f)
        {
            float minY = currentBoundsCenter.y - currentEffectiveHalfSize.y;
            float maxY = currentBoundsCenter.y + currentEffectiveHalfSize.y;
            float height = currentEffectiveSize.y;

            while (y > maxY) y -= height;
            while (y < minY) y += height;
        }

        return new Vector3(x, y, pos.z);
    }

    /// <summary>
    /// Calcula o vetor de deslocamento mínimo de 'from' até 'to' considerando a periodicidade toroidal.
    /// Evita que cálculos de interpolação atravessem a arena inteira em linha reta no frame do wrap.
    /// </summary>
    public Vector3 GetToroidalDelta(Vector3 from, Vector3 to)
    {
        float dx = to.x - from.x;
        float dy = to.y - from.y;
        float dz = to.z - from.z;

        if (enableWrap)
        {
            if (wrapHorizontal && currentEffectiveSize.x > 0.001f)
            {
                float w = currentEffectiveSize.x;
                dx -= Mathf.Round(dx / w) * w;
            }

            if (wrapVertical && currentEffectiveSize.y > 0.001f)
            {
                float h = currentEffectiveSize.y;
                dy -= Mathf.Round(dy / h) * h;
            }
        }

        return new Vector3(dx, dy, dz);
    }

    /// <summary>
    /// Distância mínima considerando o espaço toroidal.
    /// </summary>
    public float GetToroidalDistance(Vector3 from, Vector3 to)
    {
        return GetToroidalDelta(from, to).magnitude;
    }

    /// <summary>
    /// Pré-popula a trilha para trás da cabeça para que os segmentos iniciais
    /// já comecem alinhados e espaçados perfeitamente no frame 1.
    /// </summary>
    private void InitializePathHistory()
    {
        pathHistory.Clear();

        float requiredLength = (initialSegments + 2) * segmentSpacing;
        int steps = Mathf.CeilToInt(requiredLength / minRecordDistance);

        Vector3 backwardDir = -transform.up;

        for (int i = 0; i <= steps; i++)
        {
            Vector3 pos = transform.position + (backwardDir * (i * minRecordDistance));
            if (enableWrap)
            {
                pos = WrapPosition(pos);
            }
            pathHistory.Add(new PathPoint(pos, transform.rotation));
        }
    }

    /// <summary>
    /// Instancia a quantidade inicial de segmentos configurada.
    /// </summary>
    private void SpawnInitialSegments()
    {
        for (int i = 0; i < initialSegments; i++)
        {
            AddSegment();
        }
    }

    /// <summary>
    /// Lê a entrada do jogador (A/D, setas ou gamepad) e gira a cabeça suavemente no plano 2D.
    /// </summary>
    private void HandleSteering()
    {
        float steerInput = 0f;

        if (inputController != null)
        {
            steerInput = inputController.Movement.x;
        }

        if (!Mathf.Approximately(steerInput, 0f))
        {
            // Input positivo (D / Seta Direita) gira no sentido horário (-Z)
            // Input negativo (A / Seta Esquerda) gira no sentido anti-horário (+Z)
            float rotationAmount = -steerInput * steerSpeed * Time.fixedDeltaTime;
            float currentAngle = rb2d != null ? rb2d.rotation : transform.eulerAngles.z;
            float nextAngle = currentAngle + rotationAmount;
            if (rb2d != null)
            {
                rb2d.MoveRotation(nextAngle);
            }
            else
            {
                transform.rotation = Quaternion.Euler(0f, 0f, nextAngle);
            }
        }
    }

    /// <summary>
    /// Move a cabeça continuamente na direção para onde está apontando (transform.up)
    /// e aplica o wrap no estilo Pac-Man caso alcance a borda da arena.
    /// </summary>
    private void HandleForwardMovement()
    {
        Vector2 currentPosition = rb2d != null ? rb2d.position : (Vector2)transform.position;
        float angle = rb2d != null ? rb2d.rotation : transform.eulerAngles.z;
        Vector2 forward = Quaternion.Euler(0f, 0f, angle) * Vector2.up;
        Vector3 nextPosition = currentPosition + (forward * (moveSpeed * Time.fixedDeltaTime));

        if (enableWrap)
        {
            nextPosition = WrapPosition(nextPosition);
        }

        if (rb2d != null)
        {
            rb2d.MovePosition(nextPosition);
        }
        else
        {
            transform.position = nextPosition;
        }
    }

    /// <summary>
    /// Atualiza o histórico de posições com a posição atual da cabeça.
    /// Utiliza distância toroidal para não registrar distâncias falsas durante a travessia de borda.
    /// </summary>
    private void UpdatePathHistory()
    {
        if (pathHistory.Count == 0)
        {
            pathHistory.Add(new PathPoint(transform.position, transform.rotation));
            return;
        }

        Vector3 headPosition = rb2d != null ? (Vector3)rb2d.position : transform.position;
        Quaternion headRotation = rb2d != null
            ? Quaternion.Euler(0f, 0f, rb2d.rotation)
            : transform.rotation;
        float distFromFirst = GetToroidalDistance(pathHistory[0].position, headPosition);

        if (distFromFirst >= minRecordDistance)
        {
            pathHistory.Insert(0, new PathPoint(headPosition, headRotation));
        }
        else
        {
            // Atualiza o primeiro ponto para a posição exata da cabeça a cada frame
            pathHistory[0] = new PathPoint(headPosition, headRotation);
        }
    }

    /// <summary>
    /// Percorre o histórico de pontos e posiciona cada segmento na distância exata ao longo da trilha.
    /// Trata a continuidade das bordas de forma que cada segmento saia por uma borda e surja
    /// na oposta suavemente e de maneira independente.
    /// </summary>
    private void UpdateSegmentsPositions()
    {
        if (segments.Count == 0 || pathHistory.Count < 2) return;

        int currentSegmentIdx = 0;
        int placedCount = 0;
        float targetDistance = segmentSpacing;
        float accumulatedDistance = 0f;

        int pointsCount = pathHistory.Count;
        int lastUsedPointIndex = 0;

        for (int i = 0; i < pointsCount - 1; i++)
        {
            PathPoint pA = pathHistory[i];
            PathPoint pB = pathHistory[i + 1];

            Vector3 delta = GetToroidalDelta(pA.position, pB.position);
            float stepDist = delta.magnitude;

            while (accumulatedDistance + stepDist >= targetDistance)
            {
                float excess = targetDistance - accumulatedDistance;
                float t = stepDist > 0.0001f ? (excess / stepDist) : 0f;

                Vector3 targetPos = pA.position + (delta * t);
                if (enableWrap)
                {
                    targetPos = WrapPosition(targetPos);
                }

                Quaternion targetRot = Quaternion.Slerp(pA.rotation, pB.rotation, t);

                segments[currentSegmentIdx].UpdatePositionAndRotation(targetPos, targetRot);

                currentSegmentIdx++;
                placedCount++;
                lastUsedPointIndex = i + 1;

                if (currentSegmentIdx >= segments.Count)
                {
                    break;
                }

                targetDistance += segmentSpacing;
            }

            if (currentSegmentIdx >= segments.Count)
            {
                break;
            }

            accumulatedDistance += stepDist;
        }

        // Se ainda restarem segmentos sem posição atingida (fim da trilha), posiciona no último ponto
        while (currentSegmentIdx < segments.Count)
        {
            PathPoint lastPoint = pathHistory[pathHistory.Count - 1];
            Vector3 finalPos = enableWrap ? WrapPosition(lastPoint.position) : lastPoint.position;
            segments[currentSegmentIdx].UpdatePositionAndRotation(finalPos, lastPoint.rotation);
            currentSegmentIdx++;
        }

        // Otimização: remove pontos antigos da lista SOMENTE se todos os segmentos tiverem sido posicionados
        // e mantém uma margem de segurança generosa para acomodar novos segmentos e curvas.
        // Nunca remove pontos se algum segmento ainda não couber na trilha!
        if (placedCount >= segments.Count && lastUsedPointIndex > 0)
        {
            int safeBuffer = Mathf.CeilToInt((segmentSpacing * 3f) / minRecordDistance);
            int trimIndex = lastUsedPointIndex + safeBuffer;
            if (trimIndex < pathHistory.Count - 1)
            {
                pathHistory.RemoveRange(trimIndex, pathHistory.Count - trimIndex);
            }
        }
    }

    /// <summary>
    /// Garante que o histórico da trilha tenha comprimento suficiente para posicionar todos os segmentos.
    /// Chamado ao adicionar novos segmentos à cobra em tempo de execução.
    /// </summary>
    private void EnsurePathCapacityForSegments()
    {
        if (pathHistory.Count < 2) return;

        float currentLength = 0f;
        for (int i = 0; i < pathHistory.Count - 1; i++)
        {
            currentLength += GetToroidalDelta(pathHistory[i].position, pathHistory[i + 1].position).magnitude;
        }

        float requiredLength = (segments.Count + 3) * segmentSpacing;
        if (currentLength < requiredLength)
        {
            float deficit = requiredLength - currentLength;
            int steps = Mathf.CeilToInt(deficit / minRecordDistance);

            PathPoint lastPt = pathHistory[pathHistory.Count - 1];
            PathPoint prevPt = pathHistory[pathHistory.Count - 2];
            Vector3 dir = GetToroidalDelta(prevPt.position, lastPt.position).normalized;
            if (dir.sqrMagnitude < 0.001f)
            {
                dir = -(lastPt.rotation * Vector3.up);
            }

            Vector3 currentPos = lastPt.position;
            for (int s = 0; s < steps; s++)
            {
                currentPos += dir * minRecordDistance;
                if (enableWrap)
                {
                    currentPos = WrapPosition(currentPos);
                }
                pathHistory.Add(new PathPoint(currentPos, lastPt.rotation));
            }
        }
    }

    /// <summary>
    /// Adiciona um novo segmento padrão ao final da cobra.
    /// </summary>
    public SnakeSegment AddSegment()
    {
        return AddSegment(segmentPrefab, null);
    }

    /// <summary>
    /// Adiciona um segmento com prefab específico e cor personalizada ao final da cobra.
    /// </summary>
    public SnakeSegment AddSegment(SnakeSegment prefab, Color? segmentColor = null)
    {
        int newIndex = segments.Count;
        SnakeSegment newSegment;

        EnsurePathCapacityForSegments();

        Vector3 spawnPos;
        Quaternion spawnRot;

        if (pathHistory.Count > 0)
        {
            PathPoint tailPt = pathHistory[pathHistory.Count - 1];
            spawnPos = tailPt.position;
            spawnRot = tailPt.rotation;
        }
        else
        {
            spawnPos = transform.position - (transform.up * ((newIndex + 1) * segmentSpacing));
            if (enableWrap)
            {
                spawnPos = WrapPosition(spawnPos);
            }
            spawnRot = transform.rotation;
        }

        SnakeSegment prefabToInstantiate = prefab != null ? prefab : segmentPrefab;

        if (prefabToInstantiate != null)
        {
            newSegment = Instantiate(prefabToInstantiate, spawnPos, spawnRot, transform.parent);
        }
        else
        {
            // Fallback caso nenhum prefab tenha sido configurado no Inspector
            GameObject segmentGo = new GameObject($"Segment_{newIndex}");
            segmentGo.transform.SetPositionAndRotation(spawnPos, spawnRot);
            if (transform.parent != null)
            {
                segmentGo.transform.SetParent(transform.parent);
            }
            newSegment = segmentGo.AddComponent<SnakeSegment>();
        }

        newSegment.Initialize(newIndex, this);

        if (segmentColor.HasValue)
        {
            newSegment.SetSegmentColor(segmentColor.Value);
        }

        if (UpgradeManager.Instance != null && UpgradeManager.Instance.BonusMaxHealth > 0f)
        {
            SegmentHealth health = newSegment.GetComponent<SegmentHealth>();
            if (health != null)
            {
                health.IncreaseMaxHealth(UpgradeManager.Instance.BonusMaxHealth, true);
            }
        }

        segments.Add(newSegment);
        return newSegment;
    }

    /// <summary>
    /// Remove um segmento específico da cobra e reindexa os restantes.
    /// </summary>
    public void RemoveSegment(int index)
    {
        if (index < 0 || index >= segments.Count) return;

        SnakeSegment seg = segments[index];
        segments.RemoveAt(index);

        if (seg != null && seg.gameObject != null)
        {
            Destroy(seg.gameObject);
        }

        // Reindexa os segmentos posteriores
        for (int i = index; i < segments.Count; i++)
        {
            if (segments[i] != null)
            {
                segments[i].SetIndex(i);
            }
        }
    }

    /// <summary>
    /// Remove um segmento específico a partir de sua instância.
    /// O corpo da cobra se reajusta automaticamente nos frames seguintes.
    /// </summary>
    public void RemoveSegment(SnakeSegment segment)
    {
        if (segment == null) return;
        int index = segments.IndexOf(segment);
        if (index >= 0)
        {
            RemoveSegment(index);
        }
    }

    /// <summary>
    /// Retorna a posição mais próxima da cobra (cabeça ou segmento) em relação a um ponto do cenário.
    /// </summary>
    public Vector3 GetNearestTargetPosition(Vector3 fromPosition)
    {
        Vector3 nearest = transform.position;
        float shortestDistSqr = (transform.position - fromPosition).sqrMagnitude;

        for (int i = 0; i < segments.Count; i++)
        {
            if (segments[i] != null)
            {
                Vector3 segPos = segments[i].transform.position;
                float distSqr = (segPos - fromPosition).sqrMagnitude;
                if (distSqr < shortestDistSqr)
                {
                    shortestDistSqr = distSqr;
                    nearest = segPos;
                }
            }
        }

        return nearest;
    }

    /// <summary>
    /// Remove o último segmento da cobra (cauda).
    /// </summary>
    public void RemoveLastSegment()
    {
        if (segments.Count > 0)
        {
            RemoveSegment(segments.Count - 1);
        }
    }

    /// <summary>
    /// Remove todos os segmentos existentes.
    /// </summary>
    public void ClearSegments()
    {
        for (int i = segments.Count - 1; i >= 0; i--)
        {
            if (segments[i] != null && segments[i].gameObject != null)
            {
                Destroy(segments[i].gameObject);
            }
        }
        segments.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        // Desenha os limites da tela / arena de wrap (Pac-Man)
        if (enableWrap)
        {
            Gizmos.color = new Color(0.2f, 1f, 0.6f, 0.75f);
            Vector2 center = boundsCenter;
            Vector2 size = customBoundsSize + new Vector2(wrapPadding * 2f, wrapPadding * 2f);

            if (useCameraBounds)
            {
                Camera cam = targetCamera != null ? targetCamera : Camera.main;
                if (cam != null && cam.orthographic)
                {
                    float h = (cam.orthographicSize + wrapPadding) * 2f;
                    float w = ((cam.orthographicSize * cam.aspect) + wrapPadding) * 2f;
                    center = (Vector2)cam.transform.position + boundsCenter;
                    size = new Vector2(w, h);
                }
            }

            Gizmos.DrawWireCube(new Vector3(center.x, center.y, 0f), new Vector3(size.x, size.y, 0.1f));
        }

        // Desenha a direção da cabeça
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.up * 1.2f);

        // Desenha a linha conectando a cabeça aos segmentos
        Gizmos.color = Color.yellow;
        Vector3 previousPos = transform.position;

        for (int i = 0; i < segments.Count; i++)
        {
            if (segments[i] != null)
            {
                Vector3 segPos = segments[i].transform.position;
                // Evita desenhar linhas cruzando a tela inteira quando segmentos estão em lados opostos
                if (!enableWrap || Vector3.Distance(previousPos, segPos) < (segmentSpacing * 3f))
                {
                    Gizmos.DrawLine(previousPos, segPos);
                }
                Gizmos.DrawWireSphere(segPos, 0.2f);
                previousPos = segPos;
            }
        }

        // Desenha os pontos registrados na trilha histórica
        if (pathHistory != null && pathHistory.Count > 1)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.4f);
            for (int i = 0; i < pathHistory.Count - 1; i++)
            {
                Vector3 p1 = pathHistory[i].position;
                Vector3 p2 = pathHistory[i + 1].position;
                if (!enableWrap || Vector3.Distance(p1, p2) < (minRecordDistance * 3.5f))
                {
                    Gizmos.DrawLine(p1, p2);
                }
            }
        }
    }
}
