using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace PCG
{
    /// <summary>
    /// Handles visualization of placement previews in the scene view.
    /// </summary>
    public class PCGPreviewGizmos
    {
        private List<PCGPlacementEngine.PlacementData> previewData = new List<PCGPlacementEngine.PlacementData>();
        private PCGSettings settings;
        private bool showGizmos = true;
        private bool showMeshPreviews = false;
        private Material previewMaterial;
        private Dictionary<GameObject, Mesh> prefabMeshCache = new Dictionary<GameObject, Mesh>();
        
        // Gizmo appearance settings
        private Color gizmoColor = new Color(0.2f, 0.8f, 0.2f, 0.8f);
        private Color selectedGizmoColor = new Color(0.8f, 0.2f, 0.2f, 0.8f);
        private float gizmoSize = 0.25f;
        private float selectedGizmoSize = 0.35f;
        
        // Selection
        private int selectedPreviewIndex = -1;
        
        /// <summary>
        /// Initialize the preview gizmos with settings
        /// </summary>
        public PCGPreviewGizmos(PCGSettings settings)
        {
            this.settings = settings;
            CreatePreviewMaterial();
        }
        
        /// <summary>
        /// Create a material for mesh previews
        /// </summary>
        private void CreatePreviewMaterial()
        {
            if (previewMaterial == null)
            {
                // Create a simple transparent material for previews
                Shader shader = Shader.Find("Standard");
                if (shader != null)
                {
                    previewMaterial = new Material(shader);
                    previewMaterial.SetFloat("_Mode", 3); // Transparent mode
                    previewMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    previewMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    previewMaterial.SetInt("_ZWrite", 0);
                    previewMaterial.DisableKeyword("_ALPHATEST_ON");
                    previewMaterial.EnableKeyword("_ALPHABLEND_ON");
                    previewMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    previewMaterial.renderQueue = 3000;
                    previewMaterial.color = new Color(0.2f, 0.8f, 0.2f, 0.5f);
                }
            }
        }
        
        /// <summary>
        /// Set the preview data to visualize
        /// </summary>
        public void SetPreviewData(List<PCGPlacementEngine.PlacementData> data)
        {
            previewData = data ?? new List<PCGPlacementEngine.PlacementData>();
            selectedPreviewIndex = -1;
        }
        
        /// <summary>
        /// Clear all preview data
        /// </summary>
        public void Clear()
        {
            previewData.Clear();
            selectedPreviewIndex = -1;
        }
        
        /// <summary>
        /// Toggle visibility of gizmos
        /// </summary>
        public void ToggleGizmos(bool show)
        {
            showGizmos = show;
            SceneView.RepaintAll();
        }
        
        /// <summary>
        /// Toggle visibility of mesh previews
        /// </summary>
        public void ToggleMeshPreviews(bool show)
        {
            showMeshPreviews = show;
            SceneView.RepaintAll();
        }
        
        /// <summary>
        /// Draw gizmos in the scene view
        /// </summary>
        public void OnSceneGUI(SceneView sceneView)
        {
            if (!showGizmos || previewData == null)
                return;
                
            // Cache meshes for preview
            if (showMeshPreviews)
            {
                foreach (var data in previewData)
                {
                    if (data.prefab != null && !prefabMeshCache.ContainsKey(data.prefab))
                    {
                        Mesh mesh = null;
                        MeshFilter meshFilter = data.prefab.GetComponentInChildren<MeshFilter>();
                        if (meshFilter != null && meshFilter.sharedMesh != null)
                        {
                            mesh = meshFilter.sharedMesh;
                        }
                        prefabMeshCache[data.prefab] = mesh;
                    }
                }
            }
            
            // Draw each preview
            for (int i = 0; i < previewData.Count; i++)
            {
                var data = previewData[i];
                bool isSelected = (i == selectedPreviewIndex);
                
                // Draw position gizmo
                Handles.color = isSelected ? selectedGizmoColor : gizmoColor;
                float size = isSelected ? selectedGizmoSize : gizmoSize;
                
                Handles.SphereHandleCap(
                    i,
                    data.position,
                    Quaternion.identity,
                    size,
                    EventType.Repaint
                );
                
                // Draw normal direction
                Handles.DrawLine(
                    data.position,
                    data.position + data.rotation * Vector3.up * size * 2
                );
                
                // Draw mesh preview if enabled
                if (showMeshPreviews && data.prefab != null && prefabMeshCache.TryGetValue(data.prefab, out Mesh mesh) && mesh != null)
                {
                    if (previewMaterial != null)
                    {
                        // Set color based on selection state
                        previewMaterial.color = isSelected ? 
                            new Color(0.8f, 0.2f, 0.2f, 0.5f) : 
                            new Color(0.2f, 0.8f, 0.2f, 0.5f);
                            
                        // Draw the mesh
                        Matrix4x4 matrix = Matrix4x4.TRS(
                            data.position,
                            data.rotation,
                            data.scale
                        );
                        
                        Graphics.DrawMesh(
                            mesh,
                            matrix,
                            previewMaterial,
                            0,
                            sceneView.camera,
                            0
                        );
                    }
                }
                
                // Handle selection
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    float handleSize = HandleUtility.GetHandleSize(data.position) * size;
                    if (Vector2.Distance(
                            HandleUtility.WorldToGUIPoint(data.position),
                            Event.current.mousePosition) < handleSize * 10)
                    {
                        selectedPreviewIndex = i;
                        Event.current.Use();
                        sceneView.Repaint();
                    }
                }
            }
            
            // Draw filter visualizations
            DrawFilterVisualizations(sceneView);
        }
        
        /// <summary>
        /// Draw visualizations for active filters
        /// </summary>
        private void DrawFilterVisualizations(SceneView sceneView)
        {
            // Draw spline influence areas if spline filter is enabled
            if (settings.splineFilter.enabled && settings.splineFilter.splineObjects.Count > 0)
            {
                Handles.color = new Color(0.2f, 0.6f, 1.0f, 0.3f);
                
                foreach (var splineObj in settings.splineFilter.splineObjects)
                {
                    if (splineObj == null)
                        continue;
                        
                    var splineContainer = splineObj.GetComponent<UnityEngine.Splines.SplineContainer>();
                    if (splineContainer == null)
                        continue;
                        
                    // Draw influence area around each knot
                    foreach (var knot in splineContainer.Spline.Knots)
                    {
                        Vector3 position = splineObj.transform.TransformPoint(knot.Position);
                        Handles.DrawWireDisc(
                            position,
                            Vector3.up,
                            settings.splineFilter.distance
                        );
                    }
                }
            }
            
            // Draw altitude filter visualization if enabled
            if (settings.altitudeFilter.enabled)
            {
                // Only draw if we have a reasonable range
                if (settings.altitudeFilter.minHeight > float.MinValue && 
                    settings.altitudeFilter.maxHeight < float.MaxValue)
                {
                    Handles.color = new Color(1.0f, 0.8f, 0.2f, 0.3f);
                    
                    // Calculate bounds of the scene to draw planes at min/max heights
                    Bounds sceneBounds = new Bounds();
                    bool initialized = false;
                    
                    foreach (var obj in settings.targetMeshes)
                    {
                        if (obj == null)
                            continue;
                            
                        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
                        foreach (var renderer in renderers)
                        {
                            if (!initialized)
                            {
                                sceneBounds = renderer.bounds;
                                initialized = true;
                            }
                            else
                            {
                                sceneBounds.Encapsulate(renderer.bounds);
                            }
                        }
                    }
                    
                    if (initialized)
                    {
                        // Expand bounds a bit for visibility
                        sceneBounds.Expand(new Vector3(5, 0, 5));
                        
                        // Draw min height plane
                        if (settings.altitudeFilter.minHeight > float.MinValue)
                        {
                            Vector3 center = new Vector3(
                                sceneBounds.center.x,
                                settings.altitudeFilter.minHeight,
                                sceneBounds.center.z
                            );
                            
                            Vector3 size = new Vector3(
                                sceneBounds.size.x,
                                0.01f,
                                sceneBounds.size.z
                            );
                            
                            Handles.DrawWireCube(center, size);
                        }
                        
                        // Draw max height plane
                        if (settings.altitudeFilter.maxHeight < float.MaxValue)
                        {
                            Vector3 center = new Vector3(
                                sceneBounds.center.x,
                                settings.altitudeFilter.maxHeight,
                                sceneBounds.center.z
                            );
                            
                            Vector3 size = new Vector3(
                                sceneBounds.size.x,
                                0.01f,
                                sceneBounds.size.z
                            );
                            
                            Handles.DrawWireCube(center, size);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Get the currently selected preview index
        /// </summary>
        public int GetSelectedPreviewIndex()
        {
            return selectedPreviewIndex;
        }
        
        /// <summary>
        /// Set the selected preview index
        /// </summary>
        public void SetSelectedPreviewIndex(int index)
        {
            if (index >= -1 && index < previewData.Count)
            {
                selectedPreviewIndex = index;
                SceneView.RepaintAll();
            }
        }
        
        /// <summary>
        /// Get the number of preview items
        /// </summary>
        public int GetPreviewCount()
        {
            return previewData?.Count ?? 0;
        }
    }
}