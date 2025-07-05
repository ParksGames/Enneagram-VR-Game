using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

namespace QuickLevel
{
    public class QuickLevelEditorWindow : EditorWindow
    {
        #region Variables

        // Tab management
        private int selectedTab = 0;
        private readonly string[] tabNames = { "Object Tools", "Transform Tools", "Import Settings", "Material Tools", "Layer Tools", "Shortcuts" };
        private GUIContent[] tabContents;

        // Material tools
        private Material selectedMaterial;
        private bool applyMaterialToChildren = false;
        private Vector2 materialScrollPosition;
        private List<Material> recentMaterials = new List<Material>();
        private const int maxRecentMaterials = 10;

        // Layer tools
        private int selectedLayer = 0;
        private bool applyLayerToChildren = false;
        private string[] layerNames;

        // Object selection
        private GameObject[] selectedObjects;
        private Vector2 objectListScrollPosition;
        private string searchFilter = "";
        private bool showHierarchy = true;

        // Prefab placement
        private GameObject selectedPrefab;
        private Vector2 prefabScrollPosition;
        private List<GameObject> recentPrefabs = new List<GameObject>();
        private const int maxRecentPrefabs = 10;

        // Renaming
        private string renamePrefix = "";
        private string renameSuffix = "";
        private string renameSearch = "";
        private string renameReplace = "";
        private bool renameKeepNumber = true;
        private int renameStartNumber = 1;
        private int renameIncrement = 1;
        private int renameDigits = 2;

        // Transform tools
        private Vector3 positionOffset = Vector3.zero;
        private Vector3 rotationOffset = Vector3.zero;
        private Vector3 scaleOffset = Vector3.one;
        private float gridSize = 1f;
        private bool snapToGrid = false;
        private bool alignToSurface = false;
        private float raycastDistance = 100f;
        private LayerMask raycastLayers = -1;

        // Import settings
        private bool generateLightmapUVs = true;
        private bool readWriteEnabled = true;
        private bool optimizeMesh = true;
        private ModelImporterMeshCompression meshCompression = ModelImporterMeshCompression.Off;
        private bool importBlendShapes = true;
        private bool importVisibility = true;
        private bool importCameras = true;
        private bool importLights = true;
        private bool preserveHierarchy = true;

        // Shortcuts
        private Dictionary<string, KeyCode> shortcuts = new Dictionary<string, KeyCode>
        {
            { "Duplicate", KeyCode.D },
            { "Delete", KeyCode.Delete },
            { "Focus", KeyCode.F },
            { "Toggle Static", KeyCode.S },
            { "Align to Ground", KeyCode.G },
            { "Reset Transform", KeyCode.R },
            { "Group Selected", KeyCode.N },
            { "Snap to Grid", KeyCode.T },
            { "Distribute X", KeyCode.X },
            { "Distribute Y", KeyCode.Y },
            { "Distribute Z", KeyCode.Z },
            { "Place Prefab", KeyCode.P },
            { "Apply Material", KeyCode.M },
            { "Apply Layer", KeyCode.L }
        };
        private Dictionary<string, bool> shortcutModifiers = new Dictionary<string, bool>
        {
            { "Duplicate", true },
            { "Delete", false },
            { "Focus", false },
            { "Toggle Static", true },
            { "Align to Ground", true },
            { "Reset Transform", true },
            { "Group Selected", true },
            { "Snap to Grid", true },
            { "Distribute X", true },
            { "Distribute Y", true },
            { "Distribute Z", true },
            { "Place Prefab", true },
            { "Apply Material", true },
            { "Apply Layer", true }
        };

        // UI
        private GUIStyle headerStyle;
        private GUIStyle subHeaderStyle;
        private GUIStyle buttonStyle;
        private GUIStyle selectedButtonStyle;
        private Color defaultColor;
        private Color highlightColor = new Color(0.2f, 0.6f, 1f);
        private Texture2D logoTexture;
        private bool initialized = false;
        #endregion

        #region Window Management
        [MenuItem("Window/QuickLevel Editor")]
        public static void ShowWindow()
        {
            GetWindow<QuickLevelEditorWindow>("QuickLevel Editor");
        }

        private void OnEnable()
        {
            // Register for selection change events
            Selection.selectionChanged += OnSelectionChanged;
            SceneView.duringSceneGui += OnSceneGUI;
            Undo.undoRedoPerformed += Repaint;

            // Initialize tab contents with icons
            tabContents = new GUIContent[]
            {
                new GUIContent(" Object Tools", EditorGUIUtility.IconContent("GameObject Icon").image),
                new GUIContent(" Transform Tools", EditorGUIUtility.IconContent("Transform Icon").image),
                new GUIContent(" Import Settings", EditorGUIUtility.IconContent("Mesh Icon").image),
                new GUIContent(" Material Tools", EditorGUIUtility.IconContent("Material Icon").image),
                new GUIContent(" Layer Tools", EditorGUIUtility.IconContent("FilterByLabel").image),
                new GUIContent(" Shortcuts", EditorGUIUtility.IconContent("Keyboard").image)
            };

            // Initialize layer names
            InitializeLayerNames();

            // Load saved settings
            LoadSettings();
        }

        private void InitializeLayerNames()
        {
            // Get all layer names
            layerNames = new string[32];
            for (int i = 0; i < 32; i++)
            {
                string layerName = LayerMask.LayerToName(i);
                layerNames[i] = string.IsNullOrEmpty(layerName) ? $"Layer {i}" : layerName;
            }
        }

        private void OnDisable()
        {
            // Unregister events
            Selection.selectionChanged -= OnSelectionChanged;
            SceneView.duringSceneGui -= OnSceneGUI;
            Undo.undoRedoPerformed -= Repaint;

            // Save settings
            SaveSettings();
        }

        private void OnSelectionChanged()
        {
            selectedObjects = Selection.gameObjects;
            Repaint();
        }

        private void InitializeStyles()
        {
            if (initialized)
                return;

            defaultColor = GUI.color;

            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft,
                margin = new RectOffset(5, 5, 10, 5)
            };

            subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
                margin = new RectOffset(5, 5, 5, 5)
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                padding = new RectOffset(10, 10, 5, 5),
                margin = new RectOffset(2, 2, 2, 2)
            };

            selectedButtonStyle = new GUIStyle(buttonStyle)
            {
                normal = { background = EditorGUIUtility.Load("IN BigTitle") as Texture2D }
            };

