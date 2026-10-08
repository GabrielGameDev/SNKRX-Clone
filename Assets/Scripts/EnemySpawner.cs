using System.Collections;
using UnityEngine;

/// <summary>
/// Responsável por criar o efeito visual de spawn (marcador "X" piscante) e instanciar o inimigo.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Marker")]
    [Tooltip("Prefab contendo um SpriteRenderer com o símbolo 'X' ou similar.")]
    [SerializeField] private GameObject spawnMarkerPrefab;

    [Tooltip("Duração total do marcador piscante antes de instanciar o inimigo.")]
    [SerializeField] private float markerDuration = 0.8f;

    /// <summary>
    /// Quantidade de spawns iniciados mas cujo inimigo ainda não apareceu na cena.
    /// O WaveManager aguarda esse valor chegar a zero antes de avançar de onda.
    /// </summary>
    public int PendingSpawns { get; private set; } = 0;

    /// <summary>
    /// Inicia o processo de spawn com o marcador visual.
    /// </summary>
    /// <param name="enemyPrefab">Prefab do inimigo a ser instanciado.</param>
    /// <param name="position">Posição de spawn dentro da arena.</param>
    /// <param name="healthMultiplier">Multiplicador de vida da onda atual.</param>
    public void SpawnEnemy(GameObject enemyPrefab, Vector3 position, float healthMultiplier)
    {
        PendingSpawns++;
        StartCoroutine(SpawnEnemyRoutine(enemyPrefab, position, healthMultiplier));
    }

    private IEnumerator SpawnEnemyRoutine(GameObject enemyPrefab, Vector3 position, float healthMultiplier)
    {
        // Cria o marcador
        GameObject marker = null;
        if (spawnMarkerPrefab != null)
        {
            marker = Instantiate(spawnMarkerPrefab, position, Quaternion.identity);
        }

        // Pisca o marcador durante a duração configurada
        if (marker != null)
        {
            var sr = marker.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                float elapsed = 0f;
                bool visible = true;
                while (elapsed < markerDuration)
                {
                    sr.enabled = visible;
                    visible = !visible;
                    elapsed += 0.1f;
                    yield return new WaitForSeconds(0.1f);
                }
                sr.enabled = false;
            }
            else
            {
                yield return new WaitForSeconds(markerDuration);
            }
        }

        // Instancia o inimigo
        var enemy = Instantiate(enemyPrefab, position, Quaternion.identity);
        var health = enemy.GetComponent<EnemyHealth>();
        if (health != null)
        {
            float newMax = health.MaxHealth * healthMultiplier;
            health.SetMaxHealth(newMax);
        }

        // Remove o marcador e decrementa o contador de pendentes
        if (marker != null)
        {
            Destroy(marker);
        }

        PendingSpawns--;
    }
}

