using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditorInternal;
using UnityEngine.Splines;

namespace PCG
{
    /// <summary>
    /// Main editor window for the PCG tool.
    /// Provides a user-friendly interface for all PCG functionality.
    /// </summary>
    public class PCGEditorWindow : EditorWindow
    {
        // Core components
        private PCGSettings settings;
        private PCGPlacementEngine placementEngine;
        private PCGBakeManager bakeManager;
        private PCGPreviewGizmos previewGizmos;

        // UI state
        private Vector2 scrollPosition;
        private bool showTargetMeshSettings = true;
        private bool showPrefabSettings = true;
        private bool showPlacementSettings = true;
        private bool showFilterSettings = true;
        private bool showSplineSettings = true;
        private bool showMaskSettings = false;
        private bool showVertexColorSettings = false;
        private bool showAdvancedSettings = false;
        private bool showPreviewSettings = true;
        private bool showActionButtons = true;

        // Preview state
        private List<PCGPlacementEngine.PlacementData> previewData = new List<PCGPlacementEngine.PlacementData>();
        private bool showMeshPreviews = false;
        private bool autoUpdatePreview = true;

        // Prefab reordering
        private int draggedPrefabIndex = -1;
        private int draggedOverPrefabIndex = -1;

        // Styles
        private GUIStyle headerStyle;
        private GUIStyle subHeaderStyle;
        private GUIStyle boxStyle;
        private GUIStyle buttonStyle;
        private GUIStyle dragHandleStyle;

