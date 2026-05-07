#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class ColliderUtilities
{
    [MenuItem("Tools/Fix Wall Colliders (Square) in Active Scene")]
    public static void FixSquareCollidersInScene()
    {
        int fixedCount = 0;
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.isLoaded)
        {
            Debug.LogWarning("No active scene loaded.");
            return;
        }

        GameObject[] roots = scene.GetRootGameObjects();
        foreach (var root in roots)
        {
            fixedCount += FixInHierarchy(root.transform);
        }

        Debug.Log($"ColliderUtilities: Fixed {fixedCount} colliders in scene {scene.name} (set isTrigger=false).");
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static int FixInHierarchy(Transform t)
    {
        int count = 0;
        if (t.name.ToLower().Contains("square") )
        {
            BoxCollider2D bc = t.GetComponent<BoxCollider2D>();
            if (bc != null && bc.isTrigger)
            {
                Undo.RecordObject(bc, "Fix Collider isTrigger");
                bc.isTrigger = false;
                count++;
            }
        }

        // Recurse children
        for (int i = 0; i < t.childCount; i++)
        {
            count += FixInHierarchy(t.GetChild(i));
        }

        return count;
    }
}
#endif
