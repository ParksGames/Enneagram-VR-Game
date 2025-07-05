using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public class ImageAssetManager : EditorWindow
{
    private enum Tab { UsedImages, UnusedImages, Rename }
    private Tab currentTab = Tab.UsedImages;

    private DefaultAsset prefabFolder;
    private DefaultAsset artFolder;
    private DefaultAsset moveTargetFolder;
    private List<DefaultAsset> excludeFolders = new List<DefaultAsset>();

    private Vector2 scrollPos;
    private string statusMessage = "Ready";

    private List<AssetInfo> foundImages = new List<AssetInfo>();
    private Dictionary<Object, string> renameMap = new Dictionary<Object, string>();

    [MenuItem("CT/Image Asset Manager")]
    public static void ShowWindow()
    {
        GetWindow<ImageAssetManager>("Image Tools");
    }

    void OnGUI()
    {
        currentTab = (Tab)GUILayout.Toolbar((int)currentTab, new string[] { "Used Images", "Unused Images", "Rename" });
        GUILayout.Space(10);

        switch (currentTab)
        {
            case Tab.UsedImages:
                DrawUsedImagesTab();
                break;
            case Tab.UnusedImages:
                DrawUnusedImagesTab();
                break;
            case Tab.Rename:
                DrawRenameTab();
                break;
        }

        GUILayout.Space(10);
        EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
    }

    void DrawUsedImagesTab()
    {
        prefabFolder = (DefaultAsset)EditorGUILayout.ObjectField("Prefab Folder", prefabFolder, typeof(DefaultAsset), false);

        GUILayout.Label("Exclude Folders (optional):", EditorStyles.boldLabel);
        int removeIndex = -1;
        for (int i = 0; i < excludeFolders.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            excludeFolders[i] = (DefaultAsset)EditorGUILayout.ObjectField(excludeFolders[i], typeof(DefaultAsset), false);
            if (GUILayout.Button("X", GUILayout.Width(20))) removeIndex = i;
            EditorGUILayout.EndHorizontal();
        }
        if (removeIndex >= 0) excludeFolders.RemoveAt(removeIndex);
        if (GUILayout.Button("Add Exclude Folder")) excludeFolders.Add(null);

        if (GUILayout.Button("Scan Used Images"))
        {
            ScanUsedImagesInPrefabs();
        }

        moveTargetFolder = (DefaultAsset)EditorGUILayout.ObjectField("Move To Folder", moveTargetFolder, typeof(DefaultAsset), false);

        if (GUILayout.Button("Move All Used Images") && moveTargetFolder != null)
        {
            MoveAssetsWithUndo(foundImages, moveTargetFolder);
        }

        DrawAssetList(true);
    }

    void DrawUnusedImagesTab()
    {
        artFolder = (DefaultAsset)EditorGUILayout.ObjectField("Art Folder", artFolder, typeof(DefaultAsset), false);

        if (GUILayout.Button("Find Unused Images"))
        {
            ScanUnusedImages();
        }

        moveTargetFolder = (DefaultAsset)EditorGUILayout.ObjectField("Move To Folder", moveTargetFolder, typeof(DefaultAsset), false);

        if (GUILayout.Button("Move All Unused Images") && moveTargetFolder != null)
        {
            MoveAssetsWithUndo(foundImages, moveTargetFolder);
        }

        DrawAssetList(false);
    }

    void DrawRenameTab()
    {
        if (GUILayout.Button("Load All Assets in Folder"))
        {
            LoadAssetsForRenaming();
        }

        if (renameMap.Count > 0)
        {
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            foreach (var kvp in renameMap.ToList())
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(kvp.Key, typeof(Object), false);
                renameMap[kvp.Key] = EditorGUILayout.TextField(renameMap[kvp.Key]);
                EditorGUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            if (GUILayout.Button("Apply Renames"))
            {
                ApplyRenames();
            }
        }
    }

    void ScanUsedImagesInPrefabs()
    {
        foundImages.Clear();
        string path = AssetDatabase.GetAssetPath(prefabFolder);
        var prefabGUIDs = AssetDatabase.FindAssets("t:Prefab", new[] { path });

        Dictionary<string, List<string>> imageToPrefabMap = new Dictionary<string, List<string>>();

        foreach (var guid in prefabGUIDs)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            string prefabName = Path.GetFileNameWithoutExtension(prefabPath);
            var dependencies = AssetDatabase.GetDependencies(prefabPath, true);

            foreach (var dep in dependencies)
            {
                if (!IsImage(dep)) continue;

                bool excluded = excludeFolders.Any(exclude =>
                {
                    if (exclude == null) return false;
                    string exPath = AssetDatabase.GetAssetPath(exclude);
                    return dep.StartsWith(exPath);
                });

                if (excluded) continue;

                if (!imageToPrefabMap.ContainsKey(dep))
                    imageToPrefabMap[dep] = new List<string>();
                imageToPrefabMap[dep].Add(prefabPath);
            }
        }

        foreach (var kvp in imageToPrefabMap)
        {
            var info = CreateAssetInfo(kvp.Key, kvp.Value);
            foundImages.Add(info);
        }

        statusMessage = $"Found {foundImages.Count} used image(s).";
    }

    void ScanUnusedImages()
    {
        foundImages.Clear();
        string path = AssetDatabase.GetAssetPath(artFolder);
        var imageGUIDs = AssetDatabase.FindAssets("t:Texture2D", new[] { path });

        var allRefs = new HashSet<string>();
        foreach (var asset in AssetDatabase.GetAllAssetPaths())
        {
            if (asset.EndsWith(".prefab") || asset.EndsWith(".unity") || asset.EndsWith(".mat") || asset.EndsWith(".asset"))
            {
                foreach (var dep in AssetDatabase.GetDependencies(asset, true))
                {
                    allRefs.Add(dep);
                }
            }
        }

        foreach (var guid in imageGUIDs)
        {
            var imgPath = AssetDatabase.GUIDToAssetPath(guid);
            if (!allRefs.Contains(imgPath))
                foundImages.Add(CreateAssetInfo(imgPath, null));
        }

        statusMessage = $"Found {foundImages.Count} unused image(s).";
    }

    void MoveAssetsWithUndo(List<AssetInfo> assets, DefaultAsset targetFolder)
    {
        string targetPath = AssetDatabase.GetAssetPath(targetFolder);
        int moved = 0;

        foreach (var info in assets)
        {
            string name = Path.GetFileName(info.path);
            string newPath = Path.Combine(targetPath, name);
            string result = AssetDatabase.MoveAsset(info.path, newPath);
            if (string.IsNullOrEmpty(result))
                moved++;
            else
                Debug.LogWarning($"Move failed: {result}");
        }

        AssetDatabase.Refresh();
        statusMessage = $"Moved {moved} asset(s).";
    }

    void LoadAssetsForRenaming()
    {
        renameMap.Clear();
        string path = artFolder ? AssetDatabase.GetAssetPath(artFolder) : "Assets";
        var imageGUIDs = AssetDatabase.FindAssets("t:Texture2D", new[] { path });

        foreach (var guid in imageGUIDs)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            renameMap[obj] = obj.name;
        }

        statusMessage = $"Loaded {renameMap.Count} asset(s) for renaming.";
    }

    void ApplyRenames()
    {
        int renamed = 0;

        foreach (var kvp in renameMap)
        {
            if (kvp.Key.name != kvp.Value && !string.IsNullOrEmpty(kvp.Value))
            {
                string path = AssetDatabase.GetAssetPath(kvp.Key);
                string error = AssetDatabase.RenameAsset(path, kvp.Value);
                if (string.IsNullOrEmpty(error))
                    renamed++;
            }
        }

        AssetDatabase.SaveAssets();
        statusMessage = $"Renamed {renamed} asset(s).";
    }

    void DrawAssetList(bool showUsedBy)
    {
        scrollPos = GUILayout.BeginScrollView(scrollPos);
        foreach (var asset in foundImages)
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label(asset.path, EditorStyles.boldLabel);

            GUILayout.Label($"📐 {asset.width}x{asset.height} | {(asset.isPowerOfTwo ? "Power of 2" : "Non-Po2")}", EditorStyles.miniLabel);

            if (GUILayout.Button("Select Image", GUILayout.Width(100)))
            {
                var obj = AssetDatabase.LoadAssetAtPath<Object>(asset.path);
                Selection.activeObject = obj;
                EditorGUIUtility.PingObject(obj);
            }

            if (showUsedBy && asset.usedBy != null)
            {
                GUILayout.Label("Used by:");
                foreach (var user in asset.usedBy)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(10);
                    if (GUILayout.Button(Path.GetFileName(user), EditorStyles.miniButtonLeft))
                    {
                        var obj = AssetDatabase.LoadAssetAtPath<Object>(user);
                        Selection.activeObject = obj;
                        EditorGUIUtility.PingObject(obj);
                    }
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndVertical();
        }
        GUILayout.EndScrollView();
    }

    bool IsImage(string path)
    {
        return path.EndsWith(".png") || path.EndsWith(".jpg") || path.EndsWith(".tga") || path.EndsWith(".psd");
    }

    AssetInfo CreateAssetInfo(string imagePath, List<string> usedBy)
    {
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath);
        int width = 0, height = 0;
        bool po2 = false;

        if (tex != null)
        {
            width = tex.width;
            height = tex.height;
            po2 = IsPowerOfTwo(width) && IsPowerOfTwo(height);
        }

        return new AssetInfo
        {
            path = imagePath,
            usedBy = usedBy,
            width = width,
            height = height,
            isPowerOfTwo = po2
        };
    }

    bool IsPowerOfTwo(int n)
    {
        return (n != 0) && ((n & (n - 1)) == 0);
    }

    private class AssetInfo
    {
        public string path;
        public List<string> usedBy;
        public int width;
        public int height;
        public bool isPowerOfTwo;
    }
}
