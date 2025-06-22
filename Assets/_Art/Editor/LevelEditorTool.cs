using UnityEngine;
using UnityEditor;

public class LevelEditorTool : EditorWindow
{
    private enum ToolTab
    {
        PrefabManager,
        MaterialReplacer,
        AdvancedTools,
        RenameTool
    }

    private ToolTab currentTab = ToolTab.PrefabManager;

    private PrefabManager prefabManager;
    private MaterialReplacer materialReplacer;
    private AdvancedTools advancedTools;
    private RenameTool renameTool;

    [MenuItem("CT/Level Editor Tool")]
    public static void ShowWindow()
    {
        GetWindow<LevelEditorTool>("Level Editor Tool");
    }

    private void OnEnable()
    {
        prefabManager = new PrefabManager();
        materialReplacer = new MaterialReplacer();
        advancedTools = new AdvancedTools();
        renameTool = new RenameTool();
    }

    private void OnGUI()
    {
        GUILayout.Label("Level Editor Tool", EditorStyles.boldLabel);

        currentTab = (ToolTab)GUILayout.Toolbar((int)currentTab,
            new string[] { "Prefab Manager", "Material Replacer", "Advanced Tools", "Rename Tool" });

        switch (currentTab)
        {
            case ToolTab.PrefabManager:
                prefabManager.OnGUI();
                break;

            case ToolTab.MaterialReplacer:
                materialReplacer.OnGUI();
                break;

            case ToolTab.AdvancedTools:
                advancedTools.OnGUI();
                break;

            case ToolTab.RenameTool:
                renameTool.OnGUI();
                break;
        }
    }

    private class PrefabManager
    {
        private string savePath = "Assets/Prefabs";
        private bool removeFromScene = false;

        public void OnGUI()
        {
            GUILayout.Label("Prefab Manager", EditorStyles.boldLabel);

            savePath = EditorGUILayout.TextField("Save Path", savePath);
            removeFromScene = EditorGUILayout.Toggle("Remove From Scene", removeFromScene);

            if (GUILayout.Button("Create Prefabs"))
            {
                CreatePrefabs();
            }
        }

        private void CreatePrefabs()
        {
            if (Selection.gameObjects.Length == 0)
            {
                EditorUtility.DisplayDialog("Error", "No objects selected in the scene.", "OK");
                return;
            }

            foreach (GameObject selectedObject in Selection.gameObjects)
            {
                string prefabName = selectedObject.name;
                string prefabPath = $"{savePath}/{prefabName}.prefab";

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(selectedObject, prefabPath);

                if (removeFromScene)
                {
                    DestroyImmediate(selectedObject);
                }
            }
        }
    }

    private class MaterialReplacer
    {
        private Material replacementMaterial;

        public void OnGUI()
        {
            GUILayout.Label("Material Replacer", EditorStyles.boldLabel);

            replacementMaterial = (Material)EditorGUILayout.ObjectField("Replacement Material", replacementMaterial,
                typeof(Material), false);

            if (GUILayout.Button("Replace Materials"))
            {
                ReplaceMaterials();
            }
        }

        private void ReplaceMaterials()
        {
            foreach (GameObject obj in Selection.gameObjects)
            {
                Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer renderer in renderers)
                {
                    renderer.sharedMaterial = replacementMaterial;
                }
            }
        }
    }

    private class AdvancedTools
    {
        private float gridSize = 1f;

        public void OnGUI()
        {
            GUILayout.Label("Advanced Tools", EditorStyles.boldLabel);

            gridSize = EditorGUILayout.FloatField("Grid Size", gridSize);

            if (GUILayout.Button("Snap to Grid"))
            {
                SnapToGrid();
            }

            if (GUILayout.Button("Distribute Objects Evenly"))
            {
                DistributeObjectsEvenly();
            }
        }

        private void SnapToGrid()
        {
            foreach (GameObject obj in Selection.gameObjects)
            {
                Vector3 position = obj.transform.position;
                obj.transform.position = new Vector3(
                    Mathf.Round(position.x / gridSize) * gridSize,
                    Mathf.Round(position.y / gridSize) * gridSize,
                    Mathf.Round(position.z / gridSize) * gridSize
                );
            }
        }

        private void DistributeObjectsEvenly()
        {
            if (Selection.gameObjects.Length < 2)
            {
                return;
            }

            GameObject[] selectedObjects = Selection.gameObjects;
            Vector3 startPosition = selectedObjects[0].transform.position;
            Vector3 endPosition = selectedObjects[selectedObjects.Length - 1].transform.position;

            float stepX = (endPosition.x - startPosition.x) / (selectedObjects.Length - 1);
            float stepY = (endPosition.y - startPosition.y) / (selectedObjects.Length - 1);
            float stepZ = (endPosition.z - startPosition.z) / (selectedObjects.Length - 1);

            for (int i = 1; i < selectedObjects.Length - 1; i++)
            {
                Vector3 newPosition = new Vector3(
                    startPosition.x + stepX * i,
                    startPosition.y + stepY * i,
                    startPosition.z + stepZ * i
                );

                selectedObjects[i].transform.position = newPosition;
            }
        }
    }
}