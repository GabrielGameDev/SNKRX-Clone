using UnityEngine;

/// <summary>Calculates a short-range steering direction away from nearby enemies.</summary>
internal static class EnemySeparation
{
    private const float SeparationRadius = 1.25f;

    public static Vector2 GetDirection(Transform self)
    {
        Vector2 push = Vector2.zero;
        int neighbors = 0;
        int selfIndex = -1;
        for (int i = 0; i < EnemyHealth.AllEnemies.Count; i++)
        {
            if (EnemyHealth.AllEnemies[i] != null && EnemyHealth.AllEnemies[i].transform == self)
            {
                selfIndex = i;
                break;
            }
        }

        for (int i = 0; i < EnemyHealth.AllEnemies.Count; i++)
        {
            EnemyHealth other = EnemyHealth.AllEnemies[i];
            if (other == null || other.transform == self || other.IsDead) continue;

            Vector2 offset = (Vector2)self.position - (Vector2)other.transform.position;
            float distance = offset.magnitude;
            if (distance >= SeparationRadius) continue;

            // Give enemies spawned at the exact same point a deterministic way to separate.
            if (distance < 0.001f)
            {
                int pairHash = selfIndex * 397 ^ i;
                float angle = Mathf.Abs(pairHash % 360) * Mathf.Deg2Rad;
                if (selfIndex > i) angle += Mathf.PI;
                offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                distance = 0.001f;
            }

            push += offset.normalized * (1f - distance / SeparationRadius);
            neighbors++;
        }

        return neighbors == 0 ? Vector2.zero : push.normalized;
    }
}
