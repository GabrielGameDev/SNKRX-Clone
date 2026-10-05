using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controla o movimento da cabeça da cobra e coordena todos os segmentos do corpo (estilo SNKRX).
/// Utiliza um buffer de histórico de posições baseado em distância percorrida, garantindo que
/// os segmentos sigam fielmente as curvas sem cortar caminho e sem jitter.
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

    [Header("Input")]
    [Tooltip("Referência ao InputController (se não definido, busca no mesmo GameObject ou usa Input clássico).")]
    [SerializeField] private InputController inputController;

    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;

    // Lista de segmentos ativos
    private readonly List<SnakeSegment> segments = new List<SnakeSegment>();

    // Histórico de pontos percorridos pela cabeça
    private readonly List<PathPoint> pathHistory = new List<PathPoint>();

    public IReadOnlyList<SnakeSegment> Segments => segments;
    public float MoveSpeed { get => moveSpeed; set => moveSpeed = value; }
    public float SteerSpeed { get => steerSpeed; set => steerSpeed = value; }
    public float SegmentSpacing { get => segmentSpacing; set => segmentSpacing = Mathf.Max(0.1f, value); }

    private void Awake()
    {
        if (inputController == null)
        {
            inputController = GetComponent<InputController>();
        }
    }

    private void Start()
    {
        InitializePathHistory();
        SpawnInitialSegments();
    }

    private void Update()
    {
        HandleSteering();
        HandleForwardMovement();
        UpdatePathHistory();
        UpdateSegmentsPositions();
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

        // Fallback para Input horizontal caso InputController não retorne valor
        // if (Mathf.Approximately(steerInput, 0f))
        // {
        //     steerInput = Input.GetAxisRaw("Horizontal");
        // }

        if (!Mathf.Approximately(steerInput, 0f))
        {
            // Input positivo (D / Seta Direita) gira no sentido horário (-Z)
            // Input negativo (A / Seta Esquerda) gira no sentido anti-horário (+Z)
            float rotationAmount = -steerInput * steerSpeed * Time.deltaTime;
            transform.Rotate(0f, 0f, rotationAmount);
        }
    }

    /// <summary>
    /// Move a cabeça continuamente na direção para onde está apontando (transform.up).
    /// </summary>
    private void HandleForwardMovement()
    {
        transform.position += transform.up * (moveSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Atualiza o histórico de posições com a posição atual da cabeça.
    /// </summary>
    private void UpdatePathHistory()
    {
        if (pathHistory.Count == 0)
        {
            pathHistory.Add(new PathPoint(transform.position, transform.rotation));
            return;
        }

        float distFromFirst = Vector3.Distance(transform.position, pathHistory[0].position);

        if (distFromFirst >= minRecordDistance)
        {
            pathHistory.Insert(0, new PathPoint(transform.position, transform.rotation));
        }
        else
        {
            // Atualiza o primeiro ponto para a posição exata da cabeça a cada frame
            pathHistory[0] = new PathPoint(transform.position, transform.rotation);
        }
    }

    /// <summary>
    /// Percorre o histórico de pontos e posiciona cada segmento na distância exata ao longo da trilha.
    /// </summary>
    private void UpdateSegmentsPositions()
    {
        if (segments.Count == 0 || pathHistory.Count < 2) return;

        int currentSegmentIdx = 0;
        float targetDistance = segmentSpacing;
        float accumulatedDistance = 0f;

        int pointsCount = pathHistory.Count;
        int lastUsedPointIndex = 0;

        for (int i = 0; i < pointsCount - 1; i++)
        {
            PathPoint pA = pathHistory[i];
            PathPoint pB = pathHistory[i + 1];

            float stepDist = Vector3.Distance(pA.position, pB.position);

            while (accumulatedDistance + stepDist >= targetDistance)
            {
                float excess = targetDistance - accumulatedDistance;
                float t = stepDist > 0.0001f ? (excess / stepDist) : 0f;

                Vector3 targetPos = Vector3.Lerp(pA.position, pB.position, t);
                Quaternion targetRot = Quaternion.Slerp(pA.rotation, pB.rotation, t);

                segments[currentSegmentIdx].UpdatePositionAndRotation(targetPos, targetRot);

                currentSegmentIdx++;
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
            segments[currentSegmentIdx].UpdatePositionAndRotation(lastPoint.position, lastPoint.rotation);
            currentSegmentIdx++;
        }

        // Otimização: remove pontos antigos da lista que estão muito além do último segmento
        int safeBuffer = 5;
        int trimIndex = lastUsedPointIndex + safeBuffer;
        if (trimIndex < pathHistory.Count - 1)
        {
            pathHistory.RemoveRange(trimIndex, pathHistory.Count - trimIndex);
        }
    }

    /// <summary>
    /// Adiciona um novo segmento ao final da cobra.
    /// </summary>
    public SnakeSegment AddSegment()
    {
        int newIndex = segments.Count;
        SnakeSegment newSegment;

        Vector3 spawnPos = transform.position - (transform.up * ((newIndex + 1) * segmentSpacing));
        Quaternion spawnRot = transform.rotation;

        if (segmentPrefab != null)
        {
            newSegment = Instantiate(segmentPrefab, spawnPos, spawnRot, transform.parent);
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
                Gizmos.DrawLine(previousPos, segments[i].transform.position);
                Gizmos.DrawWireSphere(segments[i].transform.position, 0.2f);
                previousPos = segments[i].transform.position;
            }
        }

        // Desenha os pontos registrados na trilha histórica
        if (pathHistory != null && pathHistory.Count > 1)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.4f);
            for (int i = 0; i < pathHistory.Count - 1; i++)
            {
                Gizmos.DrawLine(pathHistory[i].position, pathHistory[i + 1].position);
            }
        }
    }
}