        [MenuItem("Tools/PCG/PCG Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<PCGEditorWindow>("PCG Tool");
            window.minSize = new Vector2(350, 500);
            window.Show();
        }

        private void OnEnable()
        {
            // Initialize settings if needed
            if (settings == null)
            {
                settings = CreateInstance<PCGSettings>();

                // Add a default prefab setting
                settings.prefabs.Add(new PCGSettings.PrefabSettings());
            }

            // Initialize components
            placementEngine = new PCGPlacementEngine(settings);
            bakeManager = new PCGBakeManager(settings);
            previewGizmos = new PCGPreviewGizmos(settings);

            // Register for scene view callbacks
            SceneView.duringSceneGui += OnSceneGUI;

            // Initialize styles that don't depend on GUI
            InitializeEditorStyles();
        }

        private void OnDisable()
        {
            // Unregister from scene view callbacks
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void InitializeEditorStyles()
        {
            // Initialize styles that only depend on EditorStyles (not GUI)
            headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 14;
            headerStyle.margin = new RectOffset(0, 0, 10, 5);

            subHeaderStyle = new GUIStyle(EditorStyles.boldLabel);
            subHeaderStyle.margin = new RectOffset(0, 0, 5, 5);

            boxStyle = new GUIStyle(EditorStyles.helpBox);
            boxStyle.padding = new RectOffset(10, 10, 10, 10);
            boxStyle.margin = new RectOffset(0, 0, 5, 5);

            dragHandleStyle = new GUIStyle(EditorStyles.label);
            dragHandleStyle.alignment = TextAnchor.MiddleCenter;
            dragHandleStyle.fontStyle = FontStyle.Bold;

            // Note: buttonStyle is initialized in OnGUI because it depends on GUI.skin
        }

        private void OnGUI()
        {
            // Initialize GUI-dependent styles
            if (buttonStyle == null)
            {
                buttonStyle = new GUIStyle(GUI.skin.button);
                buttonStyle.padding = new RectOffset(10, 10, 5, 5);
            }

            if (settings == null)
            {
                settings = CreateInstance<PCGSettings>();
                placementEngine = new PCGPlacementEngine(settings);
                bakeManager = new PCGBakeManager(settings);
                previewGizmos = new PCGPreviewGizmos(settings);
            }

            EditorGUI.BeginChangeCheck();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            GUILayout.Label("PCG Tool", headerStyle);
            EditorGUILayout.Space();

            // Save/Load buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("New Setup", buttonStyle))
            {
                if (EditorUtility.DisplayDialog("New PCG Setup", "Create a new PCG setup? This will clear all current settings.", "Yes", "Cancel"))
                {
                    settings = CreateInstance<PCGSettings>();
                    settings.prefabs.Add(new PCGSettings.PrefabSettings());
                    placementEngine = new PCGPlacementEngine(settings);
                    bakeManager = new PCGBakeManager(settings);
                    previewGizmos = new PCGPreviewGizmos(settings);
                    ClearPreview();
                }
            }

            if (GUILayout.Button("Save Setup", buttonStyle))
            {
                string path = EditorUtility.SaveFilePanelInProject("Save PCG Setup", "PCGSetup", "asset", "Save PCG setup as asset");
                if (!string.IsNullOrEmpty(path))
                {
                    AssetDatabase.CreateAsset(settings, path);
                    AssetDatabase.SaveAssets();
                    EditorUtility.FocusProjectWindow();
                    Selection.activeObject = settings;
                    EditorGUIUtility.PingObject(settings);
                }
            }

            if (GUILayout.Button("Load Setup", buttonStyle))
            {
                string path = EditorUtility.OpenFilePanelWithFilters("Load PCG Setup", "Assets", new string[] { "PCG Setup", "asset" });
                if (!string.IsNullOrEmpty(path))
                {
                    path = "Assets" + path.Substring(Application.dataPath.Length);
                    PCGSettings loadedSettings = AssetDatabase.LoadAssetAtPath<PCGSettings>(path);
                    if (loadedSettings != null)
                    {
                        settings = loadedSettings;
                        placementEngine = new PCGPlacementEngine(settings);
                        bakeManager = new PCGBakeManager(settings);
                        previewGizmos = new PCGPreviewGizmos(settings);
                        ClearPreview();
                    }
                }
            }

            if (GUILayout.Button("Export JSON", buttonStyle))
            {
                string path = EditorUtility.SaveFilePanel("Export PCG Setup as JSON", "", "PCGSetup", "json");
                if (!string.IsNullOrEmpty(path))
                {
                    string json = settings.ToJson();
                    File.WriteAllText(path, json);
                }
            }

            if (GUILayout.Button("Import JSON", buttonStyle))
            {
                string path = EditorUtility.OpenFilePanel("Import PCG Setup from JSON", "", "json");
                if (!string.IsNullOrEmpty(path))
                {
                    string json = File.ReadAllText(path);
                    PCGSettings loadedSettings = PCGSettings.FromJson(json);
                    if (loadedSettings != null)
                    {
                        settings = loadedSettings;
                        placementEngine = new PCGPlacementEngine(settings);
                        bakeManager = new PCGBakeManager(settings);
                        previewGizmos = new PCGPreviewGizmos(settings);
                        ClearPreview();
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Target Mesh Settings
            showTargetMeshSettings = EditorGUILayout.Foldout(showTargetMeshSettings, "Target Mesh Settings", true);
            if (showTargetMeshSettings)
            {
                EditorGUILayout.BeginVertical(boxStyle);

                EditorGUILayout.HelpBox("Add one or more meshes to use as placement targets.", MessageType.Info);

                SerializedObject so = new SerializedObject(settings);
                SerializedProperty targetMeshesProp = so.FindProperty("targetMeshes");
                EditorGUILayout.PropertyField(targetMeshesProp, true);
                so.ApplyModifiedProperties();

                if (settings.targetMeshes.Count == 0)
                {
                    EditorGUILayout.HelpBox("At least one target mesh is required for placement.", MessageType.Warning);
                }

                EditorGUILayout.EndVertical();
            }

            // Prefab Settings
            showPrefabSettings = EditorGUILayout.Foldout(showPrefabSettings, "Prefab Settings", true);
            if (showPrefabSettings)
            {
                EditorGUILayout.BeginVertical(boxStyle);

                EditorGUILayout.HelpBox("Add prefabs to place on the target meshes. Adjust weights to control relative frequency.", MessageType.Info);

                settings.groupByPrefab = EditorGUILayout.Toggle("Group by Prefab Type", settings.groupByPrefab);

                // Prefab list
                for (int i = 0; i < settings.prefabs.Count; i++)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    // Drag handle and header
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("≡", dragHandleStyle, GUILayout.Width(20));
                    EditorGUILayout.LabelField("Prefab " + (i + 1), subHeaderStyle);

                    // Remove button
                    if (GUILayout.Button("X", GUILayout.Width(25)))
                    {
                        settings.prefabs.RemoveAt(i);
                        i--;
                        UpdatePreview();
                        continue;
                    }
                    EditorGUILayout.EndHorizontal();

                    // Prefab fields
                    var prefab = settings.prefabs[i];
                    prefab.prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", prefab.prefab, typeof(GameObject), false);
                    prefab.weight = EditorGUILayout.Slider("Weight", prefab.weight, 0, 100);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Scale Range", GUILayout.Width(100));
                    prefab.minScale = EditorGUILayout.FloatField(prefab.minScale, GUILayout.Width(50));
                    EditorGUILayout.LabelField("to", GUILayout.Width(20));
                    prefab.maxScale = EditorGUILayout.FloatField(prefab.maxScale, GUILayout.Width(50));
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.LabelField("Rotation Range (degrees)");

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Min", GUILayout.Width(30));
                    prefab.minRotation = EditorGUILayout.Vector3Field("", prefab.minRotation);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Max", GUILayout.Width(30));
                    prefab.maxRotation = EditorGUILayout.Vector3Field("", prefab.maxRotation);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.EndVertical();

                    // Handle drag and drop reordering
                    Rect prefabRect = GUILayoutUtility.GetLastRect();
                    Event evt = Event.current;

                    switch (evt.type)
                    {
                        case EventType.MouseDown:
                            if (prefabRect.Contains(evt.mousePosition))
                            {
                                draggedPrefabIndex = i;
                                evt.Use();
                            }
                            break;

                        case EventType.MouseDrag:
                            if (draggedPrefabIndex == i)
                            {
                                DragAndDrop.PrepareStartDrag();
                                DragAndDrop.SetGenericData("PrefabIndex", i);
                                DragAndDrop.StartDrag("Drag Prefab");
                                evt.Use();
                            }
                            break;

                        case EventType.DragUpdated:
                        case EventType.DragPerform:
                            if (prefabRect.Contains(evt.mousePosition) && DragAndDrop.GetGenericData("PrefabIndex") != null)
                            {
                                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                                draggedOverPrefabIndex = i;

                                if (evt.type == EventType.DragPerform)
                                {
                                    DragAndDrop.AcceptDrag();

                                    int draggedIndex = (int)DragAndDrop.GetGenericData("PrefabIndex");
                                    if (draggedIndex != i)
                                    {
                                        var temp = settings.prefabs[draggedIndex];
                                        settings.prefabs.RemoveAt(draggedIndex);
                                        settings.prefabs.Insert(i, temp);
                                        UpdatePreview();
                                    }

                                    draggedPrefabIndex = -1;
                                    draggedOverPrefabIndex = -1;
                                }

                                evt.Use();
                            }
                            break;

                        case EventType.DragExited:
                            draggedPrefabIndex = -1;
                            draggedOverPrefabIndex = -1;
                            evt.Use();
                            break;
                    }
                }

                // Add prefab button
                if (GUILayout.Button("Add Prefab"))
                {
                    settings.prefabs.Add(new PCGSettings.PrefabSettings());
                }

                EditorGUILayout.EndVertical();
            }

            // Placement Settings
            showPlacementSettings = EditorGUILayout.Foldout(showPlacementSettings, "Placement Settings", true);
            if (showPlacementSettings)
            {
                EditorGUILayout.BeginVertical(boxStyle);

                settings.instanceCount = EditorGUILayout.IntField("Instance Count", settings.instanceCount);
                settings.randomSeed = EditorGUILayout.IntField("Random Seed", settings.randomSeed);

                if (GUILayout.Button("Randomize Seed"))
                {
                    settings.randomSeed = Random.Range(0, 99999);
                    UpdatePreview();
                }

                settings.collisionRadius = EditorGUILayout.FloatField("Collision Radius", settings.collisionRadius);

                // Layer masks using layer numbers directly
                List<string> layerNames = new List<string>();
                List<int> layerNumbers = new List<int>();
                for(int i = 0; i < 32; i++) {
                    string layerName = LayerMask.LayerToName(i);
                    if(!string.IsNullOrEmpty(layerName)) {
                        layerNames.Add(layerName);
                        layerNumbers.Add(i);
                    }
                }

                int allowedIndex = EditorGUILayout.Popup("Allowed Layer", 
                    layerNumbers.IndexOf(Mathf.RoundToInt(Mathf.Log(settings.allowedLayerMask.value, 2))),
                    layerNames.ToArray());
                if(allowedIndex >= 0) {
                    settings.allowedLayerMask = 1 << layerNumbers[allowedIndex];
                }

                int blockingIndex = EditorGUILayout.Popup("Blocking Layer",
                    layerNumbers.IndexOf(Mathf.RoundToInt(Mathf.Log(settings.blockLayerMask.value, 2))),
                    layerNames.ToArray());
                if(blockingIndex >= 0) {
                    settings.blockLayerMask = 1 << layerNumbers[blockingIndex];
                }

                EditorGUILayout.EndVertical();
            }
            // Filter Settings
            showFilterSettings = EditorGUILayout.Foldout(showFilterSettings, "Filter Settings", true);
            if (showFilterSettings)
            {
                EditorGUILayout.BeginVertical(boxStyle);

                // Slope Filter
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                settings.slopeFilter.enabled = EditorGUILayout.ToggleLeft("Slope Filter", settings.slopeFilter.enabled);
                if (settings.slopeFilter.enabled)
                {
                    EditorGUI.indentLevel++;
                    settings.slopeFilter.name = EditorGUILayout.TextField("Name", settings.slopeFilter.name);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Angle Range (degrees)", GUILayout.Width(150));
                    settings.slopeFilter.minAngle = EditorGUILayout.FloatField(settings.slopeFilter.minAngle, GUILayout.Width(50));
                    EditorGUILayout.LabelField("to", GUILayout.Width(20));
                    settings.slopeFilter.maxAngle = EditorGUILayout.FloatField(settings.slopeFilter.maxAngle, GUILayout.Width(50));
                    EditorGUILayout.EndHorizontal();

                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndVertical();

                // Altitude Filter
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                settings.altitudeFilter.enabled = EditorGUILayout.ToggleLeft("Altitude Filter", settings.altitudeFilter.enabled);
                if (settings.altitudeFilter.enabled)
                {
                    EditorGUI.indentLevel++;
                    settings.altitudeFilter.name = EditorGUILayout.TextField("Name", settings.altitudeFilter.name);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Height Range", GUILayout.Width(150));
                    settings.altitudeFilter.minHeight = EditorGUILayout.FloatField(settings.altitudeFilter.minHeight, GUILayout.Width(50));
                    EditorGUILayout.LabelField("to", GUILayout.Width(20));
                    settings.altitudeFilter.maxHeight = EditorGUILayout.FloatField(settings.altitudeFilter.maxHeight, GUILayout.Width(50));
                    EditorGUILayout.EndHorizontal();

                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndVertical();

                // Proximity Filter
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                settings.proximityFilter.enabled = EditorGUILayout.ToggleLeft("Proximity Filter", settings.proximityFilter.enabled);
                if (settings.proximityFilter.enabled)
                {
                    EditorGUI.indentLevel++;
                    settings.proximityFilter.name = EditorGUILayout.TextField("Name", settings.proximityFilter.name);

                    settings.proximityFilter.minDistance = EditorGUILayout.FloatField("Minimum Distance", settings.proximityFilter.minDistance);
                    settings.proximityFilter.maxDistance = EditorGUILayout.FloatField("Maximum Distance (0 = no limit)", settings.proximityFilter.maxDistance);

                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndVertical();

                // Spline Filter
                showSplineSettings = EditorGUILayout.Foldout(showSplineSettings, "Spline Filter", true);
                if (showSplineSettings)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    settings.splineFilter.enabled = EditorGUILayout.ToggleLeft("Enable Spline Filter", settings.splineFilter.enabled);
                    if (settings.splineFilter.enabled)
                    {
                        EditorGUI.indentLevel++;
                        settings.splineFilter.name = EditorGUILayout.TextField("Name", settings.splineFilter.name);

                        SerializedObject so = new SerializedObject(settings);
                        SerializedProperty splineObjectsProp = so.FindProperty("splineFilter.splineObjects");
                        EditorGUILayout.PropertyField(splineObjectsProp, true);
                        so.ApplyModifiedProperties();

                        settings.splineFilter.distance = EditorGUILayout.FloatField("Influence Distance", settings.splineFilter.distance);
                        settings.splineFilter.mode = (PCGSettings.SplineFilter.SplineMode)EditorGUILayout.EnumPopup("Mode", settings.splineFilter.mode);

                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.EndVertical();
                }

                // Mask Filter
                showMaskSettings = EditorGUILayout.Foldout(showMaskSettings, "Texture Mask Filter", true);
                if (showMaskSettings)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    settings.maskFilter.enabled = EditorGUILayout.ToggleLeft("Enable Texture Mask", settings.maskFilter.enabled);
                    if (settings.maskFilter.enabled)
                    {
                        EditorGUI.indentLevel++;
                        settings.maskFilter.name = EditorGUILayout.TextField("Name", settings.maskFilter.name);

                        settings.maskFilter.maskTexture = (Texture2D)EditorGUILayout.ObjectField("Mask Texture", settings.maskFilter.maskTexture, typeof(Texture2D), false);
                        settings.maskFilter.channel = (PCGSettings.MaskFilter.MaskChannel)EditorGUILayout.EnumPopup("Channel", settings.maskFilter.channel);
                        settings.maskFilter.threshold = EditorGUILayout.Slider("Threshold", settings.maskFilter.threshold, 0, 1);
                        settings.maskFilter.invert = EditorGUILayout.Toggle("Invert Mask", settings.maskFilter.invert);

                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.EndVertical();
                }

                // Vertex Color Filter
                showVertexColorSettings = EditorGUILayout.Foldout(showVertexColorSettings, "Vertex Color Filter", true);
                if (showVertexColorSettings)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    settings.vertexColorFilter.enabled = EditorGUILayout.ToggleLeft("Enable Vertex Color Filter", settings.vertexColorFilter.enabled);
                    if (settings.vertexColorFilter.enabled)
                    {
                        EditorGUI.indentLevel++;
                        settings.vertexColorFilter.name = EditorGUILayout.TextField("Name", settings.vertexColorFilter.name);

                        settings.vertexColorFilter.channel = (PCGSettings.VertexColorFilter.ColorChannel)EditorGUILayout.EnumPopup("Channel", settings.vertexColorFilter.channel);
                        settings.vertexColorFilter.threshold = EditorGUILayout.Slider("Threshold", settings.vertexColorFilter.threshold, 0, 1);
                        settings.vertexColorFilter.invert = EditorGUILayout.Toggle("Invert Filter", settings.vertexColorFilter.invert);

                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.EndVertical();
            }

            // Advanced Settings
            showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "Advanced Settings", true);
            if (showAdvancedSettings)
            {
                EditorGUILayout.BeginVertical(boxStyle);

                settings.maxPlacementAttempts = EditorGUILayout.IntField("Max Placement Attempts", settings.maxPlacementAttempts);

                EditorGUILayout.HelpBox("For custom extensions and scripting, use the PCGExtensionAPI class.", MessageType.Info);

                if (GUILayout.Button("Add Example Extension"))
                {
                    // Register an example extension
                    PCGExtensionAPI.RegisterValidationCallback((pos, normal, prefab, s) => 
                        PCGExtensionAPI.MaterialFilter(pos, normal, "Grass"));

                    EditorUtility.DisplayDialog("Extension Added", 
                        "Added example material filter extension that only allows placement on materials containing 'Grass' in their name.", "OK");
                }

                EditorGUILayout.EndVertical();
            }

            // Preview Settings
            showPreviewSettings = EditorGUILayout.Foldout(showPreviewSettings, "Preview Settings", true);
            if (showPreviewSettings)
            {
                EditorGUILayout.BeginVertical(boxStyle);

                autoUpdatePreview = EditorGUILayout.Toggle("Auto-Update Preview", autoUpdatePreview);

                EditorGUILayout.BeginHorizontal();
                bool newShowMeshPreviews = EditorGUILayout.Toggle("Show Mesh Previews", showMeshPreviews);
                if (newShowMeshPreviews != showMeshPreviews)
                {
                    showMeshPreviews = newShowMeshPreviews;
                    previewGizmos.ToggleMeshPreviews(showMeshPreviews);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.LabelField("Preview Count: " + previewData.Count);

                EditorGUILayout.EndVertical();
            }

            // Action Buttons
            showActionButtons = EditorGUILayout.Foldout(showActionButtons, "Actions", true);
            if (showActionButtons)
            {
                EditorGUILayout.BeginVertical(boxStyle);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Generate Preview", buttonStyle))
                {
                    UpdatePreview();
                }

                if (GUILayout.Button("Clear Preview", buttonStyle))
                {
                    ClearPreview();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Bake Prefabs", buttonStyle))
                {
                    BakePrefabs();
                }

                if (GUILayout.Button("Clear Baked", buttonStyle))
                {
                    bakeManager.Clear();
                }
                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button("Export to Prefab"))
                {
                    string path = EditorUtility.SaveFilePanelInProject("Export PCG to Prefab", "PCG_Generated", "prefab", "Save PCG objects as prefab");
                    if (!string.IsNullOrEmpty(path))
                    {
                        bakeManager.ExportToPrefab(path);
                    }
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();

            // Check for changes and update preview if needed
            if (EditorGUI.EndChangeCheck() && autoUpdatePreview)
            {
                UpdatePreview();
            }
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (previewGizmos != null)
            {
                previewGizmos.OnSceneGUI(sceneView);
            }
        }

        private void UpdatePreview()
        {
            if (settings == null || placementEngine == null || previewGizmos == null)
                return;

            // Recreate the placement engine with updated settings
            placementEngine = new PCGPlacementEngine(settings);

            // Generate new placements
            previewData = placementEngine.GeneratePlacements();

            // Update the preview gizmos
            previewGizmos.SetPreviewData(previewData);

            // Repaint scene views
            SceneView.RepaintAll();
        }

        private void ClearPreview()
        {
            previewData.Clear();
            previewGizmos.Clear();
            SceneView.RepaintAll();
        }

        private void BakePrefabs()
        {
            if (previewData.Count == 0)
            {
                // Generate new placements if there are none
                previewData = placementEngine.GeneratePlacements();
                previewGizmos.SetPreviewData(previewData);
            }

            // Bake the placements
            bakeManager.Bake(previewData);

            // Clear the preview
            ClearPreview();
        }
    }
}
