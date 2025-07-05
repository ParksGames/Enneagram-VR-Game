using UnityEngine;
using UnityEditor;

public class PrefabReplacerEditor : EditorWindow
{
    GameObject prefabToReplaceWith;

    [MenuItem("CT/Prefab Replacer")]
    public static void ShowWindow()
    {
        GetWindow<PrefabReplacerEditor>("Prefab Replacer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Replace selected prefabs in the scene", EditorStyles.boldLabel);

        prefabToReplaceWith = (GameObject)EditorGUILayout.ObjectField("Prefab to use:", prefabToReplaceWith, typeof(GameObject), false);

        if (GUILayout.Button("Replace Prefabs"))
        {
            ReplaceSelectedPrefabs();
        }
    }

    private void ReplaceSelectedPrefabs()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            GameObject newObject = (GameObject)PrefabUtility.InstantiatePrefab(prefabToReplaceWith);
            Undo.RegisterCreatedObjectUndo(newObject, "Create " + newObject.name);
            newObject.transform.SetParent(go.transform.parent, true);
            newObject.transform.localPosition = go.transform.localPosition;
            newObject.transform.localRotation = go.transform.localRotation;
            newObject.transform.localScale = go.transform.localScale;
            Undo.DestroyObjectImmediate(go);
        }
    }
}
