using UnityEngine;
using UnityEditor;

public class ShaderMaterialReplacerEditor : EditorWindow
{
    private string shaderName = "Custom/WorldPositionRandomColorWithLighting";
    private string materialName = "Material_Name";
    private Shader targetShader;
    private Material replacementMaterial;
    private bool checkByShader = true;

    [MenuItem("CT/Replace Shader or Material")]
    public static void ShowWindow()
    {
        GetWindow<ShaderMaterialReplacerEditor>("Replace Shader or Material");
    }

    private void OnGUI()
    {
        GUILayout.Label("Replace Materials", EditorStyles.boldLabel);

        checkByShader = EditorGUILayout.Toggle("Check by Shader", checkByShader);

        if (checkByShader)
        {
            shaderName = EditorGUILayout.TextField("Shader Name", shaderName);
            targetShader = Shader.Find(shaderName);

            if (targetShader == null)
            {
                EditorGUILayout.HelpBox($"Shader '{shaderName}' not found.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Target Shader:", targetShader.name);
        }
        else
        {
            materialName = EditorGUILayout.TextField("Material Name", materialName);
        }

        replacementMaterial = (Material)EditorGUILayout.ObjectField("Replacement Material", replacementMaterial, typeof(Material), false);

        if (replacementMaterial == null)
        {
            EditorGUILayout.HelpBox("Select a replacement material.", MessageType.Info);
            return;
        }

        if (GUILayout.Button("Replace Materials in Selected Objects"))
        {
            ReplaceMaterialsInSelection();
        }
    }

    private void ReplaceMaterialsInSelection()
    {
        if (replacementMaterial == null)
        {
            return;
        }

        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            return;
        }

        int materialCount = 0;

        foreach (GameObject obj in selectedObjects)
        {
            Undo.RecordObject(obj, "Replace Shader or Material");


            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;

                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null)
                        continue;

                    if (checkByShader && materials[i].shader == targetShader)
                    {
                        Undo.RecordObject(renderer, "Replace Shader Material");
                        materials[i] = replacementMaterial;
                        materialCount++;
                    }
                    else if (!checkByShader && materials[i].name == materialName)
                    {
                        Undo.RecordObject(renderer, "Replace Material Name");
                        materials[i] = replacementMaterial;
                        materialCount++;
                    }
                }

                renderer.sharedMaterials = materials;
            }
        }

        string checkType = checkByShader ? $"shader '{shaderName}'" : $"material name '{materialName}'";
    }
}
