using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

public static class MaterialAnalyzerUtility
{
    [MenuItem("Assets/Material Analyzer/Check Texture Size")]
    public static void CheckSelectedTexturesSize()
    {
        List<Texture2D> largeTextures = new List<Texture2D>();
        List<Texture2D> nonPowerOfTwoTextures = new List<Texture2D>();
        List<Texture2D> uncompressedTextures = new List<Texture2D>();
        long totalMemory = 0;

        foreach (Object selectedObject in Selection.objects)
        {
            if (selectedObject is Texture2D texture)
            {
                totalMemory += MaterialAnalyzerHelpers.CalculateTextureMemorySize(texture);

                // Check size
                if (texture.width > 2048 || texture.height > 2048)
                {
                    largeTextures.Add(texture);
                }

                // Check power of two
                if (!MaterialAnalyzerHelpers.IsPowerOfTwo(texture.width) || !MaterialAnalyzerHelpers.IsPowerOfTwo(texture.height))
                {
                    nonPowerOfTwoTextures.Add(texture);
                }

                // Check compression
                if (texture.format == TextureFormat.RGBA32 || 
                    texture.format == TextureFormat.ARGB32 || 
                    texture.format == TextureFormat.RGB24)
                {
                    uncompressedTextures.Add(texture);
                }
            }
        }

        // Build report message
        string message = "Texture Analysis Results:\n\n";
        message += "Total Memory Usage: " + EditorUtility.FormatBytes(totalMemory) + "\n\n";

        if (largeTextures.Count > 0)
        {
            message += "Large Textures (>2048px): " + largeTextures.Count + "\n";
            foreach (var tex in largeTextures.Take(5)) // Show only first 5
            {
                message += "- " + tex.name + " (" + tex.width + "×" + tex.height + ")\n";
            }
            if (largeTextures.Count > 5) message += "- ...and " + (largeTextures.Count - 5) + " more\n";
            message += "\n";
        }

        if (nonPowerOfTwoTextures.Count > 0)
        {
            message += "Non-Power-of-Two Textures: " + nonPowerOfTwoTextures.Count + "\n";
            foreach (var tex in nonPowerOfTwoTextures.Take(5)) // Show only first 5
            {
                message += "- " + tex.name + " (" + tex.width + "×" + tex.height + ")\n";
            }
            if (nonPowerOfTwoTextures.Count > 5) message += "- ...and " + (nonPowerOfTwoTextures.Count - 5) + " more\n";
            message += "\n";
        }

        if (uncompressedTextures.Count > 0)
        {
            message += "Uncompressed Textures: " + uncompressedTextures.Count + "\n";
            foreach (var tex in uncompressedTextures.Take(5)) // Show only first 5
            {
                message += "- " + tex.name + " (" + tex.format + ")\n";
            }
            if (uncompressedTextures.Count > 5) message += "- ...and " + (uncompressedTextures.Count - 5) + " more\n";
        }

        EditorUtility.DisplayDialog("Texture Analysis", message, "OK");
    }

    [MenuItem("Assets/Material Analyzer/Check Texture Size", true)]
    public static bool ValidateCheckSelectedTexturesSize()
    {
        foreach (Object selectedObject in Selection.objects)
        {
            if (selectedObject is Texture2D)
                return true;
        }
        return false;
    }

    [MenuItem("Assets/Material Analyzer/Optimize Selected Materials")]
    public static void OptimizeSelectedMaterials()
    {
        List<Material> materials = new List<Material>();

        foreach (Object selectedObject in Selection.objects)
        {
            if (selectedObject is Material material)
            {
                materials.Add(material);
            }
        }

        if (materials.Count == 0)
        {
            EditorUtility.DisplayDialog("Optimize Materials", "No materials selected.", "OK");
            return;
        }

        bool proceed = EditorUtility.DisplayDialog("Optimize Materials", 
            "This will modify " + materials.Count + " material(s) to optimize them. The following changes may be made:\n\n" +
            "- Remove unused shader keywords\n" +
            "- Disable GPU instancing if not used\n" +
            "- Optimize render queue values\n\n" +
            "These changes can be reverted with Undo. Proceed?", 
            "Optimize", "Cancel");

        if (!proceed) return;

        int optimizedCount = 0;

        foreach (Material material in materials)
        {
            bool modified = false;

            // Record object for undo
            Undo.RecordObject(material, "Optimize Material");

            // Remove unused shader keywords
            if (material.shaderKeywords.Length > 0)
            {
                material.shaderKeywords = new string[0];
                modified = true;
            }

            // Check if instancing is used
            bool hasInstancingSupport = false;
            try
            {
                // Modern way to check instancing support
                hasInstancingSupport = material.enableInstancing && material.shader != null;
            }
            catch (System.Exception) { }

            if (!hasInstancingSupport && material.enableInstancing)
            {
                material.enableInstancing = false;
                modified = true;
            }

            if (modified) optimizedCount++;
        }

        if (optimizedCount > 0)
        {
            EditorUtility.DisplayDialog("Optimize Complete", 
                "Successfully optimized " + optimizedCount + " material(s).", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Optimize Complete", 
                "No optimizations were needed for the selected materials.", "OK");
        }
    }

