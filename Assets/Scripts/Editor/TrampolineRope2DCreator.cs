using UnityEditor;
using UnityEngine;

public static class SimpleVineSwingCreator
{
    [MenuItem("GameObject/Too Much Monkey Business/Simple Vine Swing", false, 10)]
    public static void CreateVineSwing()
    {
        GameObject rope = new GameObject("Simple Vine Swing");
        Undo.RegisterCreatedObjectUndo(rope, "Create Simple Vine Swing");

        rope.transform.position = GetSpawnPosition();

        LineRenderer line = rope.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.startWidth = 0.12f;
        line.endWidth = 0.08f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = new Color(0.45f, 0.28f, 0.12f);
        line.endColor = new Color(0.45f, 0.28f, 0.12f);

        Rigidbody body = rope.AddComponent<Rigidbody>();
        body.useGravity = true;
        body.constraints = RigidbodyConstraints.FreezePositionZ;

        rope.AddComponent<HingeJoint>().axis = Vector3.forward;
        rope.AddComponent<SimpleVineSwing>();

        Selection.activeGameObject = rope;
        SceneView.lastActiveSceneView?.FrameSelected();
    }

    private static Vector3 GetSpawnPosition()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;

        if (sceneView == null)
        {
            return new Vector3(500f, 6f, 500f);
        }

        Vector3 position = sceneView.pivot;
        position.z = 500f;
        return position;
    }
}
