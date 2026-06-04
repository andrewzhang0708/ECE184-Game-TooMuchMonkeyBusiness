using UnityEngine;

[ExecuteAlways]
public class BlueVortexSetup : MonoBehaviour
{
    [Header("Vortex Shape")]
    public float radius = 1.2f;
    public int maxParticles = 300;

    [Header("Particle Motion")]
    public float lifetime = 1.2f;
    public float startSpeed = 0.25f;
    public float radialInwardSpeed = -0.8f;
    public float orbitalSpeed = 4.5f;
    public float rotationSpeed = 240f;

    [Header("Particle Look")]
    public float startSize = 0.16f;
    public float emissionRate = 85f;

    [Header("Optional Overall Rotation")]
    public bool rotateWholeVortex = true;
    public float wholeRotationSpeed = 80f;

    private ParticleSystem ps;

    [ContextMenu("Setup Blue Vortex")]
    public void SetupBlueVortex()
    {
        ps = GetComponentInChildren<ParticleSystem>();

        if (ps == null)
        {
            GameObject child = new GameObject("BlueVortex_ParticleSystem");
            child.transform.SetParent(transform);
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;

            ps = child.AddComponent<ParticleSystem>();
        }

        var main = ps.main;
        main.loop = true;
        main.duration = 2f;
        main.startLifetime = lifetime;
        main.startSpeed = startSpeed;
        main.startSize = startSize;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new Color(0.04f, 0.12f, 0.45f, 1f);
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = maxParticles;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = emissionRate;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;
        shape.arc = 360f;
        shape.radiusThickness = 0.05f; // closer to edge emission

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.radial = new ParticleSystem.MinMaxCurve(radialInwardSpeed);
        velocity.orbitalZ = new ParticleSystem.MinMaxCurve(orbitalSpeed);

        var color = ps.colorOverLifetime;
        color.enabled = true;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.02f, 0.05f, 0.20f), 0.00f),
                new GradientColorKey(new Color(0.05f, 0.25f, 1.00f), 0.35f),
                new GradientColorKey(new Color(0.15f, 0.85f, 1.00f), 0.70f),
                new GradientColorKey(new Color(0.01f, 0.02f, 0.08f), 1.00f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.0f, 0.00f),
                new GradientAlphaKey(0.85f, 0.25f),
                new GradientAlphaKey(0.95f, 0.65f),
                new GradientAlphaKey(0.0f, 1.00f)
            }
        );
        color.color = new ParticleSystem.MinMaxGradient(gradient);

        var size = ps.sizeOverLifetime;
        size.enabled = true;

        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.25f);
        sizeCurve.AddKey(0.45f, 1.0f);
        sizeCurve.AddKey(1f, 0.0f);
        size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var rotation = ps.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(rotationSpeed * Mathf.Deg2Rad);

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 20;

        Material mat = CreateParticleMaterial();
        if (mat != null)
        {
            renderer.material = mat;
        }

        ps.Play();
    }

    private Material CreateParticleMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");

        if (shader == null)
        {
            shader = Shader.Find("Particles/Standard Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            Debug.LogWarning("Could not find a suitable particle shader.");
            return null;
        }

        Material mat = new Material(shader);
        mat.name = "M_BlueVortex_Particles";

        // These property names differ by shader, so set what exists.
        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", new Color(0.05f, 0.35f, 1f, 0.8f));
        }

        if (mat.HasProperty("_Color"))
        {
            mat.SetColor("_Color", new Color(0.05f, 0.35f, 1f, 0.8f));
        }

        return mat;
    }

    private void Update()
    {
        if (!Application.isPlaying) return;

        if (rotateWholeVortex)
        {
            transform.Rotate(0f, 0f, wholeRotationSpeed * Time.deltaTime);
        }
    }

    private void Reset()
    {
        SetupBlueVortex();
    }
}