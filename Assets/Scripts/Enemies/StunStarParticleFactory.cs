using UnityEngine;

public static class StunStarParticleFactory
{
    public static ParticleSystem Create(
        Transform parent,
        Vector3 localPosition,
        float sizeMultiplier
    )
    {
        GameObject effectObject = new GameObject("Stun Stars");
        effectObject.transform.SetParent(parent, false);
        effectObject.transform.localPosition = localPosition;
        effectObject.transform.localScale =
            Vector3.one * Mathf.Max(0.01f, sizeMultiplier);

        ParticleSystem particles = effectObject.AddComponent<ParticleSystem>();
        ParticleSystemRenderer particleRenderer =
            effectObject.GetComponent<ParticleSystemRenderer>();

        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ConfigureParticles(particles);
        ApplyStarRenderer(particleRenderer);

        return particles;
    }

    public static void ApplyStarRenderer(ParticleSystem particles)
    {
        if (particles == null)
        {
            return;
        }

        ApplyStarRenderer(particles.GetComponent<ParticleSystemRenderer>());
    }

    private static void ConfigureParticles(ParticleSystem particles)
    {
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.duration = 1f;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.25f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.22f, 0.34f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.72f, 0.05f, 1f),
            new Color(1f, 1f, 0.55f, 1f)
        );
        main.maxParticles = 16;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 8f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.65f;
        shape.radiusThickness = 1f;

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.orbitalZ = new ParticleSystem.MinMaxCurve(2.4f);

        ParticleSystem.RotationOverLifetimeModule rotation = particles.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-3f, 3f);

        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 1f, 0.65f), 0f),
                new GradientColorKey(new Color(1f, 0.55f, 0.02f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.12f),
                new GradientAlphaKey(1f, 0.72f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        color.color = gradient;

        ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
        size.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.25f),
            new Keyframe(0.2f, 1f),
            new Keyframe(0.8f, 0.85f),
            new Keyframe(1f, 0.1f)
        );
        size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
    }

    private static void ApplyStarRenderer(ParticleSystemRenderer particleRenderer)
    {
        if (particleRenderer == null)
        {
            return;
        }

        particleRenderer.renderMode = ParticleSystemRenderMode.Mesh;
        particleRenderer.mesh = CreateStarMesh();
        particleRenderer.alignment = ParticleSystemRenderSpace.View;
        particleRenderer.sortingOrder = 50;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        }

        if (shader == null)
        {
            return;
        }

        Material material = new Material(shader)
        {
            name = "Runtime Stun Star Material"
        };

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", Color.white);
        }
        else if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", Color.white);
        }

        particleRenderer.material = material;
    }

    private static Mesh CreateStarMesh()
    {
        const int pointCount = 10;
        Vector3[] vertices = new Vector3[pointCount + 1];
        Vector2[] uv = new Vector2[pointCount + 1];
        int[] triangles = new int[pointCount * 6];

        vertices[0] = Vector3.zero;
        uv[0] = new Vector2(0.5f, 0.5f);

        for (int i = 0; i < pointCount; i++)
        {
            float angle = Mathf.PI * 0.5f + i * Mathf.PI * 2f / pointCount;
            float radius = i % 2 == 0 ? 0.5f : 0.22f;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;

            vertices[i + 1] = new Vector3(x, y, 0f);
            uv[i + 1] = new Vector2(x + 0.5f, y + 0.5f);

            int triangleIndex = i * 6;
            triangles[triangleIndex] = 0;
            triangles[triangleIndex + 1] = i + 1;
            triangles[triangleIndex + 2] = (i + 1) % pointCount + 1;
            triangles[triangleIndex + 3] = 0;
            triangles[triangleIndex + 4] = (i + 1) % pointCount + 1;
            triangles[triangleIndex + 5] = i + 1;
        }

        Mesh mesh = new Mesh
        {
            name = "Runtime Stun Star"
        };
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
