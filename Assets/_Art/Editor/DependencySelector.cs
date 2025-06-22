using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class DependencySelector : Editor
{
    [MenuItem("CT/Select Dependencies in Assets Folder (Exclude _KikRush)", false, 20)]
    public static void SelectDependenciesInAssetsFolderExcludingKikRush()
    {

        var selectedObjects = Selection.objects;

        List<string> selectedPaths = new List<string>();
        foreach (var obj in selectedObjects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path))
                selectedPaths.Add(path);
        }

        string[] dependencies = AssetDatabase.GetDependencies(selectedPaths.ToArray(), true);

        List<Object> filteredDependencies = new List<Object>();
        foreach (var dependencyPath in dependencies)
        {
            if (dependencyPath.StartsWith("Assets/") && !dependencyPath.StartsWith("Assets/_KikRush"))
            {
                var dependency = AssetDatabase.LoadAssetAtPath<Object>(dependencyPath);
                if (dependency != null)
                    filteredDependencies.Add(dependency);
            }
        }

        Selection.objects = filteredDependencies.ToArray();
    }
}