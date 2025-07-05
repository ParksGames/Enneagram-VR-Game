# Unity Material Analyzer

A comprehensive Unity Editor tool for analyzing and organizing materials, shaders, and textures in your scenes and project.

## Features

- **Complete Material Analysis**: Analyze all materials in your scene or project, showing usage counts, shader information, and texture dependencies.
- **Shader Insights**: View shader usage, keywords, and material dependencies to optimize shader variants.
- **Texture Optimization**: Identify oversized, uncompressed, or non-power-of-two textures that might impact performance.
- **Performance Recommendations**: Get actionable suggestions for optimizing your project's materials and textures.
- **Advanced Filtering**: Search, sort, and filter materials, shaders, and textures using multiple criteria.
- **Selection Integration**: Right-click on GameObjects, Materials, or Renderers to analyze them directly.
- **Export Reports**: Generate HTML or CSV reports for sharing or documentation.

## How to Use

### Opening the Analyzer

1. Open via menu: **Window → Analysis → Material Analyzer**
2. Or right-click a GameObject and select **Analyze Materials**
3. Or right-click a Material and select **Analyze in Material Analyzer**

### Overview Tab

The Overview tab provides a high-level summary of your scene's materials, shaders, and textures, along with performance recommendations and quick actions.

### Materials Tab

Analyze individual materials in your scene or project:
- View shader usage, texture dependencies, and usage count
- Group materials by shader type
- Search and filter by name or shader
- Ping materials in the Project window or select them

### Shaders Tab

Inspect shaders used in your project:
- See how many materials use each shader
- View shader keywords and variants
- Identify shaders that might cause performance issues

### Textures Tab

Optimize your texture usage:
- Find oversized or inefficiently formatted textures
- See memory usage for each texture
- Group textures by size category
- Identify materials using each texture

### Settings Tab

Customize the analyzer behavior:
- Set warning thresholds for materials and textures
- Customize the display colors
- Export or import settings

## Context Menu Integration

Material Analyzer integrates with Unity's context menus:

- **GameObject → Analyze Materials**: Analyze materials on selected GameObjects
- **Material context menu → Analyze in Material Analyzer**: Focus on a specific material
- **Renderer context menu → Analyze Materials**: Analyze materials on a specific renderer
- **Asset context menu → Material Analyzer**: Additional texture and material batch operations

## Tips for Optimization

1. **Reduce Material Count**: Consolidate materials using the same shader when possible
2. **Optimize Texture Sizes**: Reduce oversized textures identified by the analyzer
3. **Check Compression Settings**: Ensure textures use appropriate compression formats
4. **Limit Shader Variants**: Minimize shader keywords to reduce variant count
5. **Use the Export Report**: Generate reports before and after optimization to track improvements

## Requirements

- Unity 2020.3 or later
- Works with Built-in Render Pipeline, URP, and HDRP
