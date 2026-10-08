using UnityEngine;

[CreateAssetMenu(menuName = "SNKRX/Wave Difficulty Config")]
public class WaveDifficultyConfig : ScriptableObject
{
    [Header("Spawn Counts")]
    [Tooltip("Número base de inimigos na primeira onda.")]
    public int baseEnemyCount = 3;
    [Tooltip("Fator de escala da quantidade por onda (ex.: 1.4 → +40% a cada onda).")]
    public float countScaleFactor = 1.4f;
    [Tooltip("Limite máximo de inimigos por onda.")]
    public int maxEnemiesPerWave = 15;

    [Header("Health Scaling")]
    [Tooltip("Multiplicador de vida por onda (ex.: 0.18 → +18% a cada onda).")]
    public float healthScalePerWave = 0.18f;

    [Header("Timing")]
    [Tooltip("Tempo entre spawns individuais dentro da mesma onda.")]
    public float timeBetweenSpawns = 0.4f;
    [Tooltip("Tempo entre ondas depois que todos inimigos morrem.")]
    public float timeBetweenWaves = 1.5f;
    [Tooltip("Duração do marcador piscante antes do spawn.")]
    public float spawnMarkerDuration = 0.8f;

    [Header("Unlock Waves")]
    public int chaserUnlockWave = 3;
    public int kamikazeUnlockWave = 6;
    public int tankUnlockWave = 10;
}
