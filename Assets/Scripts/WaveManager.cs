using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gerencia o ciclo infinito de ondas de inimigos.
/// Cada onda aumenta a quantidade de inimigos, a vida deles e adiciona novos tipos de inimigos.
/// </summary>
public class WaveManager : MonoBehaviour
{
    [Header("Configurações")]
    [Tooltip("Asset ScriptableObject contendo parâmetros de dificuldade.")]
    [SerializeField] private WaveDifficultyConfig config;

    [Header("Referências de Prefabs")]
    [Tooltip("Prefab do inimigo básico (Wanderer). Deve estar na pasta Enemies.")]
    [SerializeField] private GameObject wandererPrefab;
    [Tooltip("Prefab do Chaser (persegue a cobra).")]
    [SerializeField] private GameObject chaserPrefab;
    [Tooltip("Prefab do Kamikaze (explosivo).")]
    [SerializeField] private GameObject kamikazePrefab;
    [Tooltip("Prefab do Tank (mais resistente e dispara projéteis).")]
    [SerializeField] private GameObject tankPrefab;

    [Header("Spawner")]
    [Tooltip("Componente que cuida do efeito visual de spawn e instancia o inimigo.")]
    [SerializeField] private EnemySpawner spawner;

    private int currentWave = 0;

    private void Start()
    {
        if (config == null)
        {
            Debug.LogError("WaveDifficultyConfig não atribuído ao WaveManager.");
            return;
        }
        StartCoroutine(WaveLoop());
    }

    private IEnumerator WaveLoop()
    {
        while (true)
        {
            currentWave++;
            GameEvents.TriggerWaveStarted(currentWave);

            // calcula quantos inimigos e o multiplicador de vida
            int enemyCount = Mathf.Min(config.maxEnemiesPerWave,
                config.baseEnemyCount + Mathf.FloorToInt(currentWave * config.countScaleFactor));
            float healthMultiplier = 1f + (currentWave - 1) * config.healthScalePerWave;

            // gera a lista dos prefabs que vão aparecer nesta onda
            List<GameObject> wavePrefabs = BuildWavePrefabList(enemyCount);

            // spawna os inimigos um a um
            foreach (var prefab in wavePrefabs)
            {
                // escolhe posição aleatória dentro da arena
                Vector3 spawnPos = GetRandomSpawnPosition();
                spawner.SpawnEnemy(prefab, spawnPos, healthMultiplier);
                yield return new WaitForSeconds(config.timeBetweenSpawns);
            }

            // Aguarda até que:
            //   1. Todos os marcadores de spawn tenham terminado (PendingSpawns == 0)
            //   2. Todos os inimigos da onda tenham morrido (AllEnemies.Count == 0)
            // Só então avança para a próxima onda.
            while (spawner.PendingSpawns > 0 || EnemyHealth.AllEnemies.Count > 0)
            {
                yield return null;
            }

            GameEvents.TriggerWaveCompleted(currentWave);
            yield return new WaitForSeconds(config.timeBetweenWaves);
        }
    }

    /// <summary>
    /// Cria uma lista de prefabs da quantidade solicitada respeitando pesos e desbloqueios.
    /// </summary>
    private List<GameObject> BuildWavePrefabList(int count)
    {
        // define pesos base (só valores positivos) – podem ser ajustados futuramente
        var weights = new Dictionary<GameObject, float>();
        if (wandererPrefab != null) weights[wandererPrefab] = 60f;
        if (currentWave >= config.chaserUnlockWave && chaserPrefab != null) weights[chaserPrefab] = 30f;
        if (currentWave >= config.kamikazeUnlockWave && kamikazePrefab != null) weights[kamikazePrefab] = 20f;
        if (currentWave >= config.tankUnlockWave && tankPrefab != null) weights[tankPrefab] = 10f;

        // normaliza pesos em relação à onda – aumenta pesos ligeiramente conforme a onda avança
        var adjustedWeights = new Dictionary<GameObject, float>();
        foreach (var kvp in weights)
        {
            // aumenta 2% por onda como exemplo simples
            float adjusted = kvp.Value * (1f + (currentWave - 1) * 0.02f);
            adjustedWeights[kvp.Key] = adjusted;
        }

        // cria a lista aleatória usando escolha ponderada
        var result = new List<GameObject>();
        var rnd = new System.Random();
        for (int i = 0; i < count; i++)
        {
            result.Add(WeightedRandomChoice(adjustedWeights, rnd));
        }
        return result;
    }

    private GameObject WeightedRandomChoice(Dictionary<GameObject, float> weightMap, System.Random rnd)
    {
        float total = 0f;
        foreach (var w in weightMap.Values) total += w;
        float roll = (float)rnd.NextDouble() * total;
        float cumulative = 0f;
        foreach (var kvp in weightMap)
        {
            cumulative += kvp.Value;
            if (roll <= cumulative) return kvp.Key;
        }
        // fallback (não deveria acontecer)
        foreach (var kvp in weightMap) return kvp.Key;
        return null;
    }

    /// <summary>
    /// Retorna uma posição aleatória dentro da arena, mantendo margem de segurança e distância mínima da cobra.
    /// </summary>
    private Vector3 GetRandomSpawnPosition()
    {
        Bounds2D bounds = ArenaBounds.GetBounds(margin: 1.5f);
        Vector3 pos = Vector3.zero;
        int attempts = 0;
        const int maxAttempts = 10;
        while (attempts < maxAttempts)
        {
            float x = Random.Range(bounds.minX, bounds.maxX);
            float y = Random.Range(bounds.minY, bounds.maxY);
            pos = new Vector3(x, y, 0f);
            // garante que não vai spawnar muito próximo da cabeça da cobra
            if (SnakeMovement.Instance != null)
            {
                if (Vector2.Distance(pos, SnakeMovement.Instance.HeadPosition) > 3f)
                    break;
            }
            attempts++;
        }
        return pos;
    }
}
