using UnityEditor;
using UnityEngine;

public class RenameTool
{
    private string prefix = "Prefix";
    private string name = "AssetName";
    private string suffix = "01";
    private bool incrementNumber = true;
    private bool removeUnderscore = false;

    public void OnGUI()
    {
        GUILayout.Label("Rename Tool", EditorStyles.boldLabel);

        prefix = EditorGUILayout.TextField("Prefix", prefix);
        name = EditorGUILayout.TextField("AssetName", name);
        suffix = EditorGUILayout.TextField("Suffix (numbers only)", suffix);
        incrementNumber = EditorGUILayout.Toggle("Increment Number", incrementNumber);
        removeUnderscore = EditorGUILayout.Toggle("Remove Underscores", removeUnderscore);

        if (GUILayout.Button("Rename Selected Objects"))
        {
            RenameSelectedObjects();
        }
    }

    private void RenameSelectedObjects()
    {
        if (Selection.objects.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", "No objects or assets selected.", "OK");
            return;
        }

        if (!int.TryParse(suffix, out int startingNumber))
        {
            return;
        }

        foreach (Object obj in Selection.objects)
        {
            string newName = $"{prefix}_" + name;

            if (incrementNumber)
            {
                newName += $"_{startingNumber:D3}";
            }

            if (removeUnderscore)
            {
                newName = newName.Replace("_", "");
            }

            string assetPath = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(assetPath))
            {
                string result = AssetDatabase.RenameAsset(assetPath, newName);
            }

            if (obj is GameObject)
            {
                ((GameObject)obj).name = newName;
            }

            startingNumber++;
        }

        AssetDatabase.SaveAssets();
    }
}
