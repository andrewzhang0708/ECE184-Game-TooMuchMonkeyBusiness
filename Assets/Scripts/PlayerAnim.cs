using UnityEngine;

public class PlayerAnim : MonoBehaviour
{

    private Animator anim;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    public void SetSpeed(float speed, float walkSpeed, float runSpeed)
    {
        float weight;
        if (speed <= walkSpeed) { weight = Mathf.Lerp(0f, 0.5f, speed / walkSpeed); }
        else { weight = Mathf.Lerp(0.5f, 1f, (speed - walkSpeed) / (runSpeed - walkSpeed)); }
        anim.SetFloat(SpeedHash, weight);
    }

    // From animation events
    public void Step()
    {
        // TODO: play step sound based on material checked by a raycast
    }

    private Material GetMaterialAtHitPoint(RaycastHit hit, MeshCollider meshCollider)
    {
        Mesh mesh = meshCollider.sharedMesh;
        if (mesh == null) return null;

        if (!hit.collider.TryGetComponent(out Renderer renderer)) return null;

        Material[] materials = renderer.sharedMaterials;
        int hitTriangleIndex = hit.triangleIndex;

        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            var subMeshTopology = mesh.GetSubMesh(i);

            int startTriangle = subMeshTopology.indexStart / 3;
            int triangleCount = subMeshTopology.indexCount / 3;

            if (hitTriangleIndex >= startTriangle && hitTriangleIndex < startTriangle + triangleCount)
            {
                if (i < materials.Length)
                {
                    return materials[i]; // Return exact sub-mesh material
                }
            }
        }

        return null;
    }
}