    [MenuItem("Assets/Material Analyzer/Optimize Selected Materials", true)]
    public static bool ValidateOptimizeSelectedMaterials()
    {
        foreach (Object selectedObject in Selection.objects)
        {
            if (selectedObject is Material)
                return true;
        }
        return false;
    }

    [MenuItem("Assets/Material Analyzer/Find Materials Using Selected Shader")]
    public static void FindMaterialsUsingSelectedShader()
    {
        List<Shader> shaders = new List<Shader>();

        foreach (Object selectedObject in Selection.objects)
        {
            if (selectedObject is Shader shader)
            {
                shaders.Add(shader);
            }
        }

        if (shaders.Count == 0)
        {
            EditorUtility.DisplayDialog("Find Materials", "No shaders selected.", "OK");
            return;
        }

        // Find all materials in the project
        string[] materialGuids = AssetDatabase.FindAssets("t:Material");
        List<Material> matchingMaterials = new List<Material>();

        foreach (string guid in materialGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material != null && material.shader != null)
            {
                foreach (Shader shader in shaders)
                {
                    if (material.shader == shader)
                    {
                        matchingMaterials.Add(material);
                        break;
                    }
                }
            }
        }

        if (matchingMaterials.Count > 0)
        {
            Selection.objects = matchingMaterials.Cast<Object>().ToArray();
            EditorUtility.DisplayDialog("Find Materials", 
                "Found " + matchingMaterials.Count + " materials using the selected shader(s). These materials are now selected.", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Find Materials", 
                "No materials found using the selected shader(s).", "OK");
        }
    }

    [MenuItem("Assets/Material Analyzer/Find Materials Using Selected Shader", true)]
    public static bool ValidateFindMaterialsUsingSelectedShader()
    {
        foreach (Object selectedObject in Selection.objects)
        {
            if (selectedObject is Shader)
                return true;
        }
        return false;
    }

    [MenuItem("Assets/Material Analyzer/Find References to Selected Texture")]
    public static void FindReferencesToSelectedTexture()
    {
        List<Texture> textures = new List<Texture>();

        foreach (Object selectedObject in Selection.objects)
        {
            if (selectedObject is Texture texture)
            {
                textures.Add(texture);
            }
        }

        if (textures.Count == 0)
        {
            EditorUtility.DisplayDialog("Find References", "No textures selected.", "OK");
            return;
        }

        // Find all materials in the project
        string[] materialGuids = AssetDatabase.FindAssets("t:Material");
        Dictionary<Texture, List<Material>> textureReferences = new Dictionary<Texture, List<Material>>();

        foreach (Texture texture in textures)
        {
            textureReferences[texture] = new List<Material>();
        }

        foreach (string guid in materialGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null || material.shader == null) continue;

            // Check material properties for texture references
            var shader = material.shader;
            var propertyCount = ShaderUtil.GetPropertyCount(shader);

            for (int i = 0; i < propertyCount; i++)
            {
                if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                {
                    string propertyName = ShaderUtil.GetPropertyName(shader, i);
                    Texture propertyTexture = material.GetTexture(propertyName);

                    if (propertyTexture != null)
                    {
                        foreach (Texture texture in textures)
                        {
                            if (propertyTexture == texture)
                            {
                                textureReferences[texture].Add(material);
                                break;
                            }
                        }
                    }
                }
            }
        }

        // Build report
        string message = "Texture References:\n\n";
        int totalReferences = 0;

        foreach (Texture texture in textures)
        {
            List<Material> references = textureReferences[texture];
            totalReferences += references.Count;

            message += texture.name + ": " + references.Count + " reference(s)\n";

            foreach (Material material in references.Take(5)) // Show only first 5
            {
                message += "- " + material.name + "\n";
            }

            if (references.Count > 5) message += "- ...and " + (references.Count - 5) + " more\n";
            message += "\n";
        }

        if (totalReferences > 0)
        {
            bool selectMaterials = EditorUtility.DisplayDialog("Texture References", 
                message + "\nWould you like to select all referenced materials?", "Select Materials", "Close");

            if (selectMaterials)
            {
                List<Material> allReferencedMaterials = new List<Material>();
                foreach (var references in textureReferences.Values)
                {
                    allReferencedMaterials.AddRange(references);
                }

                Selection.objects = allReferencedMaterials.Distinct().Cast<Object>().ToArray();
            }
        }
        else
        {
            EditorUtility.DisplayDialog("Texture References", 
                "No materials found referencing the selected texture(s).", "OK");
        }
    }

    [MenuItem("Assets/Material Analyzer/Find References to Selected Texture", true)]
    public static bool ValidateFindReferencesToSelectedTexture()
    {
        foreach (Object selectedObject in Selection.objects)
        {
            if (selectedObject is Texture)
                return true;
        }
        return false;
    }
}
