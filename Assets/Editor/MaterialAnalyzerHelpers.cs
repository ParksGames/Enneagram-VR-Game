using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;

public static class MaterialAnalyzerHelpers
{
    // Texture memory calculation methods
    public static long CalculateTextureMemorySize(Texture2D texture)
    {
        if (texture == null) return 0;

        // Calculate base memory size
        long memorySize = (long)texture.width * texture.height;
        int bytesPerPixel = GetBytesPerPixel(texture.format);
        memorySize *= bytesPerPixel;

        // Add mipmap sizes if present
        if (texture.mipmapCount > 1)
        {
            float mipmapSizeFactor = 1.33f; // Approximate factor for all mipmap levels (1 + 1/4 + 1/16 + ...)
            memorySize = (long)(memorySize * mipmapSizeFactor);
        }

        return memorySize;
    }

    public static int GetBytesPerPixel(TextureFormat format)
    {
        switch (format)
        {
            case TextureFormat.Alpha8: return 1;
            case TextureFormat.ARGB4444: return 2;
            case TextureFormat.RGB24: return 3;
            case TextureFormat.RGBA32: return 4;
            case TextureFormat.ARGB32: return 4;
            case TextureFormat.RGB565: return 2;
            case TextureFormat.R16: return 2;
            case TextureFormat.DXT1: return 1; // Compressed (approximate)
            case TextureFormat.DXT5: return 1; // Compressed (approximate)
            case TextureFormat.RGBA4444: return 2;
            case TextureFormat.BGRA32: return 4;
            case TextureFormat.RHalf: return 2;
            case TextureFormat.RGHalf: return 4;
            case TextureFormat.RGBAHalf: return 8;
            case TextureFormat.RFloat: return 4;
            case TextureFormat.RGFloat: return 8;
            case TextureFormat.RGBAFloat: return 16;
            case TextureFormat.BC6H: return 1; // Compressed
            case TextureFormat.BC7: return 1; // Compressed
            case TextureFormat.BC4: return 1; // Compressed
            case TextureFormat.BC5: return 1; // Compressed
            case TextureFormat.DXT1Crunched: return 1; // Compressed
            case TextureFormat.DXT5Crunched: return 1; // Compressed
            case TextureFormat.PVRTC_RGB2: return 1; // Compressed
            case TextureFormat.PVRTC_RGBA2: return 1; // Compressed
            case TextureFormat.PVRTC_RGB4: return 1; // Compressed
            case TextureFormat.PVRTC_RGBA4: return 1; // Compressed
            case TextureFormat.ETC_RGB4: return 1; // Compressed
            case TextureFormat.EAC_R: return 1; // Compressed
            case TextureFormat.EAC_R_SIGNED: return 1; // Compressed
            case TextureFormat.EAC_RG: return 1; // Compressed
            case TextureFormat.EAC_RG_SIGNED: return 1; // Compressed
            case TextureFormat.ETC2_RGB: return 1; // Compressed
            case TextureFormat.ETC2_RGBA1: return 1; // Compressed
            case TextureFormat.ETC2_RGBA8: return 1; // Compressed
            case TextureFormat.ASTC_4x4: return 1; // Compressed
            case TextureFormat.ASTC_5x5: return 1; // Compressed
            case TextureFormat.ASTC_6x6: return 1; // Compressed
            case TextureFormat.ASTC_8x8: return 1; // Compressed
            case TextureFormat.ASTC_10x10: return 1; // Compressed
            case TextureFormat.ASTC_12x12: return 1; // Compressed
            case TextureFormat.ASTC_HDR_4x4: return 1; // Compressed
            case TextureFormat.ASTC_HDR_5x5: return 1; // Compressed
            case TextureFormat.ASTC_HDR_6x6: return 1; // Compressed
            case TextureFormat.ASTC_HDR_8x8: return 1; // Compressed
            case TextureFormat.ASTC_HDR_10x10: return 1; // Compressed
            case TextureFormat.ASTC_HDR_12x12: return 1; // Compressed
            default: return 4; // Default to 4 bytes per pixel as a fallback
        }
    }

    public static bool IsPowerOfTwo(int x)
    {
        return (x & (x - 1)) == 0 && x > 0;
    }

    public static List<string> AnalyzeTextureIssues(Texture2D texture, int sizeWarningThreshold, int sizeErrorThreshold)
    {
        List<string> issues = new List<string>();

        if (texture == null) return issues;

        // Check size
        if (texture.width > sizeErrorThreshold || texture.height > sizeErrorThreshold)
        {
            issues.Add("Oversized texture");
        }
        else if (texture.width > sizeWarningThreshold || texture.height > sizeWarningThreshold)
        {
            issues.Add("Large texture");
        }

        // Check if power of two
        if (!IsPowerOfTwo(texture.width) || !IsPowerOfTwo(texture.height))
        {
            issues.Add("Non-power-of-two");
        }

        // Check compression
        if (texture.format == TextureFormat.RGBA32 || 
            texture.format == TextureFormat.ARGB32 || 
            texture.format == TextureFormat.RGB24 ||
            texture.format == TextureFormat.BGRA32)
        {
            issues.Add("Uncompressed");
        }

        // Check if read/write is enabled (can use more memory)
        if (IsReadWriteEnabled(texture))
        {
            issues.Add("Read/Write Enabled");
        }

        return issues;
    }

