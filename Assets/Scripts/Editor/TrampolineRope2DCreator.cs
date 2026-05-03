using UnityEditor;
using UnityEngine;

public static class TrampolineRope2DCreator
{
    [MenuItem("GameObject/Too Much Monkey Business/Trampoline Rope 2D", false, 10)]
    public static void CreateRope()
    {
        GameObject rope = new GameObject("Trampoline Rope 2D");
        Undo.RegisterCreatedObjectUndo(rope, "Create Trampoline Rope 2D");

        rope.transform.position = GetSpawnPosition();

        LineRenderer line = rope.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.startWidth = 0.12f;
        line.endWidth = 0.08f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = new Color(0.45f, 0.28f, 0.12f);
        line.endColor = new Color(0.45f, 0.28f, 0.12f);

        rope.AddComponent<TrampolineRope2D>();

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
