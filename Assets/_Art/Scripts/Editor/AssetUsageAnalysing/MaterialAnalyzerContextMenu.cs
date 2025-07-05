using UnityEngine;
using UnityEditor;

public class MaterialAnalyzerContextMenu
{
    [MenuItem("GameObject/Analyze Materials", false, 49)]
    static void AnalyzeSelectedGameObjects()
    {
        // Open the analyzer window
        MaterialAnalyzerWindow window = EditorWindow.GetWindow<MaterialAnalyzerWindow>("Material Analyzer");
        window.minSize = new Vector2(500, 300);
        window.Show();

        // Focus on materials tab and filter to show only the selected objects
        window.AnalyzeSelectedGameObjects();
    }

    [MenuItem("GameObject/Analyze Materials", true)]
    static bool ValidateAnalyzeSelectedGameObjects()
    {
        // Validate that there is at least one selected GameObject with a Renderer component
        foreach (GameObject go in Selection.gameObjects)
        {
            if (go.GetComponentInChildren<Renderer>() != null)
            {
                return true;
            }
        }

        return false;
    }

    [MenuItem("CONTEXT/Material/Analyze in Material Analyzer")]
    static void AnalyzeMaterialInAnalyzer(MenuCommand command)
    {
        Material material = command.context as Material;
        if (material == null) return;

        // Open the analyzer window
        MaterialAnalyzerWindow window = EditorWindow.GetWindow<MaterialAnalyzerWindow>("Material Analyzer");
        window.minSize = new Vector2(500, 300);
        window.Show();

        // Focus on the material
        window.FocusOnMaterial(material);
    }

    [MenuItem("CONTEXT/Renderer/Analyze Materials")]
    static void AnalyzeRendererMaterials(MenuCommand command)
    {
        Renderer renderer = command.context as Renderer;
        if (renderer == null) return;

        // Open the analyzer window
        MaterialAnalyzerWindow window = EditorWindow.GetWindow<MaterialAnalyzerWindow>("Material Analyzer");
        window.minSize = new Vector2(500, 300);
        window.Show();

        // Focus on the renderer's materials
        window.AnalyzeRenderer(renderer);
    }

    [MenuItem("Window/Analysis/Material Analyzer")]
    public static void ShowWindow()
    {
        MaterialAnalyzerWindow window = EditorWindow.GetWindow<MaterialAnalyzerWindow>("Material Analyzer");
        window.minSize = new Vector2(500, 300);
        window.Show();
    }
}