    public static bool IsReadWriteEnabled(Texture2D texture)
    {
        string assetPath = AssetDatabase.GetAssetPath(texture);
        if (!string.IsNullOrEmpty(assetPath))
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                return importer.isReadable;
            }
        }
        return false;
    }

    public static StringBuilder GenerateCSVReport(List<MaterialInfo> materials, List<ShaderInfo> shaders, List<TextureInfo> textures, AnalysisOverview overview)
    {
        StringBuilder csv = new StringBuilder();

        // Add summary
        csv.AppendLine("MATERIAL ANALYSIS SUMMARY");
        csv.AppendLine("Generated on," + System.DateTime.Now.ToString());
        csv.AppendLine();
        csv.AppendLine("Materials," + overview.MaterialCount);
        csv.AppendLine("Unique Materials," + overview.UniqueMaterialCount);
        csv.AppendLine("Shaders," + overview.ShaderCount);
        csv.AppendLine("Shader Keywords," + overview.ShaderKeywordCount);
        csv.AppendLine("Textures," + overview.TextureCount);
        csv.AppendLine("Texture Memory," + EditorUtility.FormatBytes(overview.TotalTextureMemory));
        csv.AppendLine("Estimated Batches," + overview.EstimatedBatches);
        csv.AppendLine();

        // Add materials
        csv.AppendLine("MATERIALS");
        csv.AppendLine("Name,Shader,Usage Count,Texture Count,Path");

        foreach (var material in materials.OrderByDescending(m => m.UsageCount))
        {
            csv.AppendLine(string.Format("{0},{1},{2},{3},{4}",
                material.Name.Replace(",", ";"),
                material.ShaderName.Replace(",", ";"),
                material.UsageCount,
                material.TextureCount,
                material.Path.Replace(",", ";")));
        }

        csv.AppendLine();

        // Add textures
        csv.AppendLine("TEXTURES");
        csv.AppendLine("Name,Width,Height,Format,Memory,Usage Count,Issues,Path");

        foreach (var texture in textures.OrderByDescending(t => t.MemorySize))
        {
            csv.AppendLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7}",
                texture.TextureName.Replace(",", ";"),
                texture.Width,
                texture.Height,
                texture.Format.Replace(",", ";"),
                EditorUtility.FormatBytes(texture.MemorySize).Replace(",", ";"),
                texture.UsageCount,
                string.Join(";", texture.Issues).Replace(",", ";"),
                texture.Path.Replace(",", ";")));
        }

        return csv;
    }

    public static void ExportCSVReport(List<MaterialInfo> materials, List<ShaderInfo> shaders, List<TextureInfo> textures, AnalysisOverview overview)
    {
        string path = EditorUtility.SaveFilePanel("Export Material Analysis CSV Report", "", "MaterialAnalysisReport", "csv");

        if (string.IsNullOrEmpty(path))
            return;

        StringBuilder csv = GenerateCSVReport(materials, shaders, textures, overview);

        // Write to file
        System.IO.File.WriteAllText(path, csv.ToString());

        // Open the file
        EditorUtility.RevealInFinder(path);
        EditorUtility.DisplayDialog("Export Complete", "CSV report exported successfully to:\n" + path, "OK");
    }

    public static void CheckShaderVariants(Shader shader, out int variantCount, out int keywordCount)
    {
        keywordCount = 0;
        variantCount = 0;

        try
        {
            // Use reflection to access ShaderUtil methods if available
            System.Type shaderUtilType = typeof(ShaderUtil);
            int passCount = 1;

            var getPassCountMethod = shaderUtilType.GetMethod("GetPassCount", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            if (getPassCountMethod != null)
                passCount = (int)getPassCountMethod.Invoke(null, new object[] { shader });

            var shaderKeywords = new List<string>();

            // Get all keywords from material that use this shader
            var materials = Resources.FindObjectsOfTypeAll<Material>();
            foreach (var material in materials)
            {
                if (material.shader == shader)
                {
                    foreach (var keyword in material.shaderKeywords)
                    {
                        if (!string.IsNullOrEmpty(keyword) && !shaderKeywords.Contains(keyword))
                            shaderKeywords.Add(keyword);
                    }
                }
            }

            keywordCount = shaderKeywords.Count;

            // Estimate variants (simplified calculation)
            if (keywordCount > 0)
            {
                // Simplified estimate: most keywords generate 2 variants (on/off)
                // But some keywords are mutually exclusive, so we use a more conservative estimate
                variantCount = 1 << Mathf.Min(keywordCount, 10); // Cap at 2^10 to avoid overflows
                variantCount = Mathf.Min(variantCount, 1024); // Reasonable cap
            }
            else
            {
                variantCount = 1; // At least one variant
            }
        }
        catch (System.Exception)
        {
            // Some built-in shaders might not support these utilities
            keywordCount = 0;
            variantCount = 1;
        }
    }

    public static string GetShaderRenderPipelineType(Shader shader)
    {
        if (shader == null) return "Unknown";

        string shaderName = shader.name.ToLowerInvariant();

        if (shaderName.Contains("universal") || shaderName.Contains("urp") || shaderName.StartsWith("universal render pipeline"))
            return "URP";

        if (shaderName.Contains("hdrp") || shaderName.StartsWith("hdrp") || shaderName.Contains("high definition"))
            return "HDRP";

        if (shaderName.StartsWith("legacy") || shaderName.Contains("standard") || shaderName.Contains("unlit") || shaderName.Contains("specular"))
            return "Built-in";

        // Check other indicators
        string renderPipelineTag = "";

        // Use Shader.GetPropertyToID to check if the shader has specific properties
        if (Shader.Find("Universal Render Pipeline/Lit") != null && 
            shader.name.Contains("Universal") || shader.name.Contains("URP"))
            return "URP";

        if (Shader.Find("HDRP/Lit") != null && 
            shader.name.Contains("HDRP") || shader.name.Contains("High Definition"))
            return "HDRP";

        return "Unknown";
    }
}
