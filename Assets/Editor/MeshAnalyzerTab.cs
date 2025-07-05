using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

public partial class MaterialAnalyzerWindow
{
    private void DrawMeshesTab()
    {
        EditorGUILayout.BeginVertical();

        // Header and search
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Meshes Analysis", _headerStyle);

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
        _meshSearchFilter = EditorGUILayout.TextField(_meshSearchFilter, _searchBoxStyle);

        if (GUILayout.Button("Clear", _buttonStyle, GUILayout.Width(50)))
        {
            _meshSearchFilter = "";
        }

        _showSceneMaterialsOnly = GUILayout.Toggle(_showSceneMaterialsOnly, "Scene Only", _buttonStyle, GUILayout.Width(85));
        _groupByVertexCount = GUILayout.Toggle(_groupByVertexCount, "Group by Size", _buttonStyle, GUILayout.Width(110));

        EditorGUILayout.EndHorizontal();

        // Sorting options
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Sort by:", GUILayout.Width(50));

        if (GUILayout.Toggle(_meshSortOption == SortOption.Name, "Name", _buttonStyle, GUILayout.Width(60)))
            _meshSortOption = SortOption.Name;

        if (GUILayout.Toggle(_meshSortOption == SortOption.Size, "Size", _buttonStyle, GUILayout.Width(60)))
            _meshSortOption = SortOption.Size;

        if (GUILayout.Toggle(_meshSortOption == SortOption.UsageCount, "Usage", _buttonStyle, GUILayout.Width(60)))
            _meshSortOption = SortOption.UsageCount;

        GUILayout.FlexibleSpace();

        EditorGUILayout.LabelField(_meshSortAscending ? "↑" : "↓", GUILayout.Width(15));
        if (GUILayout.Button("Reverse", _buttonStyle, GUILayout.Width(65)))
            _meshSortAscending = !_meshSortAscending;

        if (GUILayout.Button("Select All", _buttonStyle, GUILayout.Width(80)))
            SelectAllFilteredMeshes();

        EditorGUILayout.EndHorizontal();

        // Action buttons
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Optimize Selected", _buttonStyle))
        {
            OptimizeSelectedMeshes();
        }

        if (GUILayout.Button("Combine Selected", _buttonStyle))
        {
            CombineSelectedMeshes();
        }

        if (GUILayout.Button("Find High-Poly", _buttonStyle))
        {
            FindHighPolyMeshes();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Meshes list
        _meshScrollPosition = EditorGUILayout.BeginScrollView(_meshScrollPosition);

        // Get filtered & sorted meshes
        List<MeshInfo> filteredMeshes = FilterAndSortMeshes();

        if (filteredMeshes.Count == 0)
        {
            EditorGUILayout.HelpBox("No meshes found with the current filter settings.", MessageType.Info);
        }
        else
        {
            // Group by vertex count if needed
            if (_groupByVertexCount)
            {
                // Define vertex count categories
                string[] sizeCategories = new string[] 
                {
                    "Very High-Poly (50k+ vertices)",
                    "High-Poly (10k - 50k vertices)",
                    "Medium-Poly (1k - 10k vertices)",
                    "Low-Poly (< 1k vertices)"
                };

                Dictionary<string, List<MeshInfo>> meshesBySize = new Dictionary<string, List<MeshInfo>>();

                foreach (var category in sizeCategories)
                {
                    meshesBySize[category] = new List<MeshInfo>();
                }

                // Sort meshes into categories
                foreach (var mesh in filteredMeshes)
                {
                    if (mesh.VertexCount >= 50000) // 50k+
                        meshesBySize["Very High-Poly (50k+ vertices)"].Add(mesh);
                    else if (mesh.VertexCount >= 10000) // 10k - 50k
                        meshesBySize["High-Poly (10k - 50k vertices)"].Add(mesh);
                    else if (mesh.VertexCount >= 1000) // 1k - 10k
                        meshesBySize["Medium-Poly (1k - 10k vertices)"].Add(mesh);
                    else // < 1k
                        meshesBySize["Low-Poly (< 1k vertices)"].Add(mesh);
                }

                // Display meshes by category
                foreach (var category in sizeCategories)
                {
                    var meshes = meshesBySize[category];

                    if (meshes.Count > 0)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                        // Category header
                        EditorGUILayout.BeginHorizontal();
                        bool expanded = EditorGUILayout.Foldout(true, "", true);
                        EditorGUILayout.LabelField(category + " ("+meshes.Count+" meshes)", _boldLabelStyle);
                        EditorGUILayout.EndHorizontal();

                        if (expanded)
                        {
                            foreach (var mesh in meshes)
                            {
                                DrawMeshItem(mesh);
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
                foreach (var mesh in filteredMeshes)
                {
                    DrawMeshItem(mesh);
                }
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawMeshItem(MeshInfo mesh)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // Mesh header
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(mesh.Name, _boldLabelStyle);

        GUILayout.FlexibleSpace();

        // Vertex count label with styling based on count
        GUIStyle vertexStyle = mesh.VertexCount > _vertexCountWarningThreshold ? 
                              (mesh.VertexCount > _vertexCountErrorThreshold ? _errorLabelStyle : _warningLabelStyle) : 
                              EditorStyles.label;
        EditorGUILayout.LabelField(mesh.VertexCount + " verts", vertexStyle, GUILayout.Width(80));

        // Usage count label
        GUIStyle usageStyle = mesh.InstanceCount > 5 ? _warningLabelStyle : EditorStyles.label;
        EditorGUILayout.LabelField("Used: " + mesh.InstanceCount, usageStyle, GUILayout.Width(60));

        // Action buttons
        if (GUILayout.Button(_pingIcon.image, _iconButtonStyle))
        {
            PingMesh(mesh);
        }

        if (GUILayout.Button(_selectIcon.image, _iconButtonStyle))
        {
            SelectMesh(mesh);
        }

        EditorGUILayout.EndHorizontal();

        // Mesh details
        EditorGUILayout.BeginHorizontal();

        // Triangle count
        GUIStyle triangleStyle = mesh.TriangleCount > _triangleCountWarningThreshold ? 
                                (mesh.TriangleCount > _triangleCountErrorThreshold ? _errorLabelStyle : _warningLabelStyle) : 
                                EditorStyles.label;
        EditorGUILayout.LabelField(mesh.TriangleCount + " triangles", triangleStyle);

        // Memory usage
        GUIStyle memoryStyle = mesh.MemorySize > 1000 ? _warningLabelStyle : EditorStyles.label;
        EditorGUILayout.LabelField(mesh.MemorySize.ToString("F1") + " KB", memoryStyle);

        // Sub-meshes
        if (mesh.SubMeshCount > 1)
        {
            EditorGUILayout.LabelField("Sub-meshes: " + mesh.SubMeshCount);
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        // Issues indicator
        if (mesh.Issues.Count > 0)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(_warningIcon.image, GUILayout.Width(20), GUILayout.Height(20));
            EditorGUILayout.LabelField(string.Join(", ", mesh.Issues), _warningLabelStyle);
            EditorGUILayout.EndHorizontal();
        }

        // Advanced info (collapsible)
        if (_showAdvancedInfo)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Readable: " + (mesh.IsReadable ? "Yes" : "No"));
            EditorGUILayout.LabelField("Has Normals: " + (mesh.HasNormals ? "Yes" : "No"));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Has Tangents: " + (mesh.HasTangents ? "Yes" : "No"));
            EditorGUILayout.LabelField("Has UVs: " + (mesh.HasUV ? "Yes" : "No"));
            EditorGUILayout.EndHorizontal();

            if (mesh.HasBlendShapes)
            {
                EditorGUILayout.LabelField("Blend Shapes: " + mesh.BlendShapeCount);
            }

            if (mesh.HasBones)
            {
                EditorGUILayout.LabelField("Bones: " + mesh.BonesCount);
            }

            EditorGUILayout.EndVertical();
        }

        // Materials using this mesh (collapsible)
        if (mesh.Materials.Count > 0)
        {
            EditorGUILayout.BeginHorizontal();
            mesh.ShowMaterials = EditorGUILayout.Foldout(mesh.ShowMaterials, "Materials ("+mesh.Materials.Count+")", true);
            EditorGUILayout.EndHorizontal();

            if (mesh.ShowMaterials)
            {
                EditorGUI.indentLevel++;
                foreach (var material in mesh.Materials)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(10);
                    GUILayout.Label(_materialIcon.image, GUILayout.Width(20), GUILayout.Height(20));
                    EditorGUILayout.LabelField(material.Name);
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button(_pingIcon.image, _iconButtonStyle))
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

    private void AnalyzeMeshes(bool forceFullScan = false)
    {
        if (!forceFullScan && _meshes.Count > 0)
            return; // Skip if already analyzed and not forcing a full scan

        _meshes.Clear();

        // Dictionary to track mesh usage
        Dictionary<Mesh, List<GameObject>> meshUsage = new Dictionary<Mesh, List<GameObject>>();
        Dictionary<Mesh, List<Material>> meshMaterials = new Dictionary<Mesh, List<Material>>();

        // Find all mesh renderers and skinned mesh renderers in the scene
        MeshFilter[] meshFilters = GameObject.FindObjectsOfType<MeshFilter>();
        SkinnedMeshRenderer[] skinnedMeshRenderers = GameObject.FindObjectsOfType<SkinnedMeshRenderer>();

        // Process mesh filters
        foreach (MeshFilter meshFilter in meshFilters)
        {
            if (meshFilter == null || meshFilter.sharedMesh == null)
                continue;

            Mesh mesh = meshFilter.sharedMesh;

            // Track mesh usage
            if (!meshUsage.ContainsKey(mesh))
            {
                meshUsage[mesh] = new List<GameObject>();
                meshMaterials[mesh] = new List<Material>();
            }

            meshUsage[mesh].Add(meshFilter.gameObject);

            // Get materials from the renderer
            MeshRenderer renderer = meshFilter.GetComponent<MeshRenderer>();
            if (renderer != null && renderer.sharedMaterials != null)
            {
                foreach (Material mat in renderer.sharedMaterials)
                {
                    if (mat != null && !meshMaterials[mesh].Contains(mat))
                        meshMaterials[mesh].Add(mat);
                }
            }
        }

        // Process skinned mesh renderers
        foreach (SkinnedMeshRenderer renderer in skinnedMeshRenderers)
        {
            if (renderer == null || renderer.sharedMesh == null)
                continue;

            Mesh mesh = renderer.sharedMesh;

            // Track mesh usage
            if (!meshUsage.ContainsKey(mesh))
            {
                meshUsage[mesh] = new List<GameObject>();
                meshMaterials[mesh] = new List<Material>();
            }

            meshUsage[mesh].Add(renderer.gameObject);

            // Get materials
            if (renderer.sharedMaterials != null)
            {
                foreach (Material mat in renderer.sharedMaterials)
                {
                    if (mat != null && !meshMaterials[mesh].Contains(mat))
                        meshMaterials[mesh].Add(mat);
                }
            }
        }

        // Create MeshInfo objects for each mesh
        foreach (var meshEntry in meshUsage)
        {
            Mesh mesh = meshEntry.Key;
            List<GameObject> usedBy = meshEntry.Value;

            MeshInfo meshInfo = new MeshInfo
            {
                Name = mesh.name,
                Mesh = mesh,
                VertexCount = mesh.vertexCount,
                TriangleCount = mesh.triangles != null ? mesh.triangles.Length / 3 : 0,
                SubMeshCount = mesh.subMeshCount,
                MemorySize = MaterialAnalyzerMeshTools.EstimateMeshMemoryUsage(mesh) / 1024f, // Convert to KB
                IsReadable = mesh.isReadable,
                HasNormals = mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Normal),
                HasTangents = mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Tangent),
                HasUV = mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0),
                HasUV2 = mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord1),
                HasBlendShapes = mesh.blendShapeCount > 0,
                BlendShapeCount = mesh.blendShapeCount,
                InstancesInScene = usedBy.ToArray(),
                InstanceCount = usedBy.Count,
                Path = AssetDatabase.GetAssetPath(mesh),
                Issues = new List<string>(),
                Materials = new List<MaterialInfo>(),
                ShowMaterials = false,
                ShowAdvancedInfo = false
            };

            // Check for bones/skinning
            meshInfo.HasBones = false;
            meshInfo.BonesCount = 0;
            foreach (var obj in usedBy)
            {
                SkinnedMeshRenderer smr = obj.GetComponent<SkinnedMeshRenderer>();
                if (smr != null && smr.bones != null && smr.bones.Length > 0)
                {
                    meshInfo.HasBones = true;
                    meshInfo.BonesCount = Mathf.Max(meshInfo.BonesCount, smr.bones.Length);
                }
            }

            // Link to material objects we already have
            foreach (Material material in meshMaterials[mesh])
            {
                foreach (var materialInfo in _materials)
                {
                    if (materialInfo.Material == material)
                    {
                        meshInfo.Materials.Add(materialInfo);
                        break;
                    }
                }
            }

            // Check for issues
            if (meshInfo.VertexCount > _vertexCountErrorThreshold)
                meshInfo.Issues.Add("Very high vertex count");
            else if (meshInfo.VertexCount > _vertexCountWarningThreshold)
                meshInfo.Issues.Add("High vertex count");

            if (meshInfo.TriangleCount > _triangleCountErrorThreshold)
                meshInfo.Issues.Add("Very high triangle count");
            else if (meshInfo.TriangleCount > _triangleCountWarningThreshold)
                meshInfo.Issues.Add("High triangle count");

            if (!meshInfo.HasNormals)
                meshInfo.Issues.Add("Missing normals");

            if (!meshInfo.HasTangents && meshInfo.HasUV)
                meshInfo.Issues.Add("Missing tangents");

            if (!meshInfo.HasUV)
                meshInfo.Issues.Add("Missing UVs");

            if (!meshInfo.IsReadable)
                meshInfo.Issues.Add("Not readable (can't be optimized)");

            if (mesh.name.Contains("Combined") || mesh.name.Contains("Merged"))
                meshInfo.IsCombined = true;

            // Check if mesh needs optimization
            meshInfo.IsOptimized = !MaterialAnalyzerMeshTools.NeedsMeshOptimization(mesh);
            if (!meshInfo.IsOptimized && meshInfo.IsReadable)
                meshInfo.Issues.Add("Not optimized");

            _meshes.Add(meshInfo);
        }

        // Also scan meshes from project
        if (forceFullScan)
        {
            string[] meshGuids = AssetDatabase.FindAssets("t:Mesh");

            foreach (string guid in meshGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);

                // Skip if we already processed this mesh
                if (mesh == null || meshUsage.ContainsKey(mesh))
                    continue;

                MeshInfo meshInfo = new MeshInfo
                {
                    Name = mesh.name,
                    Mesh = mesh,
                    VertexCount = mesh.vertexCount,
                    TriangleCount = mesh.triangles != null ? mesh.triangles.Length / 3 : 0,
                    SubMeshCount = mesh.subMeshCount,
                    MemorySize = MaterialAnalyzerMeshTools.EstimateMeshMemoryUsage(mesh) / 1024f, // Convert to KB
                    IsReadable = mesh.isReadable,
                    HasNormals = mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Normal),
                    HasTangents = mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Tangent),
                    HasUV = mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0),
                    HasUV2 = mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord1),
                    HasBlendShapes = mesh.blendShapeCount > 0,
                    BlendShapeCount = mesh.blendShapeCount,
                    InstancesInScene = new GameObject[0],
                    InstanceCount = 0,
                    Path = path,
                    Issues = new List<string>(),
                    Materials = new List<MaterialInfo>(),
                    ShowMaterials = false,
                    ShowAdvancedInfo = false
                };

                // Check for issues
                if (meshInfo.VertexCount > _vertexCountErrorThreshold)
                    meshInfo.Issues.Add("Very high vertex count");
                else if (meshInfo.VertexCount > _vertexCountWarningThreshold)
                    meshInfo.Issues.Add("High vertex count");

                if (meshInfo.TriangleCount > _triangleCountErrorThreshold)
                    meshInfo.Issues.Add("Very high triangle count");
                else if (meshInfo.TriangleCount > _triangleCountWarningThreshold)
                    meshInfo.Issues.Add("High triangle count");

                if (!meshInfo.HasNormals)
                    meshInfo.Issues.Add("Missing normals");

                if (!meshInfo.HasTangents && meshInfo.HasUV)
                    meshInfo.Issues.Add("Missing tangents");

                if (!meshInfo.HasUV)
                    meshInfo.Issues.Add("Missing UVs");

                if (!meshInfo.IsReadable)
                    meshInfo.Issues.Add("Not readable (can't be optimized)");

                if (mesh.name.Contains("Combined") || mesh.name.Contains("Merged"))
                    meshInfo.IsCombined = true;

                // Check if mesh needs optimization
                meshInfo.IsOptimized = !MaterialAnalyzerMeshTools.NeedsMeshOptimization(mesh);
                if (!meshInfo.IsOptimized && meshInfo.IsReadable)
                    meshInfo.Issues.Add("Not optimized");

                _meshes.Add(meshInfo);
            }
        }
    }

    private void UpdateOverviewWithMeshData()
    {
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

    private List<MeshInfo> FilterAndSortMeshes()
    {
        // Apply filters
        var filtered = _meshes;

        if (_showSceneMaterialsOnly)
        {
            filtered = filtered.Where(m => m.InstanceCount > 0).ToList();
        }

        if (!string.IsNullOrEmpty(_meshSearchFilter))
        {
            string searchLower = _meshSearchFilter.ToLowerInvariant();
            filtered = filtered.Where(m => 
                m.Name.ToLowerInvariant().Contains(searchLower)
            ).ToList();
        }

        // Apply sorting
        switch (_meshSortOption)
        {
            case SortOption.Name:
                filtered = _meshSortAscending ? 
                    filtered.OrderBy(m => m.Name).ToList() : 
                    filtered.OrderByDescending(m => m.Name).ToList();
                break;
            case SortOption.Size: // Sort by vertex count
                filtered = _meshSortAscending ? 
                    filtered.OrderBy(m => m.VertexCount).ToList() : 
                    filtered.OrderByDescending(m => m.VertexCount).ToList();
                break;
            case SortOption.UsageCount:
                filtered = _meshSortAscending ? 
                    filtered.OrderBy(m => m.InstanceCount).ToList() : 
                    filtered.OrderByDescending(m => m.InstanceCount).ToList();
                break;
        }

        return filtered;
    }

    private void PingMesh(MeshInfo mesh)
    {
        if (mesh.Mesh != null)
        {
            EditorGUIUtility.PingObject(mesh.Mesh);
        }
    }

    private void SelectMesh(MeshInfo mesh)
    {
        if (mesh.Mesh != null)
        {
            Selection.activeObject = mesh.Mesh;
        }
    }

    private void SelectAllFilteredMeshes()
    {
        List<UnityEngine.Object> filteredMeshes = new List<UnityEngine.Object>();
        List<MeshInfo> meshInfos = FilterAndSortMeshes();

        foreach (var mesh in meshInfos)
        {
            if (mesh.Mesh != null)
            {
                filteredMeshes.Add(mesh.Mesh);
            }
        }

        if (filteredMeshes.Count > 0)
        {
            Selection.objects = filteredMeshes.ToArray();
        }
    }

    private void OptimizeSelectedMeshes()
    {
        List<MeshInfo> selectedMeshes = new List<MeshInfo>();
        List<MeshInfo> filteredMeshes = FilterAndSortMeshes();

        // If we have a specific selection, use that
        if (Selection.objects.Length > 0)
        {
            foreach (var selected in Selection.objects)
            {
                if (selected is Mesh mesh)
                {
                    foreach (var meshInfo in filteredMeshes)
                    {
                        if (meshInfo.Mesh == mesh)
                        {
                            selectedMeshes.Add(meshInfo);
                            break;
                        }
                    }
                }
            }
        }
        else
        {
            // If no selection, use all filtered meshes
            selectedMeshes = filteredMeshes;
        }

        if (selectedMeshes.Count == 0)
        {
            EditorUtility.DisplayDialog("Optimize Meshes", "No meshes selected to optimize.", "OK");
            return;
        }

        bool proceed = EditorUtility.DisplayDialog("Optimize Meshes", 
            "This will optimize " + selectedMeshes.Count + " mesh(es) by:\n\n" +
            "- Recalculating normals (if needed)\n" +
            "- Recalculating tangents (if needed)\n" +
            "- Optimizing triangle order for better GPU performance\n\n" +
            "Note: Only meshes that are marked as Read/Write enabled can be optimized.\n\n" +
            "Proceed?", 
            "Optimize", "Cancel");

        if (!proceed) return;

        int optimizedCount = 0;

        foreach (var meshInfo in selectedMeshes)
        {
            if (!meshInfo.IsReadable)
            {
                Debug.LogWarning("Cannot optimize mesh " + meshInfo.Name + " because it is not marked as Read/Write enabled.");
                continue;
            }

            Mesh mesh = meshInfo.Mesh;

            // Record for undo
            Undo.RecordObject(mesh, "Optimize Mesh");

            bool modified = false;

            // Recalculate normals if missing
            if (!meshInfo.HasNormals)
            {
                mesh.RecalculateNormals();
                modified = true;
            }

            // Recalculate tangents if missing but has UVs and normals
            if (!meshInfo.HasTangents && meshInfo.HasUV && meshInfo.HasNormals)
            {
                mesh.RecalculateTangents();
                modified = true;
            }

            // Optimize mesh
            mesh.Optimize();
            modified = true;

            if (modified)
            {
                optimizedCount++;
                meshInfo.HasNormals = mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Normal);
                meshInfo.HasTangents = mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Tangent);
                meshInfo.IsOptimized = true;
                meshInfo.Issues.Remove("Missing normals");
                meshInfo.Issues.Remove("Missing tangents");
                meshInfo.Issues.Remove("Not optimized");
            }
        }

        if (optimizedCount > 0)
        {
            EditorUtility.DisplayDialog("Optimization Complete", 
                "Successfully optimized " + optimizedCount + " mesh(es).", "OK");

            // Refresh the view
            PerformAnalysis();
        }
        else
        {
            EditorUtility.DisplayDialog("Optimization Complete", 
                "No meshes could be optimized. Make sure they are marked as Read/Write enabled in their import settings.", "OK");
        }
    }

    private void CombineSelectedMeshes()
    {
        // Implement selected mesh combination
        List<Mesh> selectedMeshes = new List<Mesh>();

        foreach (Object selected in Selection.objects)
        {
            if (selected is Mesh mesh)
            {
                selectedMeshes.Add(mesh);
            }
        }

        if (selectedMeshes.Count < 2)
        {
            EditorUtility.DisplayDialog("Combine Meshes", "You need to select at least 2 meshes to combine.", "OK");
            return;
        }

        bool proceed = EditorUtility.DisplayDialog("Combine Meshes", 
            "This will create a new combined mesh from the " + selectedMeshes.Count + " selected meshes.\n\n" +
            "Note: This is a simple combination that works best for meshes that will use the same material. " +
            "For more complex mesh combining needs, consider using a dedicated asset from the Asset Store.\n\n" +
            "Proceed?", 
            "Combine", "Cancel");

        if (!proceed) return;

        // Create combine instances
        CombineInstance[] combine = new CombineInstance[selectedMeshes.Count];

        for (int i = 0; i < selectedMeshes.Count; i++)
        {
            combine[i].mesh = selectedMeshes[i];
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

        // Refresh the view
        PerformAnalysis();
    }

    private void FindHighPolyMeshes()
    {
        List<Object> highPolyMeshes = new List<Object>();

        foreach (var meshInfo in _meshes)
        {
            if (meshInfo.VertexCount > _vertexCountWarningThreshold)
            {
                highPolyMeshes.Add(meshInfo.Mesh);
            }
        }

        if (highPolyMeshes.Count > 0)
        {
            Selection.objects = highPolyMeshes.ToArray();
            EditorUtility.DisplayDialog("High-Poly Meshes", 
                "Found " + highPolyMeshes.Count + " high-poly meshes with more than " + 
                _vertexCountWarningThreshold + " vertices.\n\nThese are now selected.", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("High-Poly Meshes", 
                "No high-poly meshes found with more than " + _vertexCountWarningThreshold + " vertices.", "OK");
        }
    }
}
