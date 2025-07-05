// SceneDependencyUtility.cs
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;
using System.Linq;

public static class SceneDependencyUtility
{
    [MenuItem("CT/Version Control/List Dependencies of Active Scene")]
    private static void ListDependencies()
    {
        // 1. Make sure there’s an open, saved scene
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid() || string.IsNullOrEmpty(scene.path))
        {
            Debug.LogError("Open & save a scene first.");
            return;
        }

        // 2. Get every asset the scene needs
        string[] paths = AssetDatabase.GetDependencies(scene.path, true)
            .Where(p => p.StartsWith("Assets/")) // ignore Packages/ etc.
            .Distinct()
            .ToArray();

        // 3. Print GUID  →  path
        foreach (string path in paths)
        {
            string guid = AssetDatabase.AssetPathToGUID(path);
            Debug.Log($"{guid}  →  {path}");
        }

        // 4. Also write it to a file beside your .git folder
        string repoRoot = Directory.GetParent(Application.dataPath)!.FullName;
        string outFile  = Path.Combine(repoRoot, "SceneDependencies.txt");
        File.WriteAllLines(outFile, paths.Select(p => $"{AssetDatabase.AssetPathToGUID(p)}\t{p}"));

        Debug.Log($"Wrote {paths.Length} entries to {outFile}");
    }
}