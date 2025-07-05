using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class MaterialAnalyzerWindow : EditorWindow
{
    // Tabs
    private enum AnalysisTab
    {
        Materials,
        Shaders,
        Textures,
        Meshes,
        Overview,
        Settings
    }

    private AnalysisTab _currentTab = AnalysisTab.Overview;

    // Selection filtering
    private bool _filterBySelection = false;
    private List<GameObject> _filteredGameObjects = new List<GameObject>();
    private List<Material> _filteredMaterials = new List<Material>();
    private Material _focusedMaterial = null;

    // Search
    private string _materialSearchFilter = "";
    private string _shaderSearchFilter = "";
    private string _textureSearchFilter = "";

    // Analysis data
    private List<MaterialInfo> _materials = new List<MaterialInfo>();
    private List<ShaderInfo> _shaders = new List<ShaderInfo>();
    private List<TextureInfo> _textures = new List<TextureInfo>();
    private List<MeshInfo> _meshes = new List<MeshInfo>();
    private AnalysisOverview _overview = new AnalysisOverview();

    // UI State
    private Vector2 _materialScrollPosition;
    private Vector2 _shaderScrollPosition;
    private Vector2 _textureScrollPosition;
    private Vector2 _meshScrollPosition;
    private bool _showSceneMaterialsOnly = true;
    private bool _showUsedAssetsOnly = true;
    private bool _groupByShader = false;
    private bool _groupBySize = false;
    private bool _groupByVertexCount = false;
    private bool _autoRefresh = true;
    private bool _showAdvancedInfo = false;
    private string _meshSearchFilter = "";

    // Sort options
    private enum SortOption
    {
        Name,
        Size,
        UsageCount,
        Type,
        Path
    }

    private SortOption _materialSortOption = SortOption.Name;
    private SortOption _shaderSortOption = SortOption.UsageCount;
    private SortOption _textureSortOption = SortOption.Size;
    private SortOption _meshSortOption = SortOption.Size;
    private bool _materialSortAscending = true;
    private bool _shaderSortAscending = false;
    private bool _textureSortAscending = false;
    private bool _meshSortAscending = false;

    // Settings
    private Color _warningColor = new Color(1f, 0.7f, 0.3f);
    private Color _errorColor = new Color(1f, 0.3f, 0.3f);
    private Color _goodColor = new Color(0.3f, 1f, 0.5f);
    private int _textureSizeWarningThreshold = 1024; // Warning if texture size > 1024
    private int _textureSizeErrorThreshold = 2048;   // Error if texture size > 2048
    private int _materialCountWarningThreshold = 50;  // Warning if > 50 materials
    private int _shaderVariantWarningThreshold = 100; // Warning if > 100 shader variants
    private int _vertexCountWarningThreshold = 10000; // Warning if > 10k vertices
    private int _vertexCountErrorThreshold = 50000;   // Error if > 50k vertices
    private int _triangleCountWarningThreshold = 15000; // Warning if > 15k triangles
    private int _triangleCountErrorThreshold = 65000;   // Error if > 65k triangles

    // Icons and styles
    private GUIStyle _headerStyle;
    private GUIStyle _boldLabelStyle;
    private GUIStyle _centeredLabelStyle;
    private GUIStyle _warningLabelStyle;
    private GUIStyle _errorLabelStyle;
    private GUIStyle _goodLabelStyle;
    private GUIStyle _tabButtonStyle;
    private GUIStyle _activeTabButtonStyle;
    private GUIStyle _statsBoxStyle;
    private GUIStyle _buttonStyle;
    private GUIStyle _searchBoxStyle;
    private GUIStyle _iconButtonStyle;
    private GUIContent _refreshIcon;
    private GUIContent _pingIcon;
    private GUIContent _selectIcon;
    private GUIContent _warningIcon;
    private GUIContent _errorIcon;
    private GUIContent _goodIcon;
    private GUIContent _infoIcon;
    private GUIContent _settingsIcon;
    private Texture2D _backgroundTexture;
    private GUIContent _materialIcon;
    private GUIContent _shaderIcon;
    private GUIContent _textureIcon;
    private GUIContent _folderIcon;
    private GUIContent _statsIcon;
    private GUIContent _meshIcon;
    private GUIContent _analyzeAllIcon;

    [MenuItem("Window/Material Analyzer")]
    public static void ShowWindow()
    {
        MaterialAnalyzerWindow window = GetWindow<MaterialAnalyzerWindow>("Material Analyzer");
        window.minSize = new Vector2(500, 300);
        window.Show();
    }

    private void OnEnable()
    {
        InitializeStyles();
        LoadIcons();
        PerformAnalysis();
    }

    private void LoadIcons()
    {
        _refreshIcon = EditorGUIUtility.IconContent("Refresh");
        _pingIcon = EditorGUIUtility.IconContent("PingableElement Icon");
        _selectIcon = EditorGUIUtility.IconContent("d_GreenLight");
        _warningIcon = EditorGUIUtility.IconContent("console.warnicon");
        _errorIcon = EditorGUIUtility.IconContent("console.erroricon");
        _goodIcon = EditorGUIUtility.IconContent("d_FilterSelectedOnly");
        _infoIcon = EditorGUIUtility.IconContent("console.infoicon");
        _settingsIcon = EditorGUIUtility.IconContent("d_Settings");
        _materialIcon = EditorGUIUtility.IconContent("Material Icon");
        _shaderIcon = EditorGUIUtility.IconContent("Shader Icon");
        _textureIcon = EditorGUIUtility.IconContent("Texture Icon");
        _folderIcon = EditorGUIUtility.IconContent("Folder Icon");
        _statsIcon = EditorGUIUtility.IconContent("UnityEditor.ProfilerWindow");
        _meshIcon = EditorGUIUtility.IconContent("Mesh Icon");
        _analyzeAllIcon = EditorGUIUtility.IconContent("d_Profiler.GlobalIllumination");
    }

    private void InitializeStyles()
    {
        _headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleLeft,
            margin = new RectOffset(10, 10, 10, 10)
        };

        _boldLabelStyle = new GUIStyle(EditorStyles.boldLabel);

        _centeredLabelStyle = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleCenter
        };

        _warningLabelStyle = new GUIStyle(EditorStyles.label)
        {
            normal = { textColor = _warningColor }
        };

        _errorLabelStyle = new GUIStyle(EditorStyles.label)
        {
            normal = { textColor = _errorColor }
        };

        _goodLabelStyle = new GUIStyle(EditorStyles.label)
        {
            normal = { textColor = _goodColor }
        };

        _tabButtonStyle = new GUIStyle(EditorStyles.toolbarButton)
        {
            fontSize = 12,
            fixedHeight = 30,
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(10, 10, 5, 5)
        };

        _activeTabButtonStyle = new GUIStyle(_tabButtonStyle)
        {
            normal = { background = EditorGUIUtility.whiteTexture, textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
        };

        _statsBoxStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(10, 10, 10, 10),
            margin = new RectOffset(5, 5, 5, 5)
        };

        _buttonStyle = new GUIStyle(EditorStyles.miniButton)
        {
            padding = new RectOffset(5, 5, 3, 3)
        };

        _searchBoxStyle = new GUIStyle(EditorStyles.toolbarSearchField)
        {
            fixedHeight = 22,
            margin = new RectOffset(5, 5, 5, 5)
        };

        _iconButtonStyle = new GUIStyle(EditorStyles.miniButton)
        {
            padding = new RectOffset(2, 2, 2, 2),
            fixedWidth = 26,
            fixedHeight = 20
        };

        // Create background texture
        _backgroundTexture = new Texture2D(1, 1);
        _backgroundTexture.SetPixel(0, 0, EditorGUIUtility.isProSkin ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.8f, 0.8f, 0.8f));
        _backgroundTexture.Apply();
    }

    private void OnGUI()
    {
        DrawToolbar();

        EditorGUILayout.Space(5);

        switch (_currentTab)
        {
            case AnalysisTab.Overview:
                DrawOverviewTab();
                break;
            case AnalysisTab.Materials:
                DrawMaterialsTab();
                break;
            case AnalysisTab.Shaders:
                DrawShadersTab();
                break;
            case AnalysisTab.Textures:
                DrawTexturesTab();
                break;
            case AnalysisTab.Meshes:
                DrawMeshesTab();
                break;
            case AnalysisTab.Settings:
                DrawSettingsTab();
                break;
        }
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        GUIStyle tabStyle = _currentTab == AnalysisTab.Overview ? _activeTabButtonStyle : _tabButtonStyle;
        if (GUILayout.Toggle(_currentTab == AnalysisTab.Overview, new GUIContent(" Overview", _statsIcon.image), tabStyle, GUILayout.Height(30)))
            _currentTab = AnalysisTab.Overview;

        tabStyle = _currentTab == AnalysisTab.Materials ? _activeTabButtonStyle : _tabButtonStyle;
        if (GUILayout.Toggle(_currentTab == AnalysisTab.Materials, new GUIContent(" Materials", _materialIcon.image), tabStyle, GUILayout.Height(30)))
            _currentTab = AnalysisTab.Materials;

        tabStyle = _currentTab == AnalysisTab.Shaders ? _activeTabButtonStyle : _tabButtonStyle;
        if (GUILayout.Toggle(_currentTab == AnalysisTab.Shaders, new GUIContent(" Shaders", _shaderIcon.image), tabStyle, GUILayout.Height(30)))
            _currentTab = AnalysisTab.Shaders;

        tabStyle = _currentTab == AnalysisTab.Textures ? _activeTabButtonStyle : _tabButtonStyle;
        if (GUILayout.Toggle(_currentTab == AnalysisTab.Textures, new GUIContent(" Textures", _textureIcon.image), tabStyle, GUILayout.Height(30)))
            _currentTab = AnalysisTab.Textures;

        tabStyle = _currentTab == AnalysisTab.Meshes ? _activeTabButtonStyle : _tabButtonStyle;
        if (GUILayout.Toggle(_currentTab == AnalysisTab.Meshes, new GUIContent(" Meshes", _meshIcon.image), tabStyle, GUILayout.Height(30)))
            _currentTab = AnalysisTab.Meshes;

        tabStyle = _currentTab == AnalysisTab.Settings ? _activeTabButtonStyle : _tabButtonStyle;
        if (GUILayout.Toggle(_currentTab == AnalysisTab.Settings, new GUIContent(" Settings", _settingsIcon.image), tabStyle, GUILayout.Height(30)))
            _currentTab = AnalysisTab.Settings;

        GUILayout.FlexibleSpace();

        // Re-Analyze All button
        if (GUILayout.Button(new GUIContent(" Re-Analyze All", _analyzeAllIcon.image), _buttonStyle, GUILayout.Width(120), GUILayout.Height(30)))
        {
            ReAnalyzeAll();
        }

        if (GUILayout.Button(_refreshIcon, _buttonStyle, GUILayout.Width(40), GUILayout.Height(30)))
        {
            PerformAnalysis();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawOverviewTab()
    {
        EditorGUILayout.BeginVertical();

        // Overview Header
        EditorGUILayout.LabelField("Scene Analysis Overview", _headerStyle);
        EditorGUILayout.Space(5);

        // Top summary boxes
        EditorGUILayout.BeginHorizontal();

        // Materials box
        EditorGUILayout.BeginVertical(_statsBoxStyle, GUILayout.MinWidth(100));
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(_materialIcon.image, GUILayout.Width(20), GUILayout.Height(20));
        EditorGUILayout.LabelField("Materials", _boldLabelStyle);
        EditorGUILayout.EndHorizontal();

        GUIStyle countStyle = _overview.MaterialCount > _materialCountWarningThreshold ? _warningLabelStyle : _goodLabelStyle;
        EditorGUILayout.LabelField(_overview.MaterialCount.ToString(), countStyle);
        EditorGUILayout.LabelField("Unique: " + _overview.UniqueMaterialCount);
        EditorGUILayout.EndVertical();

        // Shaders box
        EditorGUILayout.BeginVertical(_statsBoxStyle, GUILayout.MinWidth(100));
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(_shaderIcon.image, GUILayout.Width(20), GUILayout.Height(20));
        EditorGUILayout.LabelField("Shaders", _boldLabelStyle);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField(_overview.ShaderCount.ToString());
        EditorGUILayout.LabelField("Keywords: " + _overview.ShaderKeywordCount);
        EditorGUILayout.EndVertical();

        // Textures box
        EditorGUILayout.BeginVertical(_statsBoxStyle, GUILayout.MinWidth(100));
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(_textureIcon.image, GUILayout.Width(20), GUILayout.Height(20));
        EditorGUILayout.LabelField("Textures", _boldLabelStyle);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField(_overview.TextureCount.ToString());
        EditorGUILayout.LabelField("Memory: " + EditorUtility.FormatBytes(_overview.TotalTextureMemory));
        EditorGUILayout.EndVertical();

        // Meshes box
        EditorGUILayout.BeginVertical(_statsBoxStyle, GUILayout.MinWidth(100));
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(_meshIcon.image, GUILayout.Width(20), GUILayout.Height(20));
        EditorGUILayout.LabelField("Meshes", _boldLabelStyle);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField(_overview.UniqueMeshCount.ToString());
        EditorGUILayout.LabelField("Vertices: " + _overview.TotalVertexCount.ToString("N0"));
        EditorGUILayout.EndVertical();

        // Performance box
        EditorGUILayout.BeginVertical(_statsBoxStyle, GUILayout.MinWidth(100));
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(_statsIcon.image, GUILayout.Width(20), GUILayout.Height(20));
        EditorGUILayout.LabelField("Performance", _boldLabelStyle);
        EditorGUILayout.EndHorizontal();

        GUIStyle batchesStyle = _overview.EstimatedBatches > 100 ? _warningLabelStyle : _goodLabelStyle;
        EditorGUILayout.LabelField("Est. Batches: " + _overview.EstimatedBatches, batchesStyle);
        EditorGUILayout.LabelField("Material Switches: " + _overview.MaterialSwitchesCount);
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(15);

        // Warnings and recommendations
        EditorGUILayout.LabelField("Analysis & Recommendations", _headerStyle);
        EditorGUILayout.BeginVertical(_statsBoxStyle);

        bool hasWarnings = false;

        // Material warnings
        if (_overview.MaterialCount > _materialCountWarningThreshold)
        {
            hasWarnings = true;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(_warningIcon.image, GUILayout.Width(20), GUILayout.Height(20));
            EditorGUILayout.LabelField("High material count ("+_overview.MaterialCount+"). Consider consolidating materials.", _warningLabelStyle);
            EditorGUILayout.EndHorizontal();
        }

        // Texture size warnings
        if (_overview.OversizedTextureCount > 0)
        {
            hasWarnings = true;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(_warningIcon.image, GUILayout.Width(20), GUILayout.Height(20));
            EditorGUILayout.LabelField("Found " + _overview.OversizedTextureCount + " oversized textures. Consider reducing their dimensions.", _warningLabelStyle);
            EditorGUILayout.EndHorizontal();
        }

        // Shader keyword warnings
        if (_overview.ShaderKeywordCount > _shaderVariantWarningThreshold)
        {
            hasWarnings = true;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(_warningIcon.image, GUILayout.Width(20), GUILayout.Height(20));
            EditorGUILayout.LabelField("High shader variant count. Consider reducing shader complexity.", _warningLabelStyle);
            EditorGUILayout.EndHorizontal();
        }

        // Non-power of two textures
        if (_overview.NonPowerOfTwoTextureCount > 0)
        {
            hasWarnings = true;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(_warningIcon.image, GUILayout.Width(20), GUILayout.Height(20));
            EditorGUILayout.LabelField("Found " + _overview.NonPowerOfTwoTextureCount + " non-power-of-two textures. May cause extra memory usage.", _warningLabelStyle);
            EditorGUILayout.EndHorizontal();
        }

        // Uncompressed textures
        if (_overview.UncompressedTextureCount > 0)
        {
            hasWarnings = true;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(_warningIcon.image, GUILayout.Width(20), GUILayout.Height(20));
            EditorGUILayout.LabelField("Found " + _overview.UncompressedTextureCount + " uncompressed textures. Consider using texture compression.", _warningLabelStyle);
            EditorGUILayout.EndHorizontal();
        }

        // Duplicate materials
        if (_overview.DuplicateMaterialCount > 0)
        {
            hasWarnings = true;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(_warningIcon.image, GUILayout.Width(20), GUILayout.Height(20));
            EditorGUILayout.LabelField("Found " + _overview.DuplicateMaterialCount + " possible duplicate materials. Consider consolidating them.", _warningLabelStyle);
            EditorGUILayout.EndHorizontal();
        }

        // High poly meshes
        if (_overview.HighPolyMeshCount > 0)
        {
            hasWarnings = true;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(_warningIcon.image, GUILayout.Width(20), GUILayout.Height(20));
            EditorGUILayout.LabelField("Found " + _overview.HighPolyMeshCount + " high-poly meshes. Consider using LODs or simplifying them.", _warningLabelStyle);
            EditorGUILayout.EndHorizontal();
        }

        // Meshes missing normals
        if (_overview.MissingNormalsMeshCount > 0)
        {
            hasWarnings = true;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(_warningIcon.image, GUILayout.Width(20), GUILayout.Height(20));
            EditorGUILayout.LabelField("Found " + _overview.MissingNormalsMeshCount + " meshes missing normals. This may affect lighting.", _warningLabelStyle);
            EditorGUILayout.EndHorizontal();
        }

        // Optimizable meshes
        if (_overview.OptimizableMeshCount > 0)
        {
            hasWarnings = true;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(_warningIcon.image, GUILayout.Width(20), GUILayout.Height(20));
            EditorGUILayout.LabelField("Found " + _overview.OptimizableMeshCount + " meshes that could be optimized.", _warningLabelStyle);
            EditorGUILayout.EndHorizontal();
        }

        // No warnings
        if (!hasWarnings)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(_goodIcon.image, GUILayout.Width(20), GUILayout.Height(20));
            EditorGUILayout.LabelField("No significant issues detected. Your scene looks optimized!", _goodLabelStyle);
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(15);

        // Quick Actions
        EditorGUILayout.LabelField("Quick Actions", _headerStyle);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Select All Materials", _buttonStyle))
        {
            SelectAllMaterials();
        }

        if (GUILayout.Button("Select Large Textures", _buttonStyle))
        {
            SelectLargeTextures();
        }

        if (GUILayout.Button("Find Unused Materials", _buttonStyle))
        {
            FindUnusedMaterials();
        }

        if (GUILayout.Button("Export Report", _buttonStyle))
        {
            ExportReport();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Find High-Poly Meshes", _buttonStyle))
        {
            FindHighPolyMeshes();
        }

        if (GUILayout.Button("Optimize Meshes", _buttonStyle))
        {
            OptimizeSelectedMeshes();
        }

        if (GUILayout.Button("Analyze Scene Meshes", _buttonStyle))
        {
            _currentTab = AnalysisTab.Meshes;
            PerformAnalysis();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawMaterialsTab()
    {
        EditorGUILayout.BeginVertical();

        // Header and search
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Materials Analysis", _headerStyle);

        // Show filter indicator if filtering by selection
        if (_filterBySelection)
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("(Filtered by Selection)", _warningLabelStyle);

            if (GUILayout.Button("Clear Filter", _buttonStyle))
            {
                ClearFilters();
            }
        }

        EditorGUILayout.EndHorizontal();

        // Controls and filtering
        EditorGUILayout.BeginHorizontal();
        _materialSearchFilter = EditorGUILayout.TextField(_materialSearchFilter, _searchBoxStyle);

        if (GUILayout.Button("Clear", _buttonStyle, GUILayout.Width(50)))
        {
            _materialSearchFilter = "";
        }

        _showSceneMaterialsOnly = GUILayout.Toggle(_showSceneMaterialsOnly, "Scene Only", _buttonStyle, GUILayout.Width(85));
        _groupByShader = GUILayout.Toggle(_groupByShader, "Group by Shader", _buttonStyle, GUILayout.Width(110));

        EditorGUILayout.EndHorizontal();

        // Sorting options
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Sort by:", GUILayout.Width(50));

        if (GUILayout.Toggle(_materialSortOption == SortOption.Name, "Name", _buttonStyle, GUILayout.Width(60)))
            _materialSortOption = SortOption.Name;

        if (GUILayout.Toggle(_materialSortOption == SortOption.UsageCount, "Usage", _buttonStyle, GUILayout.Width(60)))
            _materialSortOption = SortOption.UsageCount;

        if (GUILayout.Toggle(_materialSortOption == SortOption.Type, "Shader", _buttonStyle, GUILayout.Width(60)))
            _materialSortOption = SortOption.Type;

        GUILayout.FlexibleSpace();

        EditorGUILayout.LabelField(_materialSortAscending ? "↑" : "↓", GUILayout.Width(15));
        if (GUILayout.Button("Reverse", _buttonStyle, GUILayout.Width(65)))
            _materialSortAscending = !_materialSortAscending;

        if (GUILayout.Button("Select All", _buttonStyle, GUILayout.Width(80)))
            SelectAllFilteredMaterials();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Materials list
        _materialScrollPosition = EditorGUILayout.BeginScrollView(_materialScrollPosition);

        // Get filtered & sorted materials
        List<MaterialInfo> filteredMaterials = FilterAndSortMaterials();

        if (filteredMaterials.Count == 0)
        {
            EditorGUILayout.HelpBox("No materials found with the current filter settings.", MessageType.Info);
        }
        else
        {
            // Group by shader if needed
            if (_groupByShader)
            {
                Dictionary<string, List<MaterialInfo>> materialsByShader = new Dictionary<string, List<MaterialInfo>>();

                foreach (var material in filteredMaterials)
                {
                    string shaderName = material.ShaderName ?? "Unknown Shader";

                    if (!materialsByShader.ContainsKey(shaderName))
                        materialsByShader[shaderName] = new List<MaterialInfo>();

                    materialsByShader[shaderName].Add(material);
                }

                // Sort shader groups
                var sortedShaderGroups = materialsByShader.OrderBy(kvp => kvp.Key).ToList();

                foreach (var shaderGroup in sortedShaderGroups)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    // Shader header
                    EditorGUILayout.BeginHorizontal();
                    bool expanded = EditorGUILayout.Foldout(true, "", true);
                    GUILayout.Label(_shaderIcon, GUILayout.Width(20), GUILayout.Height(20));
                    EditorGUILayout.LabelField(shaderGroup.Key + " ("+shaderGroup.Value.Count+" materials)", _boldLabelStyle);
                    EditorGUILayout.EndHorizontal();

                    if (expanded)
                    {
                        foreach (var material in shaderGroup.Value)
                        {
                            DrawMaterialItem(material);
                        }
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(2);
                }
            }
            else
            {
                // Normal list view
                foreach (var material in filteredMaterials)
                {
                    DrawMaterialItem(material);
                }
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawMaterialItem(MaterialInfo material)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // Material header
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(material.Name, _boldLabelStyle);

        GUILayout.FlexibleSpace();

        // Usage count label
        GUIStyle usageStyle = material.UsageCount > 10 ? _warningLabelStyle : EditorStyles.label;
        EditorGUILayout.LabelField("Used: " + material.UsageCount, usageStyle, GUILayout.Width(70));

        // Action buttons
        if (GUILayout.Button(_pingIcon, _iconButtonStyle))
        {
            PingMaterial(material);
        }

        if (GUILayout.Button(_selectIcon, _iconButtonStyle))
        {
            SelectMaterial(material);
        }

        EditorGUILayout.EndHorizontal();

        // Material details
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Shader: " + material.ShaderName, GUILayout.MaxWidth(400));
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        // Texture list (collapsible)
        if (material.TextureCount > 0)
        {
            EditorGUILayout.BeginHorizontal();
            material.ShowTextures = EditorGUILayout.Foldout(material.ShowTextures, "Textures ("+material.TextureCount+")", true);
            EditorGUILayout.EndHorizontal();

            if (material.ShowTextures)
            {
                EditorGUI.indentLevel++;
                foreach (var texture in material.Textures)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(10);
                    GUILayout.Label(_textureIcon, GUILayout.Width(20), GUILayout.Height(20));
                    EditorGUILayout.LabelField(texture.TextureName);
                    GUILayout.FlexibleSpace();

                    if (texture.MemorySize > 0)
                    {
                        GUIStyle memoryStyle = GetMemorySizeStyle(texture.MemorySize);
                        EditorGUILayout.LabelField(EditorUtility.FormatBytes(texture.MemorySize), memoryStyle, GUILayout.Width(100));
                    }

                    if (GUILayout.Button(_pingIcon, _iconButtonStyle))
                    {
                        PingTexture(texture);
                    }

                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }
        }

        // Keywords list (collapsible)
        if (material.Keywords != null && material.Keywords.Count > 0)
        {
            EditorGUILayout.BeginHorizontal();
            material.ShowKeywords = EditorGUILayout.Foldout(material.ShowKeywords, "Shader Keywords ("+material.Keywords.Count+")", true);
            EditorGUILayout.EndHorizontal();

            if (material.ShowKeywords)
            {
                EditorGUI.indentLevel++;
                foreach (var keyword in material.Keywords)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(10);
                    EditorGUILayout.LabelField(keyword);
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(2);
    }

    private void DrawShadersTab()
    {
        EditorGUILayout.BeginVertical();

        // Header and search
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Shaders Analysis", _headerStyle);
        EditorGUILayout.EndHorizontal();

        // Controls and filtering
        EditorGUILayout.BeginHorizontal();
        _shaderSearchFilter = EditorGUILayout.TextField(_shaderSearchFilter, _searchBoxStyle);

        if (GUILayout.Button("Clear", _buttonStyle, GUILayout.Width(50)))
        {
            _shaderSearchFilter = "";
        }

        _showUsedAssetsOnly = GUILayout.Toggle(_showUsedAssetsOnly, "Used Only", _buttonStyle, GUILayout.Width(85));
        _showAdvancedInfo = GUILayout.Toggle(_showAdvancedInfo, "Advanced Info", _buttonStyle, GUILayout.Width(110));

        EditorGUILayout.EndHorizontal();

        // Sorting options
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Sort by:", GUILayout.Width(50));

        if (GUILayout.Toggle(_shaderSortOption == SortOption.Name, "Name", _buttonStyle, GUILayout.Width(60)))
            _shaderSortOption = SortOption.Name;

        if (GUILayout.Toggle(_shaderSortOption == SortOption.UsageCount, "Usage", _buttonStyle, GUILayout.Width(60)))
            _shaderSortOption = SortOption.UsageCount;

        GUILayout.FlexibleSpace();

        EditorGUILayout.LabelField(_shaderSortAscending ? "↑" : "↓", GUILayout.Width(15));
        if (GUILayout.Button("Reverse", _buttonStyle, GUILayout.Width(65)))
            _shaderSortAscending = !_shaderSortAscending;

        if (GUILayout.Button("Select All", _buttonStyle, GUILayout.Width(80)))
            SelectAllFilteredShaders();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Shaders list
        _shaderScrollPosition = EditorGUILayout.BeginScrollView(_shaderScrollPosition);

        // Get filtered & sorted shaders
        List<ShaderInfo> filteredShaders = FilterAndSortShaders();

        if (filteredShaders.Count == 0)
        {
            EditorGUILayout.HelpBox("No shaders found with the current filter settings.", MessageType.Info);
        }
        else
        {
            foreach (var shader in filteredShaders)
            {
                DrawShaderItem(shader);
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawShaderItem(ShaderInfo shader)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // Shader header
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(shader.Name, _boldLabelStyle);

        GUILayout.FlexibleSpace();

        // Usage count label
        GUIStyle usageStyle = shader.UsageCount > 10 ? _warningLabelStyle : EditorStyles.label;
        EditorGUILayout.LabelField("Used: " + shader.UsageCount, usageStyle, GUILayout.Width(70));

        // Action buttons
        if (GUILayout.Button(_pingIcon, _iconButtonStyle))
        {
            PingShader(shader);
        }

        if (GUILayout.Button(_selectIcon, _iconButtonStyle))
        {
            SelectShader(shader);
        }

        EditorGUILayout.EndHorizontal();

        // Shader details
        if (shader.RenderQueue > 0)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Render Queue: " + shader.RenderQueue);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        if (_showAdvancedInfo)
        {
            // Pass count
            if (shader.PassCount > 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Pass Count: " + shader.PassCount);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            // Is instanced
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Instancing: " + (shader.SupportsInstancing ? "Supported" : "Not Supported"));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        // Materials using this shader (collapsible)
        if (shader.MaterialCount > 0)
        {
            EditorGUILayout.BeginHorizontal();
            shader.ShowMaterials = EditorGUILayout.Foldout(shader.ShowMaterials, "Materials ("+shader.MaterialCount+")", true);
            EditorGUILayout.EndHorizontal();

            if (shader.ShowMaterials)
            {
                EditorGUI.indentLevel++;
                foreach (var material in shader.Materials)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(10);
                    GUILayout.Label(_materialIcon, GUILayout.Width(20), GUILayout.Height(20));
                    EditorGUILayout.LabelField(material.Name);
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button(_pingIcon, _iconButtonStyle))
                    {
                        PingMaterial(material);
                    }

                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }
        }

        // Keywords (collapsible)
        if (shader.Keywords != null && shader.Keywords.Count > 0)
        {
            EditorGUILayout.BeginHorizontal();
            shader.ShowKeywords = EditorGUILayout.Foldout(shader.ShowKeywords, "Keywords ("+shader.Keywords.Count+")", true);
            EditorGUILayout.EndHorizontal();

            if (shader.ShowKeywords)
            {
                EditorGUI.indentLevel++;
                foreach (var keyword in shader.Keywords)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(10);
                    EditorGUILayout.LabelField(keyword);
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(2);
    }

    private void DrawTexturesTab()
    {
        EditorGUILayout.BeginVertical();

        // Header and search
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Textures Analysis", _headerStyle);
        EditorGUILayout.EndHorizontal();

        // Controls and filtering
        EditorGUILayout.BeginHorizontal();
        _textureSearchFilter = EditorGUILayout.TextField(_textureSearchFilter, _searchBoxStyle);

        if (GUILayout.Button("Clear", _buttonStyle, GUILayout.Width(50)))
        {
            _textureSearchFilter = "";
        }

        _showUsedAssetsOnly = GUILayout.Toggle(_showUsedAssetsOnly, "Used Only", _buttonStyle, GUILayout.Width(85));
        _groupBySize = GUILayout.Toggle(_groupBySize, "Group by Size", _buttonStyle, GUILayout.Width(110));

        EditorGUILayout.EndHorizontal();

        // Sorting options
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Sort by:", GUILayout.Width(50));

        if (GUILayout.Toggle(_textureSortOption == SortOption.Name, "Name", _buttonStyle, GUILayout.Width(60)))
            _textureSortOption = SortOption.Name;

        if (GUILayout.Toggle(_textureSortOption == SortOption.Size, "Size", _buttonStyle, GUILayout.Width(60)))
            _textureSortOption = SortOption.Size;

        if (GUILayout.Toggle(_textureSortOption == SortOption.UsageCount, "Usage", _buttonStyle, GUILayout.Width(60)))
            _textureSortOption = SortOption.UsageCount;

        if (GUILayout.Toggle(_textureSortOption == SortOption.Type, "Type", _buttonStyle, GUILayout.Width(60)))
            _textureSortOption = SortOption.Type;

        GUILayout.FlexibleSpace();

        EditorGUILayout.LabelField(_textureSortAscending ? "↑" : "↓", GUILayout.Width(15));
        if (GUILayout.Button("Reverse", _buttonStyle, GUILayout.Width(65)))
            _textureSortAscending = !_textureSortAscending;

        if (GUILayout.Button("Select All", _buttonStyle, GUILayout.Width(80)))
            SelectAllFilteredTextures();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Textures list
        _textureScrollPosition = EditorGUILayout.BeginScrollView(_textureScrollPosition);

        // Get filtered & sorted textures
        List<TextureInfo> filteredTextures = FilterAndSortTextures();

        if (filteredTextures.Count == 0)
        {
            EditorGUILayout.HelpBox("No textures found with the current filter settings.", MessageType.Info);
        }
        else
        {
            // Group by size category if needed
            if (_groupBySize)
            {
                // Define size categories
                string[] sizeCategories = new string[] 
                {
                    "Very Large (100MB+)",
                    "Large (10MB - 100MB)",
                    "Medium (1MB - 10MB)",
                    "Small (100KB - 1MB)",
                    "Very Small (<100KB)"
                };

                Dictionary<string, List<TextureInfo>> texturesBySize = new Dictionary<string, List<TextureInfo>>();

                foreach (var category in sizeCategories)
                {
                    texturesBySize[category] = new List<TextureInfo>();
                }

                // Sort textures into categories
                foreach (var texture in filteredTextures)
                {
                    if (texture.MemorySize >= 100 * 1024 * 1024) // 100MB+
                        texturesBySize["Very Large (100MB+)"].Add(texture);
                    else if (texture.MemorySize >= 10 * 1024 * 1024) // 10MB - 100MB
                        texturesBySize["Large (10MB - 100MB)"].Add(texture);
                    else if (texture.MemorySize >= 1 * 1024 * 1024) // 1MB - 10MB
                        texturesBySize["Medium (1MB - 10MB)"].Add(texture);
                    else if (texture.MemorySize >= 100 * 1024) // 100KB - 1MB
                        texturesBySize["Small (100KB - 1MB)"].Add(texture);
                    else // <100KB
                        texturesBySize["Very Small (<100KB)"].Add(texture);
                }

                // Display textures by category
                foreach (var category in sizeCategories)
                {
                    var textures = texturesBySize[category];

                    if (textures.Count > 0)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                        // Category header
                        EditorGUILayout.BeginHorizontal();
                        bool expanded = EditorGUILayout.Foldout(true, "", true);
                        EditorGUILayout.LabelField(category + " ("+textures.Count+" textures)", _boldLabelStyle);
                        EditorGUILayout.EndHorizontal();

                        if (expanded)
                        {
                            foreach (var texture in textures)
                            {
                                DrawTextureItem(texture);
                            }
                        }

                        EditorGUILayout.EndVertical();
                        EditorGUILayout.Space(2);
                    }
                }
            }
            else
            {
                // Normal list view
                foreach (var texture in filteredTextures)
                {
                    DrawTextureItem(texture);
                }
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawTextureItem(TextureInfo texture)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // Texture header
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(texture.TextureName, _boldLabelStyle);

        GUILayout.FlexibleSpace();

        // Memory size
        if (texture.MemorySize > 0)
        {
            GUIStyle memoryStyle = GetMemorySizeStyle(texture.MemorySize);
            EditorGUILayout.LabelField(EditorUtility.FormatBytes(texture.MemorySize), memoryStyle, GUILayout.Width(90));
        }

        // Usage count label
        GUIStyle usageStyle = texture.UsageCount > 5 ? _warningLabelStyle : EditorStyles.label;
        EditorGUILayout.LabelField("Used: " + texture.UsageCount, usageStyle, GUILayout.Width(60));

        // Action buttons
        if (GUILayout.Button(_pingIcon, _iconButtonStyle))
        {
            PingTexture(texture);
        }

        if (GUILayout.Button(_selectIcon, _iconButtonStyle))
        {
            SelectTexture(texture);
        }

        EditorGUILayout.EndHorizontal();

        // Texture details
        EditorGUILayout.BeginHorizontal();

        // Size display
        if (texture.Width > 0 && texture.Height > 0)
        {
            GUIStyle sizeStyle = (texture.Width > _textureSizeWarningThreshold || texture.Height > _textureSizeWarningThreshold) ? 
                                 _warningLabelStyle : EditorStyles.label;
            EditorGUILayout.LabelField(texture.Width + "×" + texture.Height, sizeStyle);
        }

        // Format display
        if (!string.IsNullOrEmpty(texture.Format))
        {
            EditorGUILayout.LabelField(texture.Format);
        }

        // MipMaps
        if (texture.MipMapCount > 1)
        {
            EditorGUILayout.LabelField("MipMaps: " + texture.MipMapCount);
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        // Issues indicator
        if (texture.Issues.Count > 0)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(_warningIcon, GUILayout.Width(20), GUILayout.Height(20));
            EditorGUILayout.LabelField(string.Join(", ", texture.Issues), _warningLabelStyle);
            EditorGUILayout.EndHorizontal();
        }

        // Materials using this texture (collapsible)
        if (texture.Materials.Count > 0)
        {
            EditorGUILayout.BeginHorizontal();
            texture.ShowMaterials = EditorGUILayout.Foldout(texture.ShowMaterials, "Materials ("+texture.Materials.Count+")", true);
            EditorGUILayout.EndHorizontal();

            if (texture.ShowMaterials)
            {
                EditorGUI.indentLevel++;
                foreach (var material in texture.Materials)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(10);
                    GUILayout.Label(_materialIcon, GUILayout.Width(20), GUILayout.Height(20));
                    EditorGUILayout.LabelField(material.Name);
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button(_pingIcon, _iconButtonStyle))
                    {
                        PingMaterial(material);
                    }

                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(2);
    }

    private void DrawSettingsTab()
    {
        EditorGUILayout.BeginVertical();

        EditorGUILayout.LabelField("Analysis Settings", _headerStyle);
        EditorGUILayout.Space(5);

        // General settings
        EditorGUILayout.LabelField("General Settings", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        _autoRefresh = EditorGUILayout.Toggle("Auto-refresh on Scene Change", _autoRefresh);
        _showSceneMaterialsOnly = EditorGUILayout.Toggle("Show Scene Materials Only", _showSceneMaterialsOnly);
        _showUsedAssetsOnly = EditorGUILayout.Toggle("Show Used Assets Only", _showUsedAssetsOnly);
        _showAdvancedInfo = EditorGUILayout.Toggle("Show Advanced Information", _showAdvancedInfo);

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // Warning thresholds
        EditorGUILayout.LabelField("Warning Thresholds", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        _textureSizeWarningThreshold = EditorGUILayout.IntField("Texture Size Warning (px)", _textureSizeWarningThreshold);
        _textureSizeErrorThreshold = EditorGUILayout.IntField("Texture Size Error (px)", _textureSizeErrorThreshold);
        _materialCountWarningThreshold = EditorGUILayout.IntField("Material Count Warning", _materialCountWarningThreshold);
        _shaderVariantWarningThreshold = EditorGUILayout.IntField("Shader Variant Warning", _shaderVariantWarningThreshold);
        _vertexCountWarningThreshold = EditorGUILayout.IntField("Vertex Count Warning", _vertexCountWarningThreshold);
        _vertexCountErrorThreshold = EditorGUILayout.IntField("Vertex Count Error", _vertexCountErrorThreshold);
        _triangleCountWarningThreshold = EditorGUILayout.IntField("Triangle Count Warning", _triangleCountWarningThreshold);
        _triangleCountErrorThreshold = EditorGUILayout.IntField("Triangle Count Error", _triangleCountErrorThreshold);

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // Color settings
        EditorGUILayout.LabelField("Color Settings", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        _warningColor = EditorGUILayout.ColorField("Warning Color", _warningColor);
        _errorColor = EditorGUILayout.ColorField("Error Color", _errorColor);
        _goodColor = EditorGUILayout.ColorField("Good Color", _goodColor);

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(15);

        // Actions
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Refresh Analysis", GUILayout.Height(30)))
        {
            PerformAnalysis();
        }

        if (GUILayout.Button("Reset to Defaults", GUILayout.Height(30)))
        {
            ResetSettings();
        }

        if (GUILayout.Button("Export Settings", GUILayout.Height(30)))
        {
            ExportSettings();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // About section
        EditorGUILayout.LabelField("About", _headerStyle);
        EditorGUILayout.HelpBox("Material Analyzer v1.0\n\nA comprehensive tool for analyzing and organizing materials, shaders, and textures in your Unity project.\n\nThis tool helps you identify potential performance issues, optimize your project, and maintain clean asset organization.", MessageType.Info);

        EditorGUILayout.EndVertical();
    }

    // Helper methods for UI
    private GUIStyle GetMemorySizeStyle(long memorySize)
    {
        if (memorySize > 50 * 1024 * 1024) // 50MB
            return _errorLabelStyle;
        else if (memorySize > 10 * 1024 * 1024) // 10MB
            return _warningLabelStyle;
        else
            return EditorStyles.label;
    }

    // Filtering and sorting methods
    private List<MaterialInfo> FilterAndSortMaterials()
    {
        // Apply filters
        var filtered = _materials;

        // First apply selection filter if active
        if (_filterBySelection)
        {
            if (_filteredMaterials.Count > 0)
            {
                filtered = filtered.Where(m => _filteredMaterials.Contains(m.Material)).ToList();
            }
            else if (_filteredGameObjects.Count > 0)
            {
                // Get all renderers from the filtered GameObjects
                List<Renderer> renderers = new List<Renderer>();
                foreach (var go in _filteredGameObjects)
                {
                    renderers.AddRange(go.GetComponentsInChildren<Renderer>());
                }

                // Get all materials from the renderers
                HashSet<Material> materials = new HashSet<Material>();
                foreach (var renderer in renderers)
                {
                    foreach (var material in renderer.sharedMaterials)
                    {
                        if (material != null)
                            materials.Add(material);
                    }
                }

                filtered = filtered.Where(m => materials.Contains(m.Material)).ToList();
            }
            else if (_focusedMaterial != null)
            {
                filtered = filtered.Where(m => m.Material == _focusedMaterial).ToList();
            }
        }
        else if (_showSceneMaterialsOnly)
        {
            filtered = filtered.Where(m => m.UsageCount > 0).ToList();
        }

        if (!string.IsNullOrEmpty(_materialSearchFilter))
        {
            string searchLower = _materialSearchFilter.ToLowerInvariant();
            filtered = filtered.Where(m => 
                m.Name.ToLowerInvariant().Contains(searchLower) || 
                (m.ShaderName != null && m.ShaderName.ToLowerInvariant().Contains(searchLower))
            ).ToList();
        }

        // Apply sorting
        switch (_materialSortOption)
        {
            case SortOption.Name:
                filtered = _materialSortAscending ? 
                    filtered.OrderBy(m => m.Name).ToList() : 
                    filtered.OrderByDescending(m => m.Name).ToList();
                break;
            case SortOption.UsageCount:
                filtered = _materialSortAscending ? 
                    filtered.OrderBy(m => m.UsageCount).ToList() : 
                    filtered.OrderByDescending(m => m.UsageCount).ToList();
                break;
            case SortOption.Type:
                filtered = _materialSortAscending ? 
                    filtered.OrderBy(m => m.ShaderName).ToList() : 
                    filtered.OrderByDescending(m => m.ShaderName).ToList();
                break;
        }

        return filtered;
    }

    private List<ShaderInfo> FilterAndSortShaders()
    {
        // Apply filters
        var filtered = _shaders;

        if (_showUsedAssetsOnly)
        {
            filtered = filtered.Where(s => s.UsageCount > 0).ToList();
        }

        if (!string.IsNullOrEmpty(_shaderSearchFilter))
        {
            string searchLower = _shaderSearchFilter.ToLowerInvariant();
            filtered = filtered.Where(s => s.Name.ToLowerInvariant().Contains(searchLower)).ToList();
        }

        // Apply sorting
        switch (_shaderSortOption)
        {
            case SortOption.Name:
                filtered = _shaderSortAscending ? 
                    filtered.OrderBy(s => s.Name).ToList() : 
                    filtered.OrderByDescending(s => s.Name).ToList();
                break;
            case SortOption.UsageCount:
                filtered = _shaderSortAscending ? 
                    filtered.OrderBy(s => s.UsageCount).ToList() : 
                    filtered.OrderByDescending(s => s.UsageCount).ToList();
                break;
        }

        return filtered;
    }

    private List<TextureInfo> FilterAndSortTextures()
    {
        // Apply filters
        var filtered = _textures;

        if (_showUsedAssetsOnly)
        {
            filtered = filtered.Where(t => t.UsageCount > 0).ToList();
        }

        if (!string.IsNullOrEmpty(_textureSearchFilter))
        {
            string searchLower = _textureSearchFilter.ToLowerInvariant();
            filtered = filtered.Where(t => t.TextureName.ToLowerInvariant().Contains(searchLower)).ToList();
        }

        // Apply sorting
        switch (_textureSortOption)
        {
            case SortOption.Name:
                filtered = _textureSortAscending ? 
                    filtered.OrderBy(t => t.TextureName).ToList() : 
                    filtered.OrderByDescending(t => t.TextureName).ToList();
                break;
            case SortOption.Size:
                filtered = _textureSortAscending ? 
                    filtered.OrderBy(t => t.MemorySize).ToList() : 
                    filtered.OrderByDescending(t => t.MemorySize).ToList();
                break;
            case SortOption.UsageCount:
                filtered = _textureSortAscending ? 
                    filtered.OrderBy(t => t.UsageCount).ToList() : 
                    filtered.OrderByDescending(t => t.UsageCount).ToList();
                break;
            case SortOption.Type:
                filtered = _textureSortAscending ? 
                    filtered.OrderBy(t => t.Format).ToList() : 
                    filtered.OrderByDescending(t => t.Format).ToList();
                break;
        }

        return filtered;
    }

    // Analysis methods
    private void PerformAnalysis()
    {
        AnalyzeSceneMaterials();
        AnalyzeProjectTextures();
        AnalyzeMeshes();
        CalculateOverview();
        Repaint();
    }

    private void ReAnalyzeAll()
    {
        EditorUtility.DisplayProgressBar("Re-Analyzing Project", "Clearing previous data...", 0.0f);

        // Clear all existing data
        _materials.Clear();
        _shaders.Clear();
        _textures.Clear();
        _meshes.Clear();

        // Force a full project scan
        EditorUtility.DisplayProgressBar("Re-Analyzing Project", "Analyzing scene materials...", 0.2f);
        AnalyzeSceneMaterials();

        EditorUtility.DisplayProgressBar("Re-Analyzing Project", "Analyzing project textures...", 0.4f);
        AnalyzeProjectTextures();

        EditorUtility.DisplayProgressBar("Re-Analyzing Project", "Analyzing project meshes...", 0.6f);
        AnalyzeMeshes(true); // Force complete scan

        EditorUtility.DisplayProgressBar("Re-Analyzing Project", "Calculating overview...", 0.8f);
        CalculateOverview();

        EditorUtility.ClearProgressBar();
        Repaint();

        // Show a confirmation dialog
        EditorUtility.DisplayDialog("Analysis Complete", 
            "Re-analysis of all project assets completed.\n\n" +
            "Found:\n" +
            "- " + _materials.Count + " materials\n" +
            "- " + _shaders.Count + " shaders\n" +
            "- " + _textures.Count + " textures\n" +
            "- " + _meshes.Count + " meshes", 
            "OK");
    }

    private void AnalyzeSceneMaterials()
    {
        _materials.Clear();
        _shaders.Clear();

        // Get all renderers in the scene
        Renderer[] renderers = GameObject.FindObjectsByType<Renderer>(FindObjectsSortMode.None);

        // Dictionary to keep track of material instances
        Dictionary<Material, int> materialUsage = new Dictionary<Material, int>();
        Dictionary<Shader, int> shaderUsage = new Dictionary<Shader, int>();
        Dictionary<Texture, int> textureUsage = new Dictionary<Texture, int>();

        // Count material usage
        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.sharedMaterials;

            foreach (Material material in materials)
            {
                if (material == null) continue;

                // Increment material usage count
                if (!materialUsage.ContainsKey(material))
                    materialUsage[material] = 0;
                materialUsage[material]++;

                // Increment shader usage count
                Shader shader = material.shader;
                if (shader != null)
                {
                    if (!shaderUsage.ContainsKey(shader))
                        shaderUsage[shader] = 0;
                    shaderUsage[shader]++;
                }

                // Find textures in material properties
                var propertyCount = ShaderUtil.GetPropertyCount(shader);
                for (int i = 0; i < propertyCount; i++)
                {
                    if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                    {
                        string propertyName = ShaderUtil.GetPropertyName(shader, i);
                        Texture texture = material.GetTexture(propertyName);

                        if (texture != null)
                        {
                            if (!textureUsage.ContainsKey(texture))
                                textureUsage[texture] = 0;
                            textureUsage[texture]++;
                        }
                    }
                }
            }
        }

        // Create MaterialInfo objects
        foreach (var materialEntry in materialUsage)
        {
            Material material = materialEntry.Key;
            int usageCount = materialEntry.Value;

            MaterialInfo materialInfo = new MaterialInfo
            {
                Name = material.name,
                Material = material,
                UsageCount = usageCount,
                ShaderName = material.shader != null ? material.shader.name : "Unknown",
                Keywords = new List<string>(material.shaderKeywords),
                Path = AssetDatabase.GetAssetPath(material),
                Textures = new List<TextureInfo>(),
                ShowTextures = false,
                ShowKeywords = false
            };

            // Get textures used by this material
            var shader = material.shader;
            if (shader != null)
            {
                var propertyCount = ShaderUtil.GetPropertyCount(shader);
                for (int i = 0; i < propertyCount; i++)
                {
                    if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                    {
                        string propertyName = ShaderUtil.GetPropertyName(shader, i);
                        Texture texture = material.GetTexture(propertyName);

                        if (texture != null)
                        {
                            TextureInfo textureInfo = new TextureInfo
                            {
                                TextureName = texture.name,
                                Texture = texture,
                                UsageCount = textureUsage[texture],
                                Path = AssetDatabase.GetAssetPath(texture),
                                Materials = new List<MaterialInfo>(),
                                Issues = new List<string>(),
                                ShowMaterials = false
                            };

                            // Get texture details if possible
                            if (texture is Texture2D texture2D)
                            {
                                textureInfo.Width = texture2D.width;
                                textureInfo.Height = texture2D.height;
                                textureInfo.Format = texture2D.format.ToString();
                                textureInfo.MipMapCount = texture2D.mipmapCount;

                                // Calculate memory size
                                long memorySize = (long)texture2D.width * texture2D.height;
                                int bytesPerPixel = GetBytesPerPixel(texture2D.format);
                                memorySize *= bytesPerPixel;

                                // Add mipmap sizes if present
                                if (texture2D.mipmapCount > 1)
                                {
                                    float mipmapSizeFactor = 1.33f; // Accounts for all mipmap levels (1 + 1/4 + 1/16 + ...)
                                    memorySize = (long)(memorySize * mipmapSizeFactor);
                                }

                                textureInfo.MemorySize = memorySize;

                                // Check for issues
                                if (texture2D.width > _textureSizeErrorThreshold || texture2D.height > _textureSizeErrorThreshold)
                                {
                                    textureInfo.Issues.Add("Oversized texture");
                                }
                                else if (texture2D.width > _textureSizeWarningThreshold || texture2D.height > _textureSizeWarningThreshold)
                                {
                                    textureInfo.Issues.Add("Large texture");
                                }

                                if (!IsPowerOfTwo(texture2D.width) || !IsPowerOfTwo(texture2D.height))
                                {
                                    textureInfo.Issues.Add("Non-power-of-two");
                                }

                                if (texture2D.format == TextureFormat.RGBA32 || 
                                    texture2D.format == TextureFormat.ARGB32 || 
                                    texture2D.format == TextureFormat.RGB24)
                                {
                                    textureInfo.Issues.Add("Uncompressed");
                                }
                            }

                            materialInfo.Textures.Add(textureInfo);
                            textureInfo.Materials.Add(materialInfo);
                        }
                    }
                }

                materialInfo.TextureCount = materialInfo.Textures.Count;
            }

            _materials.Add(materialInfo);
        }

        // Create ShaderInfo objects
        foreach (var shaderEntry in shaderUsage)
        {
            Shader shader = shaderEntry.Key;
            int usageCount = shaderEntry.Value;

            ShaderInfo shaderInfo = new ShaderInfo
            {
                Name = shader.name,
                Shader = shader,
                UsageCount = usageCount,
                RenderQueue = shader.renderQueue,
                Path = AssetDatabase.GetAssetPath(shader),
                Materials = new List<MaterialInfo>(),
                Keywords = new List<string>(),
                ShowMaterials = false,
                ShowKeywords = false
            };

            // Get additional shader info if possible
            try
            {
                // Use reflection to get pass count if available
                System.Type shaderUtilType = typeof(ShaderUtil);
                var getPassCountMethod = shaderUtilType.GetMethod("GetPassCount", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                if (getPassCountMethod != null)
                    shaderInfo.PassCount = (int)getPassCountMethod.Invoke(null, new object[] { shader });

                // Check if shader supports instancing through properties
                shaderInfo.SupportsInstancing = shader.FindPropertyIndex("_MainTex") != -1;
            }
            catch (System.Exception)
            {
                // Some built-in shaders might not support these utilities
            }

            // Get all materials using this shader
            foreach (var materialInfo in _materials)
            {
                if (materialInfo.ShaderName == shader.name)
                {
                    shaderInfo.Materials.Add(materialInfo);

                    // Add all keywords from this material to the shader keywords list
                    foreach (var keyword in materialInfo.Keywords)
                    {
                        if (!shaderInfo.Keywords.Contains(keyword))
                        {
                            shaderInfo.Keywords.Add(keyword);
                        }
                    }
                }
            }

            shaderInfo.MaterialCount = shaderInfo.Materials.Count;

            _shaders.Add(shaderInfo);
        }

        // Process textures
        _textures = new List<TextureInfo>();
        HashSet<Texture> processedTextures = new HashSet<Texture>();

        // First add textures found in materials
        foreach (var materialInfo in _materials)
        {
            foreach (var textureInfo in materialInfo.Textures)
            {
                if (!processedTextures.Contains(textureInfo.Texture))
                {
                    _textures.Add(textureInfo);
                    processedTextures.Add(textureInfo.Texture);
                }
            }
        }
    }

    private void AnalyzeProjectTextures()
    {
        // Find all textures in project (including those not in scene)
        string[] guids = AssetDatabase.FindAssets("t:Texture");

        HashSet<string> processedTexturePaths = new HashSet<string>();

        // Add paths of textures we've already found
        foreach (var textureInfo in _textures)
        {
            if (!string.IsNullOrEmpty(textureInfo.Path))
            {
                processedTexturePaths.Add(textureInfo.Path);
            }
        }

        // Process remaining textures
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (processedTexturePaths.Contains(path))
                continue;

            Texture texture = AssetDatabase.LoadAssetAtPath<Texture>(path);
            if (texture == null)
                continue;

            TextureInfo textureInfo = new TextureInfo
            {
                TextureName = texture.name,
                Texture = texture,
                UsageCount = 0, // Not used in scene
                Path = path,
                Materials = new List<MaterialInfo>(),
                Issues = new List<string>(),
                ShowMaterials = false
            };

            // Get texture details if possible
            if (texture is Texture2D texture2D)
            {
                textureInfo.Width = texture2D.width;
                textureInfo.Height = texture2D.height;
                textureInfo.Format = texture2D.format.ToString();
                textureInfo.MipMapCount = texture2D.mipmapCount;

                // Calculate memory size
                long memorySize = (long)texture2D.width * texture2D.height;
                int bytesPerPixel = GetBytesPerPixel(texture2D.format);
                memorySize *= bytesPerPixel;

                // Add mipmap sizes if present
                if (texture2D.mipmapCount > 1)
                {
                    float mipmapSizeFactor = 1.33f; // Accounts for all mipmap levels
                    memorySize = (long)(memorySize * mipmapSizeFactor);
                }

                textureInfo.MemorySize = memorySize;

                // Check for issues
                if (texture2D.width > _textureSizeErrorThreshold || texture2D.height > _textureSizeErrorThreshold)
                {
                    textureInfo.Issues.Add("Oversized texture");
                }
                else if (texture2D.width > _textureSizeWarningThreshold || texture2D.height > _textureSizeWarningThreshold)
                {
                    textureInfo.Issues.Add("Large texture");
                }

                if (!IsPowerOfTwo(texture2D.width) || !IsPowerOfTwo(texture2D.height))
                {
                    textureInfo.Issues.Add("Non-power-of-two");
                }

                if (texture2D.format == TextureFormat.RGBA32 || 
                    texture2D.format == TextureFormat.ARGB32 || 
                    texture2D.format == TextureFormat.RGB24)
                {
                    textureInfo.Issues.Add("Uncompressed");
                }
            }

            _textures.Add(textureInfo);
        }
    }

    private void CalculateOverview()
    {
        _overview.MaterialCount = 0;
        _overview.UniqueMaterialCount = _materials.Count;
        _overview.ShaderCount = _shaders.Count;
        _overview.TextureCount = _textures.Count;
        _overview.TotalTextureMemory = 0;
        _overview.OversizedTextureCount = 0;
        _overview.NonPowerOfTwoTextureCount = 0;
        _overview.UncompressedTextureCount = 0;
        _overview.DuplicateMaterialCount = 0;

        // Initialize mesh overview data
        _overview.MeshCount = 0;
        _overview.UniqueMeshCount = _meshes.Count;
        _overview.TotalVertexCount = 0;
        _overview.TotalTriangleCount = 0;
        _overview.TotalMeshMemory = 0;
        _overview.HighPolyMeshCount = 0;
        _overview.NonReadableMeshCount = 0;
        _overview.MissingNormalsMeshCount = 0;
        _overview.MissingTangentsMeshCount = 0;
        _overview.MissingUVMeshCount = 0;
        _overview.OptimizableMeshCount = 0;

        // Count materials used in scene
        foreach (var material in _materials)
        {
            _overview.MaterialCount += material.UsageCount;
        }

        // Count shader keywords
        HashSet<string> uniqueKeywords = new HashSet<string>();
        foreach (var shader in _shaders)
        {
            foreach (var keyword in shader.Keywords)
            {
                uniqueKeywords.Add(keyword);
            }
        }
        _overview.ShaderKeywordCount = uniqueKeywords.Count;

        // Process textures
        foreach (var texture in _textures)
        {
            // Add to total memory
            _overview.TotalTextureMemory += texture.MemorySize;

            // Count issues
            if (texture.Width > _textureSizeErrorThreshold || texture.Height > _textureSizeErrorThreshold)
            {
                _overview.OversizedTextureCount++;
            }

            if (!IsPowerOfTwo(texture.Width) || !IsPowerOfTwo(texture.Height))
            {
                _overview.NonPowerOfTwoTextureCount++;
            }

            if (texture.Format == "RGBA32" || texture.Format == "ARGB32" || texture.Format == "RGB24")
            {
                _overview.UncompressedTextureCount++;
            }
        }

        // Detect potential duplicate materials
        Dictionary<string, int> shaderMaterialCounts = new Dictionary<string, int>();
        foreach (var material in _materials)
        {
            if (!shaderMaterialCounts.ContainsKey(material.ShaderName))
                shaderMaterialCounts[material.ShaderName] = 0;

            shaderMaterialCounts[material.ShaderName]++;
        }

        foreach (var entry in shaderMaterialCounts)
        {
            if (entry.Value > 3) // If more than 3 materials use the same shader, some might be duplicates
            {
                _overview.DuplicateMaterialCount += entry.Value - 1; // Assume at least one is needed
            }
        }

        // Estimate batches
        _overview.EstimatedBatches = _materials.Count; // Worst case, one batch per material
        _overview.MaterialSwitchesCount = _overview.MaterialCount - _overview.UniqueMaterialCount;

        // Process mesh data
        foreach (var mesh in _meshes)
        {
            _overview.MeshCount += mesh.InstanceCount;
            _overview.TotalVertexCount += mesh.VertexCount * Mathf.Max(1, mesh.InstanceCount);
            _overview.TotalTriangleCount += mesh.TriangleCount * Mathf.Max(1, mesh.InstanceCount);
            _overview.TotalMeshMemory += mesh.MemorySize;

            if (mesh.VertexCount > _vertexCountWarningThreshold)
                _overview.HighPolyMeshCount++;

            if (!mesh.IsReadable)
                _overview.NonReadableMeshCount++;

            if (!mesh.HasNormals)
                _overview.MissingNormalsMeshCount++;

            if (!mesh.HasTangents && mesh.HasUV)
                _overview.MissingTangentsMeshCount++;

            if (!mesh.HasUV)
                _overview.MissingUVMeshCount++;

            if (!mesh.IsOptimized && mesh.IsReadable)
                _overview.OptimizableMeshCount++;
        }
    }

    // Utility methods
    private int GetBytesPerPixel(TextureFormat format)
    {
        switch (format)
        {
            case TextureFormat.Alpha8: return 1;
            case TextureFormat.ARGB4444: return 2;
            case TextureFormat.RGB24: return 3;
            case TextureFormat.RGBA32: return 4;
            case TextureFormat.ARGB32: return 4;
            case TextureFormat.RGB565: return 2;
            case TextureFormat.DXT1: return 1; // Compressed (approximate)
            case TextureFormat.DXT5: return 1; // Compressed (approximate)
            case TextureFormat.PVRTC_RGB2: return 1; // Compressed (approximate)
            case TextureFormat.PVRTC_RGBA2: return 1; // Compressed (approximate)
            case TextureFormat.PVRTC_RGB4: return 1; // Compressed (approximate)
            case TextureFormat.PVRTC_RGBA4: return 1; // Compressed (approximate)
            case TextureFormat.ETC_RGB4: return 1; // Compressed (approximate)
            case TextureFormat.ETC2_RGB: return 1; // Compressed (approximate)
            case TextureFormat.ETC2_RGBA1: return 1; // Compressed (approximate)
            case TextureFormat.ETC2_RGBA8: return 1; // Compressed (approximate)
            case TextureFormat.ASTC_4x4: return 1; // Compressed (approximate)
            case TextureFormat.ASTC_5x5: return 1; // Compressed (approximate)
            case TextureFormat.ASTC_6x6: return 1; // Compressed (approximate)
            case TextureFormat.ASTC_8x8: return 1; // Compressed (approximate)
            case TextureFormat.ASTC_10x10: return 1; // Compressed (approximate)
            case TextureFormat.ASTC_12x12: return 1; // Compressed (approximate)
            default: return 4; // Default to 4 bytes per pixel
        }
    }

    private bool IsPowerOfTwo(int x)
    {
        return (x & (x - 1)) == 0 && x > 0;
    }

    // Action methods
    private void PingMaterial(MaterialInfo material)
    {
        if (material.Material != null)
        {
            EditorGUIUtility.PingObject(material.Material);
        }
    }

    private void SelectMaterial(MaterialInfo material)
    {
        if (material.Material != null)
        {
            Selection.activeObject = material.Material;
        }
    }

    private void PingShader(ShaderInfo shader)
    {
        if (shader.Shader != null)
        {
            EditorGUIUtility.PingObject(shader.Shader);
        }
    }

    private void SelectShader(ShaderInfo shader)
    {
        if (shader.Shader != null)
        {
            Selection.activeObject = shader.Shader;
        }
    }

    private void PingTexture(TextureInfo texture)
    {
        if (texture.Texture != null)
        {
            EditorGUIUtility.PingObject(texture.Texture);
        }
    }

    private void SelectTexture(TextureInfo texture)
    {
        if (texture.Texture != null)
        {
            Selection.activeObject = texture.Texture;
        }
    }

    private void SelectAllMaterials()
    {
        List<UnityEngine.Object> allMaterials = new List<UnityEngine.Object>();
        foreach (var material in _materials)
        {
            if (material.Material != null)
            {
                allMaterials.Add(material.Material);
            }
        }

        if (allMaterials.Count > 0)
        {
            Selection.objects = allMaterials.ToArray();
        }
    }

    private void SelectAllFilteredMaterials()
    {
        List<UnityEngine.Object> filteredMaterials = new List<UnityEngine.Object>();
        List<MaterialInfo> materialInfos = FilterAndSortMaterials();

        foreach (var material in materialInfos)
        {
            if (material.Material != null)
            {
                filteredMaterials.Add(material.Material);
            }
        }

        if (filteredMaterials.Count > 0)
        {
            Selection.objects = filteredMaterials.ToArray();
        }
    }

    private void SelectAllFilteredShaders()
    {
        List<UnityEngine.Object> filteredShaders = new List<UnityEngine.Object>();
        List<ShaderInfo> shaderInfos = FilterAndSortShaders();

        foreach (var shader in shaderInfos)
        {
            if (shader.Shader != null)
            {
                filteredShaders.Add(shader.Shader);
            }
        }

        if (filteredShaders.Count > 0)
        {
            Selection.objects = filteredShaders.ToArray();
        }
    }

    private void SelectAllFilteredTextures()
    {
        List<UnityEngine.Object> filteredTextures = new List<UnityEngine.Object>();
        List<TextureInfo> textureInfos = FilterAndSortTextures();

        foreach (var texture in textureInfos)
        {
            if (texture.Texture != null)
            {
                filteredTextures.Add(texture.Texture);
            }
        }

        if (filteredTextures.Count > 0)
        {
            Selection.objects = filteredTextures.ToArray();
        }
    }

    private void SelectLargeTextures()
    {
        List<UnityEngine.Object> largeTextures = new List<UnityEngine.Object>();

        foreach (var texture in _textures)
        {
            if (texture.Width > _textureSizeWarningThreshold || 
                texture.Height > _textureSizeWarningThreshold ||
                texture.MemorySize > 10 * 1024 * 1024) // > 10MB
            {
                if (texture.Texture != null)
                {
                    largeTextures.Add(texture.Texture);
                }
            }
        }

        if (largeTextures.Count > 0)
        {
            Selection.objects = largeTextures.ToArray();
        }
    }

    private void FindUnusedMaterials()
    {
        List<UnityEngine.Object> unusedMaterials = new List<UnityEngine.Object>();

        foreach (var material in _materials)
        {
            if (material.UsageCount == 0 && material.Material != null)
            {
                unusedMaterials.Add(material.Material);
            }
        }

        if (unusedMaterials.Count > 0)
        {
            Selection.objects = unusedMaterials.ToArray();
            EditorUtility.DisplayDialog("Unused Materials", "Found " + unusedMaterials.Count + " unused materials. These are now selected in the project browser.", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Unused Materials", "No unused materials found in the project.", "OK");
        }
    }

    private void ExportReport()
    {
        string path = EditorUtility.SaveFilePanel("Export Material Analysis Report", "", "MaterialAnalysisReport", "html");

        if (string.IsNullOrEmpty(path))
            return;

        System.Text.StringBuilder reportBuilder = new System.Text.StringBuilder();

        // HTML header
        reportBuilder.AppendLine("<!DOCTYPE html>");
        reportBuilder.AppendLine("<html lang=\"en\">");
        reportBuilder.AppendLine("<head>");
        reportBuilder.AppendLine("    <meta charset=\"UTF-8\">");
        reportBuilder.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        reportBuilder.AppendLine("    <title>Unity Material Analysis Report</title>");
        reportBuilder.AppendLine("    <style>");
        reportBuilder.AppendLine("        body { font-family: Arial, sans-serif; margin: 0; padding: 20px; }");
        reportBuilder.AppendLine("        h1, h2, h3 { color: #333; }");
        reportBuilder.AppendLine("        .summary { display: flex; flex-wrap: wrap; margin-bottom: 20px; }");
        reportBuilder.AppendLine("        .summary-box { border: 1px solid #ddd; border-radius: 5px; padding: 15px; margin: 10px; min-width: 200px; }");
        reportBuilder.AppendLine("        .summary-box h3 { margin-top: 0; }");
        reportBuilder.AppendLine("        table { border-collapse: collapse; width: 100%; margin-bottom: 20px; }");
        reportBuilder.AppendLine("        th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
        reportBuilder.AppendLine("        th { background-color: #f2f2f2; }");
        reportBuilder.AppendLine("        tr:nth-child(even) { background-color: #f9f9f9; }");
        reportBuilder.AppendLine("        .warning { color: #ff9800; }");
        reportBuilder.AppendLine("        .error { color: #f44336; }");
        reportBuilder.AppendLine("        .good { color: #4caf50; }");
        reportBuilder.AppendLine("    </style>");
        reportBuilder.AppendLine("</head>");
        reportBuilder.AppendLine("<body>");

        // Header
        reportBuilder.AppendLine("    <h1>Unity Material Analysis Report</h1>");
        reportBuilder.AppendLine("    <p>Generated on " + System.DateTime.Now.ToString() + "</p>");

        // Summary
        reportBuilder.AppendLine("    <h2>Summary</h2>");
        reportBuilder.AppendLine("    <div class=\"summary\">");

        // Materials summary
        reportBuilder.AppendLine("        <div class=\"summary-box\">");
        reportBuilder.AppendLine("            <h3>Materials</h3>");
        reportBuilder.AppendLine("            <p>Total: " + _overview.MaterialCount + "</p>");
        reportBuilder.AppendLine("            <p>Unique: " + _overview.UniqueMaterialCount + "</p>");
        reportBuilder.AppendLine("        </div>");

        // Shaders summary
        reportBuilder.AppendLine("        <div class=\"summary-box\">");
        reportBuilder.AppendLine("            <h3>Shaders</h3>");
        reportBuilder.AppendLine("            <p>Count: " + _overview.ShaderCount + "</p>");
        reportBuilder.AppendLine("            <p>Keywords: " + _overview.ShaderKeywordCount + "</p>");
        reportBuilder.AppendLine("        </div>");

        // Textures summary
        reportBuilder.AppendLine("        <div class=\"summary-box\">");
        reportBuilder.AppendLine("            <h3>Textures</h3>");
        reportBuilder.AppendLine("            <p>Count: " + _overview.TextureCount + "</p>");
        reportBuilder.AppendLine("            <p>Memory: " + EditorUtility.FormatBytes(_overview.TotalTextureMemory) + "</p>");
        reportBuilder.AppendLine("        </div>");

        // Performance summary
        reportBuilder.AppendLine("        <div class=\"summary-box\">");
        reportBuilder.AppendLine("            <h3>Performance</h3>");
        reportBuilder.AppendLine("            <p>Est. Batches: " + _overview.EstimatedBatches + "</p>");
        reportBuilder.AppendLine("            <p>Material Switches: " + _overview.MaterialSwitchesCount + "</p>");
        reportBuilder.AppendLine("        </div>");

        reportBuilder.AppendLine("    </div>");

        // Issues
        reportBuilder.AppendLine("    <h2>Issues & Recommendations</h2>");
        reportBuilder.AppendLine("    <ul>");

        bool hasWarnings = false;

        // Material warnings
        if (_overview.MaterialCount > _materialCountWarningThreshold)
        {
            hasWarnings = true;
            reportBuilder.AppendLine("        <li class=\"warning\">High material count ("+_overview.MaterialCount+"). Consider consolidating materials.</li>");
        }

        // Texture size warnings
        if (_overview.OversizedTextureCount > 0)
        {
            hasWarnings = true;
            reportBuilder.AppendLine("        <li class=\"warning\">Found " + _overview.OversizedTextureCount + " oversized textures. Consider reducing their dimensions.</li>");
        }

        // Shader keyword warnings
        if (_overview.ShaderKeywordCount > _shaderVariantWarningThreshold)
        {
            hasWarnings = true;
            reportBuilder.AppendLine("        <li class=\"warning\">High shader variant count. Consider reducing shader complexity.</li>");
        }

        // Non-power of two textures
        if (_overview.NonPowerOfTwoTextureCount > 0)
        {
            hasWarnings = true;
            reportBuilder.AppendLine("        <li class=\"warning\">Found " + _overview.NonPowerOfTwoTextureCount + " non-power-of-two textures. May cause extra memory usage.</li>");
        }

        // Uncompressed textures
        if (_overview.UncompressedTextureCount > 0)
        {
            hasWarnings = true;
            reportBuilder.AppendLine("        <li class=\"warning\">Found " + _overview.UncompressedTextureCount + " uncompressed textures. Consider using texture compression.</li>");
        }

        // Duplicate materials
        if (_overview.DuplicateMaterialCount > 0)
        {
            hasWarnings = true;
            reportBuilder.AppendLine("        <li class=\"warning\">Found " + _overview.DuplicateMaterialCount + " possible duplicate materials. Consider consolidating them.</li>");
        }

        // No warnings
        if (!hasWarnings)
        {
            reportBuilder.AppendLine("        <li class=\"good\">No significant issues detected. Your scene looks optimized!</li>");
        }

        reportBuilder.AppendLine("    </ul>");

        // Materials table
        reportBuilder.AppendLine("    <h2>Materials</h2>");
        reportBuilder.AppendLine("    <table>");
        reportBuilder.AppendLine("        <tr>");
        reportBuilder.AppendLine("            <th>Name</th>");
        reportBuilder.AppendLine("            <th>Shader</th>");
        reportBuilder.AppendLine("            <th>Usage Count</th>");
        reportBuilder.AppendLine("            <th>Texture Count</th>");
        reportBuilder.AppendLine("        </tr>");

        foreach (var material in _materials.OrderByDescending(m => m.UsageCount))
        {
            reportBuilder.AppendLine("        <tr>");
            reportBuilder.AppendLine("            <td>" + material.Name + "</td>");
            reportBuilder.AppendLine("            <td>" + material.ShaderName + "</td>");
            reportBuilder.AppendLine("            <td>" + material.UsageCount + "</td>");
            reportBuilder.AppendLine("            <td>" + material.TextureCount + "</td>");
            reportBuilder.AppendLine("        </tr>");
        }

        reportBuilder.AppendLine("    </table>");

        // Textures table
        reportBuilder.AppendLine("    <h2>Textures</h2>");
        reportBuilder.AppendLine("    <table>");
        reportBuilder.AppendLine("        <tr>");
        reportBuilder.AppendLine("            <th>Name</th>");
        reportBuilder.AppendLine("            <th>Size</th>");
        reportBuilder.AppendLine("            <th>Format</th>");
        reportBuilder.AppendLine("            <th>Memory</th>");
        reportBuilder.AppendLine("            <th>Usage Count</th>");
        reportBuilder.AppendLine("            <th>Issues</th>");
        reportBuilder.AppendLine("        </tr>");

        foreach (var texture in _textures.OrderByDescending(t => t.MemorySize))
        {
            string sizeClass = "";
            if (texture.Width > _textureSizeErrorThreshold || texture.Height > _textureSizeErrorThreshold)
                sizeClass = "error";
            else if (texture.Width > _textureSizeWarningThreshold || texture.Height > _textureSizeWarningThreshold)
                sizeClass = "warning";

            string memoryClass = "";
            if (texture.MemorySize > 50 * 1024 * 1024) // 50MB
                memoryClass = "error";
            else if (texture.MemorySize > 10 * 1024 * 1024) // 10MB
                memoryClass = "warning";

            reportBuilder.AppendLine("        <tr>");
            reportBuilder.AppendLine("            <td>" + texture.TextureName + "</td>");
            reportBuilder.AppendLine("            <td class=\"" + sizeClass + "\">" + texture.Width + "×" + texture.Height + "</td>");
            reportBuilder.AppendLine("            <td>" + texture.Format + "</td>");
            reportBuilder.AppendLine("            <td class=\"" + memoryClass + "\">" + EditorUtility.FormatBytes(texture.MemorySize) + "</td>");
            reportBuilder.AppendLine("            <td>" + texture.UsageCount + "</td>");
            reportBuilder.AppendLine("            <td class=\"" + (texture.Issues.Count > 0 ? "warning" : "") + "\">" + string.Join(", ", texture.Issues) + "</td>");
            reportBuilder.AppendLine("        </tr>");
        }

        reportBuilder.AppendLine("    </table>");

        // HTML footer
        reportBuilder.AppendLine("    <footer>");
        reportBuilder.AppendLine("        <p>Generated by Material Analyzer for Unity</p>");
        reportBuilder.AppendLine("    </footer>");
        reportBuilder.AppendLine("</body>");
        reportBuilder.AppendLine("</html>");

        // Write to file
        System.IO.File.WriteAllText(path, reportBuilder.ToString());

        // Open the file
        EditorUtility.RevealInFinder(path);
        EditorUtility.DisplayDialog("Export Complete", "Report exported successfully to:\n" + path, "OK");
    }

    private void ExportSettings()
    {
        string path = EditorUtility.SaveFilePanel("Export Material Analyzer Settings", "", "MaterialAnalyzerSettings", "json");

        if (string.IsNullOrEmpty(path))
            return;

        MaterialAnalyzerSettings settings = new MaterialAnalyzerSettings
        {
            AutoRefresh = _autoRefresh,
            ShowSceneMaterialsOnly = _showSceneMaterialsOnly,
            ShowUsedAssetsOnly = _showUsedAssetsOnly,
            ShowAdvancedInfo = _showAdvancedInfo,
            TextureSizeWarningThreshold = _textureSizeWarningThreshold,
            TextureSizeErrorThreshold = _textureSizeErrorThreshold,
            MaterialCountWarningThreshold = _materialCountWarningThreshold,
            ShaderVariantWarningThreshold = _shaderVariantWarningThreshold,
            WarningColor = _warningColor,
            ErrorColor = _errorColor,
            GoodColor = _goodColor
        };

        string json = JsonUtility.ToJson(settings, true);
        System.IO.File.WriteAllText(path, json);

        EditorUtility.DisplayDialog("Export Complete", "Settings exported successfully to:\n" + path, "OK");
    }

    private void ResetSettings()
    {
        _warningColor = new Color(1f, 0.7f, 0.3f);
        _errorColor = new Color(1f, 0.3f, 0.3f);
        _goodColor = new Color(0.3f, 1f, 0.5f);
        _textureSizeWarningThreshold = 1024;
        _textureSizeErrorThreshold = 2048;
        _materialCountWarningThreshold = 50;
        _shaderVariantWarningThreshold = 100;
        _vertexCountWarningThreshold = 10000;
        _vertexCountErrorThreshold = 50000;
        _triangleCountWarningThreshold = 15000;
        _triangleCountErrorThreshold = 65000;
        _autoRefresh = true;

        InitializeStyles();

        EditorUtility.DisplayDialog("Reset Complete", "Settings have been reset to defaults.", "OK");
    }

    // Context menu support methods
    public void AnalyzeSelectedGameObjects()
    {
        _currentTab = AnalysisTab.Materials;
        _filteredGameObjects = new List<GameObject>(Selection.gameObjects);
        _filterBySelection = true;
        _materialSearchFilter = "";
        PerformAnalysis();
    }

    public void FocusOnMaterial(Material material)
    {
        _currentTab = AnalysisTab.Materials;
        _focusedMaterial = material;
        _filteredMaterials = new List<Material> { material };
        _filterBySelection = true;
        _materialSearchFilter = "";
        PerformAnalysis();
    }

    public void AnalyzeRenderer(Renderer renderer)
    {
        if (renderer == null) return;

        _currentTab = AnalysisTab.Materials;
        _filteredMaterials = new List<Material>(renderer.sharedMaterials);
        _filterBySelection = true;
        _materialSearchFilter = "";
        PerformAnalysis();
    }

    public void ClearFilters()
    {
        _filterBySelection = false;
        _filteredGameObjects.Clear();
        _filteredMaterials.Clear();
        _focusedMaterial = null;
        _materialSearchFilter = "";
        PerformAnalysis();
    }
}

// Data structures for analysis
public class MaterialInfo
{
    public string Name;
    public Material Material;
    public int UsageCount;
    public string ShaderName;
    public List<string> Keywords;
    public string Path;
    public List<TextureInfo> Textures;
    public int TextureCount;
    public bool ShowTextures;
    public bool ShowKeywords;
}

public class ShaderInfo
{
    public string Name;
    public Shader Shader;
    public int UsageCount;
    public int RenderQueue;
    public string Path;
    public List<MaterialInfo> Materials;
    public int MaterialCount;
    public int PassCount;
    public bool SupportsInstancing;
    public List<string> Keywords;
    public bool ShowMaterials;
    public bool ShowKeywords;
}

public class TextureInfo
{
    public string TextureName;
    public Texture Texture;
    public int UsageCount;
    public string Path;
    public int Width;
    public int Height;
    public string Format;
    public int MipMapCount;
    public long MemorySize;
    public List<MaterialInfo> Materials;
    public List<string> Issues;
    public bool ShowMaterials;
}

public class AnalysisOverview
{
    public int MaterialCount;
    public int UniqueMaterialCount;
    public int ShaderCount;
    public int ShaderKeywordCount;
    public int TextureCount;
    public long TotalTextureMemory;
    public int OversizedTextureCount;
    public int NonPowerOfTwoTextureCount;
    public int UncompressedTextureCount;
    public int DuplicateMaterialCount;
    public int EstimatedBatches;
    public int MaterialSwitchesCount;

    // Mesh stats
    public int MeshCount;
    public int UniqueMeshCount;
    public int TotalVertexCount;
    public int TotalTriangleCount;
    public float TotalMeshMemory; // In KB
    public int HighPolyMeshCount;
    public int NonReadableMeshCount;
    public int MissingNormalsMeshCount;
    public int MissingTangentsMeshCount;
    public int MissingUVMeshCount;
    public int OptimizableMeshCount;
}

[System.Serializable]
public class MaterialAnalyzerSettings
{
    public bool AutoRefresh = true;
    public bool ShowSceneMaterialsOnly = true;
    public bool ShowUsedAssetsOnly = true;
    public bool ShowAdvancedInfo = false;
    public int TextureSizeWarningThreshold = 1024;
    public int TextureSizeErrorThreshold = 2048;
    public int MaterialCountWarningThreshold = 50;
    public int ShaderVariantWarningThreshold = 100;
    public int VertexCountWarningThreshold = 10000;
    public int VertexCountErrorThreshold = 50000;
    public int TriangleCountWarningThreshold = 15000;
    public int TriangleCountErrorThreshold = 65000;
    public Color WarningColor = new Color(1f, 0.7f, 0.3f);
    public Color ErrorColor = new Color(1f, 0.3f, 0.3f);
    public Color GoodColor = new Color(0.3f, 1f, 0.5f);
}

public class MeshInfo
{
    public string Name;
    public Mesh Mesh;
    public int VertexCount;
    public int TriangleCount;
    public int SubMeshCount;
    public float MemorySize; // In KB
    public bool IsReadable;
    public bool HasNormals;
    public bool HasTangents;
    public bool HasUV;
    public bool HasUV2;
    public bool HasBlendShapes;
    public bool HasBones;
    public int BonesCount;
    public int BlendShapeCount;
    public GameObject[] InstancesInScene;
    public int InstanceCount;
    public bool IsOptimized;
    public List<string> Issues;
    public string Path;
    public List<MaterialInfo> Materials;
    public bool ShowMaterials;
    public bool IsCombined;
    public bool ShowAdvancedInfo;
}