            initialized = true;
        }

        private void SaveSettings()
        {
            EditorPrefs.SetInt("QuickLevel_SelectedTab", selectedTab);
            EditorPrefs.SetBool("QuickLevel_ShowHierarchy", showHierarchy);
            EditorPrefs.SetFloat("QuickLevel_GridSize", gridSize);
            EditorPrefs.SetBool("QuickLevel_SnapToGrid", snapToGrid);
            EditorPrefs.SetBool("QuickLevel_AlignToSurface", alignToSurface);
            EditorPrefs.SetFloat("QuickLevel_RaycastDistance", raycastDistance);
            EditorPrefs.SetBool("QuickLevel_GenerateLightmapUVs", generateLightmapUVs);
            EditorPrefs.SetBool("QuickLevel_ReadWriteEnabled", readWriteEnabled);
            EditorPrefs.SetBool("QuickLevel_OptimizeMesh", optimizeMesh);
            EditorPrefs.SetInt("QuickLevel_MeshCompression", (int)meshCompression);
            EditorPrefs.SetBool("QuickLevel_ImportBlendShapes", importBlendShapes);
            EditorPrefs.SetBool("QuickLevel_ImportVisibility", importVisibility);
            EditorPrefs.SetBool("QuickLevel_ImportCameras", importCameras);
            EditorPrefs.SetBool("QuickLevel_ImportLights", importLights);
            EditorPrefs.SetBool("QuickLevel_PreserveHierarchy", preserveHierarchy);

            // Save shortcuts
            foreach (var shortcut in shortcuts)
            {
                EditorPrefs.SetInt("QuickLevel_Shortcut_" + shortcut.Key, (int)shortcut.Value);
                EditorPrefs.SetBool("QuickLevel_ShortcutModifier_" + shortcut.Key, shortcutModifiers[shortcut.Key]);
            }
        }

        private void LoadSettings()
        {
            selectedTab = EditorPrefs.GetInt("QuickLevel_SelectedTab", 0);
            showHierarchy = EditorPrefs.GetBool("QuickLevel_ShowHierarchy", true);
            gridSize = EditorPrefs.GetFloat("QuickLevel_GridSize", 1f);
            snapToGrid = EditorPrefs.GetBool("QuickLevel_SnapToGrid", false);
            alignToSurface = EditorPrefs.GetBool("QuickLevel_AlignToSurface", false);
            raycastDistance = EditorPrefs.GetFloat("QuickLevel_RaycastDistance", 100f);
            generateLightmapUVs = EditorPrefs.GetBool("QuickLevel_GenerateLightmapUVs", true);
            readWriteEnabled = EditorPrefs.GetBool("QuickLevel_ReadWriteEnabled", true);
            optimizeMesh = EditorPrefs.GetBool("QuickLevel_OptimizeMesh", true);
            meshCompression = (ModelImporterMeshCompression)EditorPrefs.GetInt("QuickLevel_MeshCompression", 0);
            importBlendShapes = EditorPrefs.GetBool("QuickLevel_ImportBlendShapes", true);
            importVisibility = EditorPrefs.GetBool("QuickLevel_ImportVisibility", true);
            importCameras = EditorPrefs.GetBool("QuickLevel_ImportCameras", true);
            importLights = EditorPrefs.GetBool("QuickLevel_ImportLights", true);
            preserveHierarchy = EditorPrefs.GetBool("QuickLevel_PreserveHierarchy", true);

            // Load shortcuts
            foreach (var key in shortcuts.Keys.ToList())
            {
                if (EditorPrefs.HasKey("QuickLevel_Shortcut_" + key))
                {
                    shortcuts[key] = (KeyCode)EditorPrefs.GetInt("QuickLevel_Shortcut_" + key);
                    shortcutModifiers[key] = EditorPrefs.GetBool("QuickLevel_ShortcutModifier_" + key);
                }
            }

            // Update selection
            selectedObjects = Selection.gameObjects;
        }
        #endregion

        #region GUI
        private void OnGUI()
        {
            InitializeStyles();

            // Draw header
            DrawHeader();

            // Draw tabs
            DrawTabs();

            // Draw content based on selected tab
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            switch (selectedTab)
            {
                case 0:
                    DrawObjectTools();
                    break;
                case 1:
                    DrawTransformTools();
                    break;
                case 2:
                    DrawImportSettings();
                    break;
                case 3:
                    DrawMaterialTools();
                    break;
                case 4:
                    DrawLayerTools();
                    break;
                case 5:
                    DrawShortcuts();
                    break;
            }
            EditorGUILayout.EndVertical();

            // Draw footer
            DrawFooter();

            // Process keyboard shortcuts
            ProcessShortcuts();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("QuickLevel Editor", headerStyle, GUILayout.ExpandWidth(true));

            if (GUILayout.Button("Help", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                ShowHelpWindow();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTabs()
        {
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < tabContents.Length; i++)
            {
                GUI.color = (selectedTab == i) ? highlightColor : defaultColor;
                if (GUILayout.Button(tabContents[i], (selectedTab == i) ? selectedButtonStyle : buttonStyle, GUILayout.Height(30)))
                {
                    selectedTab = i;
                }
            }
            GUI.color = defaultColor;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Selection: " + (selectedObjects != null ? selectedObjects.Length : 0) + " objects", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void ShowHelpWindow()
        {
            EditorUtility.DisplayDialog("QuickLevel Editor Help",
                "QuickLevel Editor is a comprehensive tool for speeding up level design.\n\n" +
                "Object Tools: Manage scene objects, rename, toggle static flags, and more.\n\n" +
                "Transform Tools: Manipulate object transforms with precision, align, distribute, and snap to grid.\n\n" +
                "Import Settings: Quickly modify mesh import settings for multiple assets.\n\n" +
                "Shortcuts: Customize keyboard shortcuts for common operations.\n\n" +
                "For more information, hover over any control to see a tooltip.",
                "OK");
        }
        #endregion

        #region Object Tools
        private void DrawObjectTools()
        {
            EditorGUILayout.BeginVertical();

            // Selection info
            EditorGUILayout.LabelField("Selection", subHeaderStyle);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent(" Select All", EditorGUIUtility.IconContent("d_SelectAll").image), buttonStyle))
            {
                SelectAllObjects();
            }
            if (GUILayout.Button(new GUIContent(" Select None", EditorGUIUtility.IconContent("d_ViewToolZoom").image), buttonStyle))
            {
                Selection.objects = new Object[0];
            }
            if (GUILayout.Button(new GUIContent(" Invert Selection", EditorGUIUtility.IconContent("d_ToggleUVOverlay").image), buttonStyle))
            {
                InvertSelection();
            }
            EditorGUILayout.EndHorizontal();

            // Search filter
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Filter:", GUILayout.Width(40));
            string newFilter = EditorGUILayout.TextField(searchFilter);
            if (newFilter != searchFilter)
            {
                searchFilter = newFilter;
            }
            if (GUILayout.Button("Clear", GUILayout.Width(60)))
            {
                searchFilter = "";
            }
            EditorGUILayout.EndHorizontal();

            // Toggle hierarchy view
            showHierarchy = EditorGUILayout.Toggle("Show Hierarchy", showHierarchy);

            // Object list
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Scene Objects", subHeaderStyle);

            objectListScrollPosition = EditorGUILayout.BeginScrollView(objectListScrollPosition, GUILayout.Height(150));
            DrawSceneObjects();
            EditorGUILayout.EndScrollView();

            // Static flags
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Static Flags", subHeaderStyle);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent(" Set Static", EditorGUIUtility.IconContent("d_Prefab Icon").image), buttonStyle))
            {
                SetStaticFlags(true);
            }
            if (GUILayout.Button(new GUIContent(" Unset Static", EditorGUIUtility.IconContent("d_PrefabVariant Icon").image), buttonStyle))
            {
                SetStaticFlags(false);
            }
            if (GUILayout.Button(new GUIContent(" Toggle Static", EditorGUIUtility.IconContent("d_ToggleUVOverlay").image), buttonStyle))
            {
                ToggleStaticFlags();
            }
            EditorGUILayout.EndHorizontal();

            // Detailed static flags
            if (selectedObjects != null && selectedObjects.Length > 0)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Batch Static Flags", buttonStyle))
                {
                    StaticFlagsWindow.ShowWindow(selectedObjects);
                }
                EditorGUILayout.EndHorizontal();
            }

            // Renaming tools
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Rename Tools", subHeaderStyle);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Prefix:", GUILayout.Width(50));
            renamePrefix = EditorGUILayout.TextField(renamePrefix);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Suffix:", GUILayout.Width(50));
            renameSuffix = EditorGUILayout.TextField(renameSuffix);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            renameSearch = EditorGUILayout.TextField(renameSearch);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Replace:", GUILayout.Width(50));
            renameReplace = EditorGUILayout.TextField(renameReplace);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            renameKeepNumber = EditorGUILayout.Toggle("Add Numbers", renameKeepNumber, GUILayout.Width(100));
            if (renameKeepNumber)
            {
                EditorGUILayout.LabelField("Start:", GUILayout.Width(40));
                renameStartNumber = EditorGUILayout.IntField(renameStartNumber, GUILayout.Width(40));
                EditorGUILayout.LabelField("Step:", GUILayout.Width(40));
                renameIncrement = EditorGUILayout.IntField(renameIncrement, GUILayout.Width(40));
                EditorGUILayout.LabelField("Digits:", GUILayout.Width(40));
                renameDigits = EditorGUILayout.IntField(renameDigits, GUILayout.Width(40));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent(" Rename Selected", EditorGUIUtility.IconContent("d_editicon.sml").image), buttonStyle))
            {
                RenameSelectedObjects();
            }
            EditorGUILayout.EndHorizontal();

            // Prefab placement
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Prefab Placement", subHeaderStyle);

            EditorGUILayout.BeginHorizontal();
            selectedPrefab = (GameObject)EditorGUILayout.ObjectField("Prefab:", selectedPrefab, typeof(GameObject), false);
            if (GUILayout.Button("Place", GUILayout.Width(60)) && selectedPrefab != null)
            {
                PlacePrefab();
            }
            EditorGUILayout.EndHorizontal();

            // Recent prefabs
            if (recentPrefabs.Count > 0)
            {
                EditorGUILayout.LabelField("Recent Prefabs:");
                prefabScrollPosition = EditorGUILayout.BeginScrollView(prefabScrollPosition, GUILayout.Height(100));
                for (int i = 0; i < recentPrefabs.Count; i++)
                {
                    if (recentPrefabs[i] == null)
                        continue;

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label(recentPrefabs[i].name, GUILayout.MaxWidth(150));
                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                    {
                        selectedPrefab = recentPrefabs[i];
                    }
                    if (GUILayout.Button("Place", GUILayout.Width(60)))
                    {
                        selectedPrefab = recentPrefabs[i];
                        PlacePrefab();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }

            // Grouping
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Grouping", subHeaderStyle);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent(" Group Selected", EditorGUIUtility.IconContent("d_Toolbar Plus").image), buttonStyle))
            {
                GroupSelectedObjects();
            }
            if (GUILayout.Button(new GUIContent(" Ungroup", EditorGUIUtility.IconContent("d_Toolbar Minus").image), buttonStyle))
            {
                UngroupSelectedObjects();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawSceneObjects()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            GameObject[] rootObjects = activeScene.GetRootGameObjects();

            foreach (GameObject obj in rootObjects)
            {
                if (string.IsNullOrEmpty(searchFilter) || obj.name.ToLower().Contains(searchFilter.ToLower()))
                {
                    DrawObjectInList(obj, 0);
                }
                else if (showHierarchy)
                {
                    // Check if any child matches the filter
                    bool hasMatchingChild = HasMatchingChild(obj, searchFilter.ToLower());
                    if (hasMatchingChild)
                    {
                        DrawObjectInList(obj, 0);
                    }
                }
            }
        }

        private bool HasMatchingChild(GameObject obj, string filter)
        {
            if (obj.name.ToLower().Contains(filter))
                return true;

            foreach (Transform child in obj.transform)
            {
                if (HasMatchingChild(child.gameObject, filter))
                    return true;
            }

            return false;
        }

        private void DrawObjectInList(GameObject obj, int indentLevel)
        {
            bool isSelected = selectedObjects != null && System.Array.Exists(selectedObjects, element => element == obj);

            EditorGUILayout.BeginHorizontal();

            // Indent
            GUILayout.Space(indentLevel * 20);

            // Foldout if has children
            bool hasChildren = obj.transform.childCount > 0;
            bool expanded = EditorPrefs.GetBool("QuickLevel_Expanded_" + obj.GetInstanceID(), false);

            if (hasChildren)
            {
                expanded = EditorGUILayout.Foldout(expanded, "", true, GUI.skin.label);
                EditorPrefs.SetBool("QuickLevel_Expanded_" + obj.GetInstanceID(), expanded);
            }
            else
            {
                GUILayout.Space(14);
            }

            // Object name with selection highlight
            GUI.color = isSelected ? highlightColor : defaultColor;
            if (GUILayout.Button(obj.name, EditorStyles.label))
            {
                if (Event.current.control || Event.current.command)
                {
                    // Add to selection
                    List<Object> newSelection = new List<Object>(Selection.objects);
                    if (newSelection.Contains(obj))
                        newSelection.Remove(obj);
                    else
                        newSelection.Add(obj);
                    Selection.objects = newSelection.ToArray();
                }
                else
                {
                    // Set as only selection
                    Selection.activeGameObject = obj;
                }
            }
            GUI.color = defaultColor;

            // Static toggle
            bool isStatic = obj.isStatic;
            bool newStatic = EditorGUILayout.Toggle(isStatic, GUILayout.Width(20));
            if (newStatic != isStatic)
            {
                Undo.RecordObject(obj, "Toggle Static Flag");
                obj.isStatic = newStatic;
            }

            // Visibility toggle
            bool isVisible = obj.activeSelf;
            bool newVisible = EditorGUILayout.Toggle(isVisible, GUILayout.Width(20));
            if (newVisible != isVisible)
            {
                Undo.RecordObject(obj, "Toggle Visibility");
                obj.SetActive(newVisible);
            }

            EditorGUILayout.EndHorizontal();

            // Draw children if expanded
            if (hasChildren && expanded && showHierarchy)
            {
                foreach (Transform child in obj.transform)
                {
                    if (string.IsNullOrEmpty(searchFilter) || child.name.ToLower().Contains(searchFilter.ToLower()))
                    {
                        DrawObjectInList(child.gameObject, indentLevel + 1);
                    }
                    else
                    {
                        // Check if any child matches the filter
                        bool hasMatchingChild = HasMatchingChild(child.gameObject, searchFilter.ToLower());
                        if (hasMatchingChild)
                        {
                            DrawObjectInList(child.gameObject, indentLevel + 1);
                        }
                    }
                }
            }
        }

        private void SelectAllObjects()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            List<GameObject> allObjects = new List<GameObject>();

            GameObject[] rootObjects = activeScene.GetRootGameObjects();
            foreach (GameObject root in rootObjects)
            {
                allObjects.Add(root);
                GetAllChildren(root.transform, allObjects);
            }

            Selection.objects = allObjects.ToArray();
        }

        private void GetAllChildren(Transform parent, List<GameObject> result)
        {
            foreach (Transform child in parent)
            {
                result.Add(child.gameObject);
                GetAllChildren(child, result);
            }
        }

        private void InvertSelection()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            List<GameObject> allObjects = new List<GameObject>();
            List<GameObject> invertedSelection = new List<GameObject>();

            GameObject[] rootObjects = activeScene.GetRootGameObjects();
            foreach (GameObject root in rootObjects)
            {
                allObjects.Add(root);
                GetAllChildren(root.transform, allObjects);
            }

            foreach (GameObject obj in allObjects)
            {
                if (selectedObjects == null || !System.Array.Exists(selectedObjects, element => element == obj))
                {
                    invertedSelection.Add(obj);
                }
            }

            Selection.objects = invertedSelection.ToArray();
        }

        private void SetStaticFlags(bool isStatic)
        {
            if (selectedObjects == null || selectedObjects.Length == 0)
                return;

            Undo.RecordObjects(selectedObjects, isStatic ? "Set Static Flags" : "Unset Static Flags");

            foreach (GameObject obj in selectedObjects)
            {
                obj.isStatic = isStatic;
            }
        }

        private void ToggleStaticFlags()
        {
            if (selectedObjects == null || selectedObjects.Length == 0)
                return;

            Undo.RecordObjects(selectedObjects, "Toggle Static Flags");

            foreach (GameObject obj in selectedObjects)
            {
                obj.isStatic = !obj.isStatic;
            }
        }

        private void RenameSelectedObjects()
        {
            if (selectedObjects == null || selectedObjects.Length == 0)
                return;

            Undo.RecordObjects(selectedObjects, "Rename Objects");

            int counter = renameStartNumber;

            foreach (GameObject obj in selectedObjects)
            {
                string newName = obj.name;

                // Apply search and replace
                if (!string.IsNullOrEmpty(renameSearch))
                {
                    newName = newName.Replace(renameSearch, renameReplace);
                }

                // Apply prefix and suffix
                newName = renamePrefix + newName + renameSuffix;

                // Apply numbering
                if (renameKeepNumber)
                {
                    string format = new string('0', renameDigits);
                    newName += counter.ToString(format);
                    counter += renameIncrement;
                }

                obj.name = newName;
            }
        }

        private void PlacePrefab()
        {
            if (selectedPrefab == null)
                return;

            // Add to recent prefabs
            if (!recentPrefabs.Contains(selectedPrefab))
            {
                recentPrefabs.Insert(0, selectedPrefab);
                if (recentPrefabs.Count > maxRecentPrefabs)
                {
                    recentPrefabs.RemoveAt(recentPrefabs.Count - 1);
                }
            }
            else
            {
                // Move to top
                recentPrefabs.Remove(selectedPrefab);
                recentPrefabs.Insert(0, selectedPrefab);
            }

            // Get placement position
            Vector3 position = Vector3.zero;

            // If scene view is available, use its position
            if (SceneView.lastActiveSceneView != null)
            {
                position = SceneView.lastActiveSceneView.pivot;
            }

            // If objects are selected, use their center
            if (selectedObjects != null && selectedObjects.Length > 0)
            {
                Vector3 center = Vector3.zero;
                foreach (GameObject obj in selectedObjects)
                {
                    center += obj.transform.position;
                }
                position = center / selectedObjects.Length;
            }

            // Create the prefab instance
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(selectedPrefab);
            Undo.RegisterCreatedObjectUndo(instance, "Place Prefab");

            // Position the instance
            instance.transform.position = position;

            // Apply grid snapping if enabled
            if (snapToGrid)
            {
                instance.transform.position = new Vector3(
                    Mathf.Round(instance.transform.position.x / gridSize) * gridSize,
                    Mathf.Round(instance.transform.position.y / gridSize) * gridSize,
                    Mathf.Round(instance.transform.position.z / gridSize) * gridSize
                );
            }

            // Apply surface alignment if enabled
            if (alignToSurface)
            {
                AlignToSurface(instance);
            }

            // Select the new instance
            Selection.activeGameObject = instance;
        }

        private void GroupSelectedObjects()
        {
            if (selectedObjects == null || selectedObjects.Length < 2)
                return;

            // Create a new parent object
            GameObject group = new GameObject("Group");
            Undo.RegisterCreatedObjectUndo(group, "Group Objects");

            // Calculate the center position of all selected objects
            Vector3 center = Vector3.zero;
            foreach (GameObject obj in selectedObjects)
            {
                center += obj.transform.position;
            }
            center /= selectedObjects.Length;

            // Position the group at the center
            group.transform.position = center;

            // Parent all selected objects to the group
            foreach (GameObject obj in selectedObjects)
            {
                Undo.SetTransformParent(obj.transform, group.transform, "Group Objects");
            }

            // Select the new group
            Selection.activeGameObject = group;
        }

        private void UngroupSelectedObjects()
        {
            if (selectedObjects == null || selectedObjects.Length == 0)
                return;

            List<GameObject> newSelection = new List<GameObject>();

            foreach (GameObject parent in selectedObjects)
            {
                if (parent.transform.childCount == 0)
                    continue;

                // Get all children
                List<Transform> children = new List<Transform>();
                foreach (Transform child in parent.transform)
                {
                    children.Add(child);
                }

                // Unparent all children
                foreach (Transform child in children)
                {
                    Undo.SetTransformParent(child, parent.transform.parent, "Ungroup Objects");
                    newSelection.Add(child.gameObject);
                }

                // Delete the parent
                Undo.DestroyObjectImmediate(parent);
            }

            // Select all the ungrouped objects
            Selection.objects = newSelection.ToArray();
        }
        #endregion

        #region Transform Tools
        private void DrawTransformTools()
        {
            EditorGUILayout.BeginVertical();

            // Position tools
            EditorGUILayout.LabelField("Position", subHeaderStyle);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Offset:", GUILayout.Width(50));
            positionOffset = EditorGUILayout.Vector3Field("", positionOffset);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply Offset", buttonStyle))
            {
                ApplyPositionOffset();
            }
            if (GUILayout.Button("Reset Position", buttonStyle))
            {
                ResetPosition();
            }
            EditorGUILayout.EndHorizontal();

            // Grid snapping
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Grid Snapping", subHeaderStyle);

            EditorGUILayout.BeginHorizontal();
            snapToGrid = EditorGUILayout.Toggle("Snap to Grid", snapToGrid);
            EditorGUILayout.LabelField("Grid Size:", GUILayout.Width(60));
            gridSize = EditorGUILayout.FloatField(gridSize, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Snap Selected", buttonStyle))
            {
                SnapSelectedToGrid();
            }
            EditorGUILayout.EndHorizontal();

            // Rotation tools
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Rotation", subHeaderStyle);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Rotate:", GUILayout.Width(50));
            rotationOffset = EditorGUILayout.Vector3Field("", rotationOffset);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply Rotation", buttonStyle))
            {
                ApplyRotationOffset();
            }
            if (GUILayout.Button("Reset Rotation", buttonStyle))
            {
                ResetRotation();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Rotate X 90°", buttonStyle))
            {
                RotateSelected(new Vector3(90, 0, 0));
            }
            if (GUILayout.Button("Rotate Y 90°", buttonStyle))
            {
                RotateSelected(new Vector3(0, 90, 0));
            }
            if (GUILayout.Button("Rotate Z 90°", buttonStyle))
            {
                RotateSelected(new Vector3(0, 0, 90));
            }
            EditorGUILayout.EndHorizontal();

            // Scale tools
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Scale", subHeaderStyle);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Scale:", GUILayout.Width(50));
            scaleOffset = EditorGUILayout.Vector3Field("", scaleOffset);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply Scale", buttonStyle))
            {
                ApplyScaleOffset();
            }
            if (GUILayout.Button("Reset Scale", buttonStyle))
            {
                ResetScale();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Scale x2", buttonStyle))
            {
                ScaleSelected(new Vector3(2, 2, 2));
            }
            if (GUILayout.Button("Scale x0.5", buttonStyle))
            {
                ScaleSelected(new Vector3(0.5f, 0.5f, 0.5f));
            }
            EditorGUILayout.EndHorizontal();

            // Alignment tools
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Alignment", subHeaderStyle);

            EditorGUILayout.BeginHorizontal();
            alignToSurface = EditorGUILayout.Toggle("Align to Surface", alignToSurface);
            EditorGUILayout.LabelField("Ray Distance:", GUILayout.Width(80));
            raycastDistance = EditorGUILayout.FloatField(raycastDistance, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Align Selected", buttonStyle))
            {
                AlignSelectedToSurface();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Distribution", subHeaderStyle);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Distribute X", buttonStyle))
            {
                DistributeSelected(0);
            }
            if (GUILayout.Button("Distribute Y", buttonStyle))
            {
                DistributeSelected(1);
            }
            if (GUILayout.Button("Distribute Z", buttonStyle))
            {
                DistributeSelected(2);
            }
            EditorGUILayout.EndHorizontal();

            // Reset all
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset All Transforms", buttonStyle, GUILayout.Height(30)))
            {
                ResetAllTransforms();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void ApplyPositionOffset()
        {
            if (selectedObjects == null || selectedObjects.Length == 0)
                return;

            Undo.RecordObjects(selectedObjects.Select(o => o.transform).ToArray(), "Apply Position Offset");

            foreach (GameObject obj in selectedObjects)
            {
                obj.transform.position += positionOffset;
            }
        }

        private void ResetPosition()
        {
            if (selectedObjects == null || selectedObjects.Length == 0)
                return;

            Undo.RecordObjects(selectedObjects.Select(o => o.transform).ToArray(), "Reset Position");

            foreach (GameObject obj in selectedObjects)
            {
                obj.transform.position = Vector3.zero;
            }
        }

        private void SnapSelectedToGrid()
        {
            if (selectedObjects == null || selectedObjects.Length == 0 || gridSize <= 0)
                return;

            Undo.RecordObjects(selectedObjects.Select(o => o.transform).ToArray(), "Snap to Grid");

            foreach (GameObject obj in selectedObjects)
            {
                Vector3 position = obj.transform.position;
                position.x = Mathf.Round(position.x / gridSize) * gridSize;
                position.y = Mathf.Round(position.y / gridSize) * gridSize;
                position.z = Mathf.Round(position.z / gridSize) * gridSize;
                obj.transform.position = position;
            }
        }

        private void ApplyRotationOffset()
        {
            if (selectedObjects == null || selectedObjects.Length == 0)
                return;

            Undo.RecordObjects(selectedObjects.Select(o => o.transform).ToArray(), "Apply Rotation Offset");

            foreach (GameObject obj in selectedObjects)
            {
                obj.transform.Rotate(rotationOffset);
            }
        }

        private void ResetRotation()
        {
            if (selectedObjects == null || selectedObjects.Length == 0)
                return;

            Undo.RecordObjects(selectedObjects.Select(o => o.transform).ToArray(), "Reset Rotation");

            foreach (GameObject obj in selectedObjects)
            {
                obj.transform.rotation = Quaternion.identity;
            }
        }

        private void RotateSelected(Vector3 angles)
        {
            if (selectedObjects == null || selectedObjects.Length == 0)
                return;

            Undo.RecordObjects(selectedObjects.Select(o => o.transform).ToArray(), "Rotate Objects");

            foreach (GameObject obj in selectedObjects)
            {
                obj.transform.Rotate(angles);
            }
        }

        private void ApplyScaleOffset()
        {
            if (selectedObjects == null || selectedObjects.Length == 0)
                return;

            Undo.RecordObjects(selectedObjects.Select(o => o.transform).ToArray(), "Apply Scale Offset");

            foreach (GameObject obj in selectedObjects)
            {
                obj.transform.localScale = Vector3.Scale(obj.transform.localScale, scaleOffset);
            }
        }

        private void ResetScale()
        {
            if (selectedObjects == null || selectedObjects.Length == 0)
                return;

            Undo.RecordObjects(selectedObjects.Select(o => o.transform).ToArray(), "Reset Scale");

            foreach (GameObject obj in selectedObjects)
            {
                obj.transform.localScale = Vector3.one;
            }
        }

        private void ScaleSelected(Vector3 scale)
        {
            if (selectedObjects == null || selectedObjects.Length == 0)
                return;

            Undo.RecordObjects(selectedObjects.Select(o => o.transform).ToArray(), "Scale Objects");

            foreach (GameObject obj in selectedObjects)
            {
                obj.transform.localScale = Vector3.Scale(obj.transform.localScale, scale);
            }
        }

        private void AlignSelectedToSurface()
        {
            if (selectedObjects == null || selectedObjects.Length == 0)
                return;

            Undo.RecordObjects(selectedObjects.Select(o => o.transform).ToArray(), "Align to Surface");

            foreach (GameObject obj in selectedObjects)
            {
                AlignToSurface(obj);
            }
        }

        private void AlignToSurface(GameObject obj)
        {
            RaycastHit hit;
            if (Physics.Raycast(obj.transform.position + Vector3.up * raycastDistance, Vector3.down, out hit, raycastDistance * 2, raycastLayers))
            {
                // Position on surface
                obj.transform.position = hit.point;

                // Align rotation to surface normal
                obj.transform.up = hit.normal;
            }
        }

        private void DistributeSelected(int axis)
        {
            if (selectedObjects == null || selectedObjects.Length < 3)
                return;

            Undo.RecordObjects(selectedObjects.Select(o => o.transform).ToArray(), "Distribute Objects");

            // Sort objects by position on the selected axis
            List<GameObject> sortedObjects = new List<GameObject>(selectedObjects);
            switch (axis)
            {
                case 0: // X
                    sortedObjects.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
                    break;
                case 1: // Y
                    sortedObjects.Sort((a, b) => a.transform.position.y.CompareTo(b.transform.position.y));
                    break;
                case 2: // Z
                    sortedObjects.Sort((a, b) => a.transform.position.z.CompareTo(b.transform.position.z));
                    break;
            }

            // Get min and max positions
            float minPos = 0, maxPos = 0;
            switch (axis)
            {
                case 0: // X
                    minPos = sortedObjects[0].transform.position.x;
                    maxPos = sortedObjects[sortedObjects.Count - 1].transform.position.x;
                    break;
                case 1: // Y
                    minPos = sortedObjects[0].transform.position.y;
                    maxPos = sortedObjects[sortedObjects.Count - 1].transform.position.y;
                    break;
                case 2: // Z
                    minPos = sortedObjects[0].transform.position.z;
                    maxPos = sortedObjects[sortedObjects.Count - 1].transform.position.z;
                    break;
            }

            // Distribute objects evenly between min and max
            float step = (maxPos - minPos) / (sortedObjects.Count - 1);
            for (int i = 1; i < sortedObjects.Count - 1; i++)
            {
                Vector3 pos = sortedObjects[i].transform.position;
                switch (axis)
                {
                    case 0: // X
                        pos.x = minPos + step * i;
                        break;
                    case 1: // Y
                        pos.y = minPos + step * i;
                        break;
                    case 2: // Z
                        pos.z = minPos + step * i;
                        break;
                }
                sortedObjects[i].transform.position = pos;
            }
        }

        private void ResetAllTransforms()
        {
            if (selectedObjects == null || selectedObjects.Length == 0)
                return;

            Undo.RecordObjects(selectedObjects.Select(o => o.transform).ToArray(), "Reset All Transforms");

            foreach (GameObject obj in selectedObjects)
            {
                obj.transform.position = Vector3.zero;
                obj.transform.rotation = Quaternion.identity;
                obj.transform.localScale = Vector3.one;
            }
        }
        #endregion

        #region Import Settings
        private void DrawImportSettings()
        {
            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField("Mesh Import Settings", subHeaderStyle);
            EditorGUILayout.HelpBox("Select mesh assets in the Project window to modify their import settings.", MessageType.Info);

            EditorGUILayout.Space();

            // Mesh settings
            EditorGUILayout.LabelField("Mesh Settings", subHeaderStyle);

            generateLightmapUVs = EditorGUILayout.Toggle("Generate Lightmap UVs", generateLightmapUVs);
            readWriteEnabled = EditorGUILayout.Toggle("Read/Write Enabled", readWriteEnabled);
            optimizeMesh = EditorGUILayout.Toggle("Optimize Mesh", optimizeMesh);
            meshCompression = (ModelImporterMeshCompression)EditorGUILayout.EnumPopup("Mesh Compression", meshCompression);

            // Model settings
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Model Settings", subHeaderStyle);

            importBlendShapes = EditorGUILayout.Toggle("Import Blend Shapes", importBlendShapes);
            importVisibility = EditorGUILayout.Toggle("Import Visibility", importVisibility);
            importCameras = EditorGUILayout.Toggle("Import Cameras", importCameras);
            importLights = EditorGUILayout.Toggle("Import Lights", importLights);
            preserveHierarchy = EditorGUILayout.Toggle("Preserve Hierarchy", preserveHierarchy);

            // Apply buttons
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply to Selected Assets", buttonStyle, GUILayout.Height(30)))
            {
                ApplyImportSettings();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save as Preset", buttonStyle))
            {
                SaveImportPreset();
            }
            if (GUILayout.Button("Load Preset", buttonStyle))
            {
                LoadImportPreset();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void ApplyImportSettings()
        {
            Object[] selectedAssets = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);
            if (selectedAssets == null || selectedAssets.Length == 0)
            {
                EditorUtility.DisplayDialog("No Assets Selected", "Please select mesh assets in the Project window.", "OK");
                return;
            }

            int modifiedCount = 0;

            foreach (Object asset in selectedAssets)
            {
                string assetPath = AssetDatabase.GetAssetPath(asset);
                if (string.IsNullOrEmpty(assetPath))
                    continue;

                // Check if it's a model asset
                if (assetPath.EndsWith(".fbx") || assetPath.EndsWith(".obj") || assetPath.EndsWith(".3ds") || assetPath.EndsWith(".dae"))
                {
                    ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                    if (importer != null)
                    {
                        // Apply mesh settings
                        importer.generateSecondaryUV = generateLightmapUVs;
                        importer.isReadable = readWriteEnabled;
                        importer.optimizeMeshVertices = optimizeMesh;
                        importer.optimizeMeshPolygons = optimizeMesh;
                        importer.meshCompression = meshCompression;

                        // Apply model settings
                        importer.importBlendShapes = importBlendShapes;
                        importer.importVisibility = importVisibility;
                        importer.importCameras = importCameras;
                        importer.importLights = importLights;
                        importer.preserveHierarchy = preserveHierarchy;

                        // Apply changes
                        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                        modifiedCount++;
                    }
                }
            }

            if (modifiedCount > 0)
            {
                EditorUtility.DisplayDialog("Import Settings Applied", $"Applied import settings to {modifiedCount} assets.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("No Assets Modified", "No compatible mesh assets were found in the selection.", "OK");
            }
        }

        private void SaveImportPreset()
        {
            string presetName = EditorUtility.SaveFilePanel("Save Import Preset", Application.dataPath, "MeshImportPreset", "json");
            if (string.IsNullOrEmpty(presetName))
                return;

            ImportPreset preset = new ImportPreset
            {
                generateLightmapUVs = generateLightmapUVs,
                readWriteEnabled = readWriteEnabled,
                optimizeMesh = optimizeMesh,
                meshCompression = (int)meshCompression,
                importBlendShapes = importBlendShapes,
                importVisibility = importVisibility,
                importCameras = importCameras,
                importLights = importLights,
                preserveHierarchy = preserveHierarchy
            };

            string json = JsonUtility.ToJson(preset, true);
            System.IO.File.WriteAllText(presetName, json);

            EditorUtility.DisplayDialog("Preset Saved", "Import settings preset has been saved.", "OK");
        }

        private void LoadImportPreset()
        {
            string presetPath = EditorUtility.OpenFilePanel("Load Import Preset", Application.dataPath, "json");
            if (string.IsNullOrEmpty(presetPath))
                return;

            if (System.IO.File.Exists(presetPath))
            {
                string json = System.IO.File.ReadAllText(presetPath);
                ImportPreset preset = JsonUtility.FromJson<ImportPreset>(json);

                generateLightmapUVs = preset.generateLightmapUVs;
                readWriteEnabled = preset.readWriteEnabled;
                optimizeMesh = preset.optimizeMesh;
                meshCompression = (ModelImporterMeshCompression)preset.meshCompression;
                importBlendShapes = preset.importBlendShapes;
                importVisibility = preset.importVisibility;
                importCameras = preset.importCameras;
                importLights = preset.importLights;
                preserveHierarchy = preset.preserveHierarchy;

                EditorUtility.DisplayDialog("Preset Loaded", "Import settings preset has been loaded.", "OK");
            }
        }

        [System.Serializable]
        private class ImportPreset
        {
            public bool generateLightmapUVs = true;
            public bool readWriteEnabled = true;
            public bool optimizeMesh = true;
            public int meshCompression = 0;
            public bool importBlendShapes = true;
            public bool importVisibility = true;
            public bool importCameras = true;
            public bool importLights = true;
            public bool preserveHierarchy = true;
        }
        #endregion

        #region Material Tools
        private void DrawMaterialTools()
        {
            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField("Material Application", subHeaderStyle);
            EditorGUILayout.HelpBox("Select objects in the scene and apply materials to them quickly.", MessageType.Info);

            EditorGUILayout.Space();

            // Material selection
            EditorGUILayout.BeginHorizontal();
            selectedMaterial = (Material)EditorGUILayout.ObjectField("Material:", selectedMaterial, typeof(Material), false);
            EditorGUILayout.EndHorizontal();

            // Apply to children option
            applyMaterialToChildren = EditorGUILayout.Toggle("Apply to Children", applyMaterialToChildren);

            // Apply button
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = selectedMaterial != null && selectedObjects != null && selectedObjects.Length > 0;
            if (GUILayout.Button(new GUIContent(" Apply Material", EditorGUIUtility.IconContent("Material Icon").image), buttonStyle, GUILayout.Height(30)))
            {
                ApplyMaterialToSelected();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // Recent materials
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Recent Materials", subHeaderStyle);

            if (recentMaterials.Count > 0)
            {
                materialScrollPosition = EditorGUILayout.BeginScrollView(materialScrollPosition, GUILayout.Height(150));
                for (int i = 0; i < recentMaterials.Count; i++)
                {
                    if (recentMaterials[i] == null)
                        continue;

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label(recentMaterials[i].name, GUILayout.MaxWidth(150));
                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                    {
                        selectedMaterial = recentMaterials[i];
                    }
                    if (GUILayout.Button("Apply", GUILayout.Width(60)))
                    {
                        selectedMaterial = recentMaterials[i];
                        ApplyMaterialToSelected();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.HelpBox("No recent materials. Apply a material to add it to this list.", MessageType.Info);
            }

            // Material extraction
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Material Extraction", subHeaderStyle);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent(" Extract from Selection", EditorGUIUtility.IconContent("d_ExtractToFolder").image), buttonStyle))
            {
                ExtractMaterialFromSelection();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void ApplyMaterialToSelected()
        {
            if (selectedMaterial == null || selectedObjects == null || selectedObjects.Length == 0)
                return;

            Undo.RecordObjects(selectedObjects, "Apply Material");

            // Add to recent materials
            if (!recentMaterials.Contains(selectedMaterial))
            {
                recentMaterials.Insert(0, selectedMaterial);
                if (recentMaterials.Count > maxRecentMaterials)
                {
                    recentMaterials.RemoveAt(recentMaterials.Count - 1);
                }
            }
            else
            {
                // Move to top
                recentMaterials.Remove(selectedMaterial);
                recentMaterials.Insert(0, selectedMaterial);
            }

            foreach (GameObject obj in selectedObjects)
            {
                // Apply to this object
                Renderer renderer = obj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = selectedMaterial;
                }

                // Apply to children if option is enabled
                if (applyMaterialToChildren)
                {
                    Renderer[] childRenderers = obj.GetComponentsInChildren<Renderer>(true);
                    foreach (Renderer childRenderer in childRenderers)
                    {
                        if (childRenderer.gameObject != obj) // Skip the parent object as it's already processed
                        {
                            Undo.RecordObject(childRenderer, "Apply Material");
                            childRenderer.sharedMaterial = selectedMaterial;
                        }
                    }
                }
            }
        }

        private void ExtractMaterialFromSelection()
        {
            if (selectedObjects == null || selectedObjects.Length == 0)
                return;

            foreach (GameObject obj in selectedObjects)
            {
                Renderer renderer = obj.GetComponent<Renderer>();
                if (renderer != null && renderer.sharedMaterial != null)
                {
                    selectedMaterial = renderer.sharedMaterial;

                    // Add to recent materials
                    if (!recentMaterials.Contains(selectedMaterial))
                    {
                        recentMaterials.Insert(0, selectedMaterial);
                        if (recentMaterials.Count > maxRecentMaterials)
                        {
                            recentMaterials.RemoveAt(recentMaterials.Count - 1);
                        }
                    }
                    else
                    {
                        // Move to top
                        recentMaterials.Remove(selectedMaterial);
                        recentMaterials.Insert(0, selectedMaterial);
                    }

                    EditorGUIUtility.PingObject(selectedMaterial);
                    break;
                }
            }
        }
        #endregion

        #region Layer Tools
        private void DrawLayerTools()
        {
            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField("Layer Management", subHeaderStyle);
            EditorGUILayout.HelpBox("Quickly change the layers of selected objects.", MessageType.Info);

            EditorGUILayout.Space();

            // Layer selection
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Layer:", GUILayout.Width(50));
            selectedLayer = EditorGUILayout.Popup(selectedLayer, layerNames);
            EditorGUILayout.EndHorizontal();

            // Apply to children option
            applyLayerToChildren = EditorGUILayout.Toggle("Apply to Children", applyLayerToChildren);

            // Apply button
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = selectedObjects != null && selectedObjects.Length > 0;
            if (GUILayout.Button(new GUIContent(" Apply Layer", EditorGUIUtility.IconContent("FilterByLabel").image), buttonStyle, GUILayout.Height(30)))
            {
                ApplyLayerToSelected();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // Layer utilities
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Layer Utilities", subHeaderStyle);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent(" Get Layer from Selection", EditorGUIUtility.IconContent("d_pick").image), buttonStyle))
            {
                GetLayerFromSelection();
            }
            EditorGUILayout.EndHorizontal();

            // Layer visibility
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Layer Visibility", subHeaderStyle);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Hide Other Layers", buttonStyle))
            {
                HideOtherLayers();
            }
            if (GUILayout.Button("Show All Layers", buttonStyle))
            {
                ShowAllLayers();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void ApplyLayerToSelected()
        {
            if (selectedObjects == null || selectedObjects.Length == 0)
                return;

            Undo.RecordObjects(selectedObjects, "Apply Layer");

            foreach (GameObject obj in selectedObjects)
            {
                // Apply to this object
                obj.layer = selectedLayer;

                // Apply to children if option is enabled
                if (applyLayerToChildren)
                {
                    Transform[] childTransforms = obj.GetComponentsInChildren<Transform>(true);
                    foreach (Transform childTransform in childTransforms)
                    {
                        if (childTransform.gameObject != obj) // Skip the parent object as it's already processed
                        {
                            Undo.RecordObject(childTransform.gameObject, "Apply Layer");
                            childTransform.gameObject.layer = selectedLayer;
                        }
                    }
                }
            }
        }

        private void GetLayerFromSelection()
        {
            if (selectedObjects == null || selectedObjects.Length == 0)
                return;

            // Get the layer from the first selected object
            selectedLayer = selectedObjects[0].layer;
        }

        private void HideOtherLayers()
        {
            if (selectedObjects == null || selectedObjects.Length == 0)
                return;

            // Get the layer from the first selected object
            int visibleLayer = selectedObjects[0].layer;

            // Create a layer mask with only this layer visible
            int layerMask = 1 << visibleLayer;

            // Apply to all scene views
            foreach (SceneView sceneView in SceneView.sceneViews)
            {
                sceneView.drawGizmos = false;
                // Note: Unity doesn't provide a direct API to hide layers in the scene view
                // This is a workaround that disables gizmos, but doesn't actually hide other layers
                // A full implementation would require custom rendering
            }

            EditorUtility.DisplayDialog("Layer Visibility", 
                "Unity doesn't provide a direct API to hide specific layers in the scene view. " +
                "Gizmos have been disabled to reduce visual clutter. " +
                "For better layer management, consider using the Layer Visibility package from the Asset Store.", 
                "OK");
        }

        private void ShowAllLayers()
        {
            // Re-enable gizmos in all scene views
            foreach (SceneView sceneView in SceneView.sceneViews)
            {
                sceneView.drawGizmos = true;
            }
        }
        #endregion

        #region Shortcuts
        private void DrawShortcuts()
        {
            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField("Keyboard Shortcuts", subHeaderStyle);
            EditorGUILayout.HelpBox("Configure keyboard shortcuts for common operations. Hold Ctrl/Cmd for shortcuts marked with (Mod).", MessageType.Info);

            EditorGUILayout.Space();

            foreach (var shortcut in shortcuts.Keys.ToList())
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(shortcut + (shortcutModifiers[shortcut] ? " (Mod)" : ""), GUILayout.Width(150));
                KeyCode newKey = (KeyCode)EditorGUILayout.EnumPopup(shortcuts[shortcut]);
                if (newKey != shortcuts[shortcut])
                {
                    shortcuts[shortcut] = newKey;
                }
                bool newModifier = EditorGUILayout.Toggle(shortcutModifiers[shortcut], GUILayout.Width(20));
                if (newModifier != shortcutModifiers[shortcut])
                {
                    shortcutModifiers[shortcut] = newModifier;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset to Defaults", buttonStyle))
            {
                ResetShortcutsToDefaults();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void ResetShortcutsToDefaults()
        {
            shortcuts["Duplicate"] = KeyCode.D;
            shortcuts["Delete"] = KeyCode.Delete;
            shortcuts["Focus"] = KeyCode.F;
            shortcuts["Toggle Static"] = KeyCode.S;
            shortcuts["Align to Ground"] = KeyCode.G;
            shortcuts["Reset Transform"] = KeyCode.R;
            shortcuts["Group Selected"] = KeyCode.N;
            shortcuts["Snap to Grid"] = KeyCode.T;
            shortcuts["Distribute X"] = KeyCode.X;
            shortcuts["Distribute Y"] = KeyCode.Y;
            shortcuts["Distribute Z"] = KeyCode.Z;
            shortcuts["Place Prefab"] = KeyCode.P;
            shortcuts["Apply Material"] = KeyCode.M;
            shortcuts["Apply Layer"] = KeyCode.L;

            shortcutModifiers["Duplicate"] = true;
            shortcutModifiers["Delete"] = false;
            shortcutModifiers["Focus"] = false;
            shortcutModifiers["Toggle Static"] = true;
            shortcutModifiers["Align to Ground"] = true;
            shortcutModifiers["Reset Transform"] = true;
            shortcutModifiers["Group Selected"] = true;
            shortcutModifiers["Snap to Grid"] = true;
            shortcutModifiers["Distribute X"] = true;
            shortcutModifiers["Distribute Y"] = true;
            shortcutModifiers["Distribute Z"] = true;
            shortcutModifiers["Place Prefab"] = true;
            shortcutModifiers["Apply Material"] = true;
            shortcutModifiers["Apply Layer"] = true;
        }

        private void ProcessShortcuts()
        {
            Event e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                bool modifierPressed = e.control || e.command;

                foreach (var shortcut in shortcuts)
                {
                    if (e.keyCode == shortcut.Value && modifierPressed == shortcutModifiers[shortcut.Key])
                    {
                        ExecuteShortcut(shortcut.Key);
                        e.Use();
                        break;
                    }
                }
            }
        }

        private void ExecuteShortcut(string shortcutName)
        {
            switch (shortcutName)
            {
                case "Duplicate":
                    DuplicateSelected();
                    break;
                case "Delete":
                    DeleteSelected();
                    break;
                case "Focus":
                    FocusOnSelected();
                    break;
                case "Toggle Static":
                    ToggleStaticFlags();
                    break;
                case "Align to Ground":
                    AlignSelectedToSurface();
                    break;
                case "Reset Transform":
                    ResetAllTransforms();
                    break;
                case "Group Selected":
                    GroupSelectedObjects();
                    break;
                case "Snap to Grid":
                    SnapSelectedToGrid();
                    break;
                case "Distribute X":
                    DistributeSelected(0);
                    break;
                case "Distribute Y":
                    DistributeSelected(1);
                    break;
                case "Distribute Z":
                    DistributeSelected(2);
                    break;
                case "Place Prefab":
                    if (selectedPrefab != null)
                        PlacePrefab();
                    break;
                case "Apply Material":
                    if (selectedMaterial != null && selectedObjects != null && selectedObjects.Length > 0)
                        ApplyMaterialToSelected();
                    break;
                case "Apply Layer":
                    if (selectedObjects != null && selectedObjects.Length > 0)
                        ApplyLayerToSelected();
                    break;
            }
        }

        private void DuplicateSelected()
        {
            if (selectedObjects == null || selectedObjects.Length == 0)
                return;

            List<GameObject> duplicates = new List<GameObject>();

            foreach (GameObject obj in selectedObjects)
            {
                GameObject duplicate = Instantiate(obj, obj.transform.parent);
                duplicate.name = obj.name + " (Copy)";
                Undo.RegisterCreatedObjectUndo(duplicate, "Duplicate Objects");
                duplicates.Add(duplicate);
            }

            Selection.objects = duplicates.ToArray();
        }

        private void DeleteSelected()
        {
            if (selectedObjects == null || selectedObjects.Length == 0)
                return;

            foreach (GameObject obj in selectedObjects)
            {
                Undo.DestroyObjectImmediate(obj);
            }
        }

        private void FocusOnSelected()
        {
            if (selectedObjects == null || selectedObjects.Length == 0 || SceneView.lastActiveSceneView == null)
                return;

            // Calculate bounds of all selected objects
            Bounds bounds = new Bounds();
            bool boundsInitialized = false;

            foreach (GameObject obj in selectedObjects)
            {
                Renderer renderer = obj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    if (!boundsInitialized)
                    {
                        bounds = renderer.bounds;
                        boundsInitialized = true;
                    }
                    else
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                }
                else
                {
                    // If no renderer, use position
                    if (!boundsInitialized)
                    {
                        bounds = new Bounds(obj.transform.position, Vector3.one);
                        boundsInitialized = true;
                    }
                    else
                    {
                        bounds.Encapsulate(obj.transform.position);
                    }
                }
            }

            // Focus the scene view on the bounds
            if (boundsInitialized)
            {
                SceneView.lastActiveSceneView.Frame(bounds, false);
            }
        }
        #endregion

        #region Scene GUI
        private void OnSceneGUI(SceneView sceneView)
        {
            // Draw grid if snap to grid is enabled
            if (snapToGrid && gridSize > 0)
            {
                DrawGrid(sceneView);
            }

            // Handle scene view shortcuts
            ProcessSceneViewShortcuts(sceneView);
        }

        private void DrawGrid(SceneView sceneView)
        {
            // Only draw grid when the tool is active
            if (!sceneView.hasFocus)
                return;

            // Get scene view camera position
            Vector3 cameraPosition = sceneView.camera.transform.position;

            // Calculate grid center (round to nearest grid cell)
            Vector3 gridCenter = new Vector3(
                Mathf.Round(cameraPosition.x / gridSize) * gridSize,
                0,
                Mathf.Round(cameraPosition.z / gridSize) * gridSize
            );

            // Draw grid lines
            int gridLines = 20; // Number of grid lines in each direction
            float gridExtent = gridSize * gridLines / 2;

            Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.2f);

            // Draw X lines
            for (int i = -gridLines / 2; i <= gridLines / 2; i++)
            {
                Vector3 start = gridCenter + new Vector3(i * gridSize, 0, -gridExtent);
                Vector3 end = gridCenter + new Vector3(i * gridSize, 0, gridExtent);
                Handles.DrawLine(start, end);
            }

            // Draw Z lines
            for (int i = -gridLines / 2; i <= gridLines / 2; i++)
            {
                Vector3 start = gridCenter + new Vector3(-gridExtent, 0, i * gridSize);
                Vector3 end = gridCenter + new Vector3(gridExtent, 0, i * gridSize);
                Handles.DrawLine(start, end);
            }

            // Draw center lines with different color
            Handles.color = new Color(0.8f, 0.8f, 0.8f, 0.4f);
            Handles.DrawLine(gridCenter + new Vector3(0, 0, -gridExtent), gridCenter + new Vector3(0, 0, gridExtent));
            Handles.DrawLine(gridCenter + new Vector3(-gridExtent, 0, 0), gridCenter + new Vector3(gridExtent, 0, 0));
        }

        private void ProcessSceneViewShortcuts(SceneView sceneView)
        {
            Event e = Event.current;
            if (e.type == EventType.KeyDown && sceneView.hasFocus)
            {
                bool modifierPressed = e.control || e.command;

                foreach (var shortcut in shortcuts)
                {
                    if (e.keyCode == shortcut.Value && modifierPressed == shortcutModifiers[shortcut.Key])
                    {
                        ExecuteShortcut(shortcut.Key);
                        e.Use();
                        sceneView.Repaint();
                        break;
                    }
                }
            }
        }
        #endregion
    }

    // Helper window for detailed static flags
    public class StaticFlagsWindow : EditorWindow
    {
        private GameObject[] selectedObjects;
        private Dictionary<StaticEditorFlags, bool?> staticFlags = new Dictionary<StaticEditorFlags, bool?>();
        private Vector2 scrollPosition;

        public static void ShowWindow(GameObject[] objects)
        {
            StaticFlagsWindow window = GetWindow<StaticFlagsWindow>("Static Flags");
            window.selectedObjects = objects;
            window.InitializeFlags();
            window.minSize = new Vector2(300, 400);
            window.Show();
        }

        private void InitializeFlags()
        {
            // Initialize with all possible static flags
            foreach (StaticEditorFlags flag in System.Enum.GetValues(typeof(StaticEditorFlags)))
            {
                staticFlags[flag] = null;
            }

            // Determine current state of flags
            if (selectedObjects != null && selectedObjects.Length > 0)
            {
                foreach (StaticEditorFlags flag in System.Enum.GetValues(typeof(StaticEditorFlags)))
                {
                    bool allHaveFlag = true;
                    bool noneHaveFlag = true;

                    foreach (GameObject obj in selectedObjects)
                    {
                        bool hasFlag = GameObjectUtility.GetStaticEditorFlags(obj).HasFlag(flag);
                        if (hasFlag)
                            noneHaveFlag = false;
                        else
                            allHaveFlag = false;
                    }

                    if (allHaveFlag)
                        staticFlags[flag] = true;
                    else if (noneHaveFlag)
                        staticFlags[flag] = false;
                    else
                        staticFlags[flag] = null; // Mixed state
                }
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Static Flags", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Selected Objects: {selectedObjects.Length}");

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All"))
            {
                foreach (StaticEditorFlags flag in System.Enum.GetValues(typeof(StaticEditorFlags)))
                {
                    staticFlags[flag] = true;
                }
            }
            if (GUILayout.Button("Select None"))
            {
                foreach (StaticEditorFlags flag in System.Enum.GetValues(typeof(StaticEditorFlags)))
                {
                    staticFlags[flag] = false;
                }
            }
            if (GUILayout.Button("Invert"))
            {
                foreach (StaticEditorFlags flag in System.Enum.GetValues(typeof(StaticEditorFlags)))
                {
                    if (staticFlags[flag].HasValue)
                        staticFlags[flag] = !staticFlags[flag].Value;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (StaticEditorFlags flag in System.Enum.GetValues(typeof(StaticEditorFlags)))
            {
                EditorGUI.BeginChangeCheck();

                // For mixed state, use a special toggle
                bool? currentValue = staticFlags[flag];
                bool newValue;

                if (!currentValue.HasValue)
                {
                    // Mixed state
                    EditorGUI.showMixedValue = true;
                    newValue = EditorGUILayout.Toggle(flag.ToString(), false);
                    EditorGUI.showMixedValue = false;
                }
                else
                {
                    newValue = EditorGUILayout.Toggle(flag.ToString(), currentValue.Value);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    staticFlags[flag] = newValue;
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            if (GUILayout.Button("Apply", GUILayout.Height(30)))
            {
                ApplyStaticFlags();
                Close();
            }
        }

        private void ApplyStaticFlags()
        {
            if (selectedObjects == null || selectedObjects.Length == 0)
                return;

            Undo.RecordObjects(selectedObjects, "Set Static Flags");

            foreach (GameObject obj in selectedObjects)
            {
                StaticEditorFlags currentFlags = GameObjectUtility.GetStaticEditorFlags(obj);

                foreach (var flagPair in staticFlags)
                {
                    if (flagPair.Value.HasValue)
                    {
                        if (flagPair.Value.Value)
                        {
                            // Set flag
                            currentFlags |= flagPair.Key;
                        }
                        else
                        {
                            // Clear flag
                            currentFlags &= ~flagPair.Key;
                        }
                    }
                }

                GameObjectUtility.SetStaticEditorFlags(obj, currentFlags);
            }
        }
    }
}
