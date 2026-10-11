using UnityEngine;

/// <summary>Builds and plays a small, non-damaging particle burst when an enemy dies.</summary>
[DisallowMultipleComponent]
public sealed class EnemyDeathParticleEffect : MonoBehaviour
{
    private static Material particleMaterial;

    private void Awake()
    {
        ParticleSystem particles = gameObject.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        Configure(particles);
        particles.Play();
    }

    private static void Configure(ParticleSystem particles)
    {
        var main = particles.main;
        main.duration = 0.42f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.24f, 0.48f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.055f, 0.12f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.96f, 0.78f), new Color(1f, 0.58f, 0.2f));
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 24;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12, 18) });

        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.045f;
        shape.arc = 360f;

        var size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.7f), new Keyframe(0.2f, 1f), new Keyframe(1f, 0f)));

        var color = particles.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.95f, 0.72f), 0f),
                new GradientColorKey(new Color(1f, 0.5f, 0.18f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.7f, 0.45f),
                new GradientAlphaKey(0f, 1f)
            });
        color.color = new ParticleSystem.MinMaxGradient(gradient);

        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 2;
        renderer.sharedMaterial = GetParticleMaterial();
    }

    private static Material GetParticleMaterial()
    {
        if (particleMaterial != null) return particleMaterial;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader != null) particleMaterial = new Material(shader);
        return particleMaterial;
    }
}
