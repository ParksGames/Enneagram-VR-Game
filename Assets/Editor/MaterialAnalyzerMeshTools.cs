using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

public static class MaterialAnalyzerMeshTools
{
    [MenuItem("Assets/Material Analyzer/Analyze Selected Meshes")]
    public static void AnalyzeSelectedMeshes()
    {
        List<Mesh> meshes = new List<Mesh>();
        long totalVertices = 0;
        long totalTriangles = 0;
        long totalMemory = 0;

        foreach (Object selectedObject in Selection.objects)
        {
            if (selectedObject is Mesh mesh)
            {
                meshes.Add(mesh);
                totalVertices += mesh.vertexCount;
                totalTriangles += mesh.triangles.Length / 3;

                // Estimate memory usage (approximate)
                totalMemory += EstimateMeshMemoryUsage(mesh);
            }
        }

        if (meshes.Count == 0)
        {
            EditorUtility.DisplayDialog("Mesh Analysis", "No meshes selected.", "OK");
            return;
        }

        // Build report message
        string message = "Mesh Analysis Results:\n\n";
        message += "Total Meshes: " + meshes.Count + "\n";
        message += "Total Vertices: " + totalVertices + "\n";
        message += "Total Triangles: " + totalTriangles + "\n";
        message += "Estimated Memory Usage: " + EditorUtility.FormatBytes(totalMemory) + "\n\n";

        // Check for high poly meshes
        List<Mesh> highPolyMeshes = meshes.Where(m => m.vertexCount > 10000).ToList();
        if (highPolyMeshes.Count > 0)
        {
            message += "High Poly Meshes (>10k vertices): " + highPolyMeshes.Count + "\n";
            foreach (var mesh in highPolyMeshes.Take(5)) // Show only first 5
            {
                message += "- " + mesh.name + " (" + mesh.vertexCount + " vertices, " + (mesh.triangles.Length / 3) + " triangles)\n";
            }
            if (highPolyMeshes.Count > 5) message += "- ...and " + (highPolyMeshes.Count - 5) + " more\n";
            message += "\n";
        }

        // Check for non-readable meshes
        List<Mesh> nonReadableMeshes = meshes.Where(m => !m.isReadable).ToList();
        if (nonReadableMeshes.Count > 0)
        {
            message += "Non-Readable Meshes: " + nonReadableMeshes.Count + "\n";
            foreach (var mesh in nonReadableMeshes.Take(5)) // Show only first 5
            {
                message += "- " + mesh.name + "\n";
            }
            if (nonReadableMeshes.Count > 5) message += "- ...and " + (nonReadableMeshes.Count - 5) + " more\n";
            message += "\n";
        }

        // Check for meshes missing normals or tangents
        List<Mesh> missingNormalsMeshes = meshes.Where(m => !m.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Normal)).ToList();
        if (missingNormalsMeshes.Count > 0)
        {
            message += "Meshes Missing Normals: " + missingNormalsMeshes.Count + "\n";
            foreach (var mesh in missingNormalsMeshes.Take(5)) // Show only first 5
            {
                message += "- " + mesh.name + "\n";
            }
            if (missingNormalsMeshes.Count > 5) message += "- ...and " + (missingNormalsMeshes.Count - 5) + " more\n";
            message += "\n";
        }

        // Check for meshes missing UVs
        List<Mesh> missingUVMeshes = meshes.Where(m => !m.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0)).ToList();
        if (missingUVMeshes.Count > 0)
        {
            message += "Meshes Missing UVs: " + missingUVMeshes.Count + "\n";
            foreach (var mesh in missingUVMeshes.Take(5)) // Show only first 5
            {
                message += "- " + mesh.name + "\n";
            }
            if (missingUVMeshes.Count > 5) message += "- ...and " + (missingUVMeshes.Count - 5) + " more\n";
        }

        EditorUtility.DisplayDialog("Mesh Analysis", message, "OK");
    }

    [MenuItem("Assets/Material Analyzer/Analyze Selected Meshes", true)]
    public static bool ValidateAnalyzeSelectedMeshes()
    {
        foreach (Object selectedObject in Selection.objects)
        {
            if (selectedObject is Mesh)
                return true;
        }
        return false;
    }

    [MenuItem("Assets/Material Analyzer/Combine Selected Meshes")]
    public static void CombineSelectedMeshes()
    {
        List<Mesh> meshes = new List<Mesh>();

        foreach (Object selectedObject in Selection.objects)
        {
            if (selectedObject is Mesh mesh)
            {
                meshes.Add(mesh);
            }
        }

        if (meshes.Count < 2)
        {
            EditorUtility.DisplayDialog("Combine Meshes", "You need to select at least 2 meshes to combine.", "OK");
            return;
        }

        bool proceed = EditorUtility.DisplayDialog("Combine Meshes", 
            "This will create a new combined mesh from the " + meshes.Count + " selected meshes.\n\n" +
            "Note: This is a simple combination that works best for meshes that will use the same material. " +
            "For more complex mesh combining needs, consider using a dedicated asset from the Asset Store.\n\n" +
            "Proceed?", 
            "Combine", "Cancel");

        if (!proceed) return;

        // Create combine instances
        CombineInstance[] combine = new CombineInstance[meshes.Count];

        for (int i = 0; i < meshes.Count; i++)
        {
            combine[i].mesh = meshes[i];
            combine[i].transform = Matrix4x4.identity;
        }

        // Create new mesh
        Mesh combinedMesh = new Mesh();
        combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // Support >65k vertices
        combinedMesh.CombineMeshes(combine);
        combinedMesh.name = "CombinedMesh";

        // Save the combined mesh as an asset
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Combined Mesh", 
            "CombinedMesh", 
            "asset", 
            "Save the combined mesh to your project");

        if (string.IsNullOrEmpty(path)) return;

        AssetDatabase.CreateAsset(combinedMesh, path);
        AssetDatabase.SaveAssets();

        // Select the new mesh in the project
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = combinedMesh;
        EditorGUIUtility.PingObject(combinedMesh);

        EditorUtility.DisplayDialog("Combine Complete", 
            "Successfully created a combined mesh with:\n" +
            combinedMesh.vertexCount + " vertices\n" +
            (combinedMesh.triangles.Length / 3) + " triangles", 
            "OK");
    }

    [MenuItem("Assets/Material Analyzer/Combine Selected Meshes", true)]
    public static bool ValidateCombineSelectedMeshes()
    {
        int meshCount = 0;
        foreach (Object selectedObject in Selection.objects)
        {
            if (selectedObject is Mesh)
                meshCount++;
        }
        return meshCount >= 2;
    }

    [MenuItem("Assets/Material Analyzer/Optimize Selected Meshes")]
    public static void OptimizeSelectedMeshes()
    {
        List<Mesh> meshes = new List<Mesh>();

        foreach (Object selectedObject in Selection.objects)
        {
            if (selectedObject is Mesh mesh)
            {
                meshes.Add(mesh);
            }
        }

        if (meshes.Count == 0)
        {
            EditorUtility.DisplayDialog("Optimize Meshes", "No meshes selected.", "OK");
            return;
        }

        bool proceed = EditorUtility.DisplayDialog("Optimize Meshes", 
            "This will modify " + meshes.Count + " mesh(es) to optimize them. The following operations will be performed:\n\n" +
            "- Recalculate normals (if needed)\n" +
            "- Recalculate tangents (if needed)\n" +
            "- Optimize triangle order for better GPU performance\n\n" +
            "These changes can be reverted with Undo. Proceed?", 
            "Optimize", "Cancel");

        if (!proceed) return;

        int optimizedCount = 0;

        foreach (Mesh mesh in meshes)
        {
            if (!mesh.isReadable)
            {
                Debug.LogWarning("Could not optimize mesh " + mesh.name + " because it is not marked as Read/Write enabled in its import settings.");
                continue;
            }

            // Record object for undo
            Undo.RecordObject(mesh, "Optimize Mesh");

            bool modified = false;

            // Check if mesh needs normals
            if (!mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Normal))
            {
                mesh.RecalculateNormals();
                modified = true;
            }

            // Check if mesh needs tangents
            if (!mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Tangent) && 
                mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Normal) &&
                mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0))
            {
                mesh.RecalculateTangents();
                modified = true;
            }

            // Optimize mesh
            mesh.Optimize();
            modified = true;

            if (modified) optimizedCount++;
        }

        if (optimizedCount > 0)
        {
            EditorUtility.DisplayDialog("Optimize Complete", 
                "Successfully optimized " + optimizedCount + " mesh(es).", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Optimize Complete", 
                "No optimizations were performed. Meshes might already be optimized or not marked as Read/Write enabled.", "OK");
        }
    }

    [MenuItem("Assets/Material Analyzer/Optimize Selected Meshes", true)]
    public static bool ValidateOptimizeSelectedMeshes()
    {
        foreach (Object selectedObject in Selection.objects)
        {
            if (selectedObject is Mesh)
                return true;
        }
        return false;
    }

    public static long EstimateMeshMemoryUsage(Mesh mesh)
    {
        if (mesh == null) return 0;

        // Basic vertex data (position, normal, tangent, color, UV)
        long vertexSize = 12; // Position (3 floats * 4 bytes)

        if (mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Normal))
            vertexSize += 12; // Normal (3 floats * 4 bytes)

        if (mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Tangent))
            vertexSize += 16; // Tangent (4 floats * 4 bytes)

        if (mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Color))
            vertexSize += 16; // Color (4 floats * 4 bytes)

        if (mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0))
            vertexSize += 8; // UV0 (2 floats * 4 bytes)

        if (mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord1))
            vertexSize += 8; // UV1 (2 floats * 4 bytes)

        // Calculate total size
        long totalVertexMemory = vertexSize * mesh.vertexCount;

        // Add index buffer memory
        long indexFormat = mesh.indexFormat == UnityEngine.Rendering.IndexFormat.UInt16 ? 2 : 4;
        long totalIndexMemory = indexFormat * mesh.triangles.Length;

        // Add blend shape memory if any
        long blendShapeMemory = 0;
        if (mesh.blendShapeCount > 0)
        {
            // Each blend shape stores positions, normals and tangents
            blendShapeMemory = mesh.blendShapeCount * mesh.vertexCount * (12 + 12 + 16);
        }

        // Add bones/weights memory if any
        long skinningMemory = 0;
        if (mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.BlendWeight) &&
            mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.BlendIndices))
        {
            // 4 weights (float) + 4 indices (typically byte or short) per vertex
            skinningMemory = mesh.vertexCount * (16 + 4);
        }

        return totalVertexMemory + totalIndexMemory + blendShapeMemory + skinningMemory;
    }

    public static bool NeedsMeshOptimization(Mesh mesh)
    {
        if (mesh == null || !mesh.isReadable)
            return false;

        // Check if mesh is missing essential data
        bool needsNormals = !mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Normal);
        bool needsTangents = !mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Tangent) && 
                           mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Normal) &&
                           mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0);

        // More optimizations could be checked here
        return needsNormals || needsTangents;
    }
}
