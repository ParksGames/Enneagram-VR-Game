using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class AssetFolderCheckerWindow : EditorWindow
{
    [System.Serializable]
    public class PlacementRule
    {
        public string[] extensions; // e.g., {".fbx", ".obj"}
        public string parentFolder; // e.g., "Models"
        public string[] excludeFolders = new string[0]; // Folders to exclude
    }

    private List<PlacementRule> rules = new List<PlacementRule>()
    {
        new PlacementRule{ extensions=new[]{"fbx"}, parentFolder="Models" },
        new PlacementRule{ extensions=new[]{"prefab"}, parentFolder="Prefabs" },
        new PlacementRule{ extensions=new[]{"mat"}, parentFolder="Materials" },
        new PlacementRule{ extensions=new[]{"png", "jpg", "jpeg"}, parentFolder="Textures" },
    };

    private DefaultAsset folderToSearch;
    private Vector2 scrollRules, scrollResults;
    private bool showOnlyWrong;

    private class AssetResult
    {
        public string path;
        public string fileName;
        public string ext;
        public string parent;
        public bool misplaced;
    }
    private List<AssetResult> foundAssets = new();

    [MenuItem("CT/Asset Folder Checker")]
    public static void OpenWindow()
    {
        GetWindow<AssetFolderCheckerWindow>("Asset Folder Checker");
    }

    void OnGUI()
    {
        GUILayout.Label("Asset Folder Checker", EditorStyles.boldLabel);

        // Folder Picker
        GUILayout.BeginHorizontal();
        GUILayout.Label("Root Folder:", GUILayout.Width(80));
        folderToSearch = (DefaultAsset)EditorGUILayout.ObjectField(folderToSearch, typeof(DefaultAsset), false);
        if (GUILayout.Button("Scan", GUILayout.Width(80)))
            ScanAssets();
        GUILayout.EndHorizontal();

        EditorGUILayout.Space(6);

        // --- Rules Table ---
        GUILayout.Label("Placement Rules", EditorStyles.boldLabel);
        scrollRules = GUILayout.BeginScrollView(scrollRules, GUILayout.Height(110));
        for (int i = 0; i < rules.Count; i++)
        {
            var rule = rules[i];
            GUILayout.BeginHorizontal();

            GUILayout.Label("Extensions:", GUILayout.Width(70));
            string joined = string.Join(", ", rule.extensions);
            string newJoined = GUILayout.TextField(joined, GUILayout.Width(120));
            if (joined != newJoined)
                rule.extensions = newJoined.Replace(" ", "").Split(',');

            GUILayout.Label("Parent Folder:", GUILayout.Width(90));
            rule.parentFolder = GUILayout.TextField(rule.parentFolder, GUILayout.Width(120));

            GUILayout.Label("Exclude Folders:", GUILayout.Width(90));
            string excludeJoined = string.Join(", ", rule.excludeFolders);
            string newExcludeJoined = GUILayout.TextField(excludeJoined, GUILayout.Width(120));
            if (excludeJoined != newExcludeJoined)
                rule.excludeFolders = newExcludeJoined.Replace(" ", "").Split(',');

            if (GUILayout.Button("❌", GUILayout.Width(22)))
            {
                rules.RemoveAt(i);
                break;
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();

        if (GUILayout.Button("Add Rule"))
            rules.Add(new PlacementRule { extensions = new string[] { "ext" }, parentFolder = "FolderName", excludeFolders = new string[0] });

        EditorGUILayout.Space(8);

        // --- Results ---
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Results: {foundAssets.Count} files ({foundAssets.Count(a => a.misplaced)} wrong)", EditorStyles.boldLabel);
        showOnlyWrong = EditorGUILayout.ToggleLeft("Show Only Wrong", showOnlyWrong);
        GUILayout.EndHorizontal();

        scrollResults = GUILayout.BeginScrollView(scrollResults);
        foreach (var asset in foundAssets)
        {
            if (showOnlyWrong && !asset.misplaced)
                continue;
                
            Color defaultColor = GUI.backgroundColor;
            GUI.backgroundColor = asset.misplaced ? new Color(1f, 0.6f, 0.6f) : Color.white;

            GUILayout.BeginHorizontal("box");
            GUILayout.Label(GetIconForExt(asset.ext), GUILayout.Width(22), GUILayout.Height(22));
            GUILayout.Label(asset.fileName, GUILayout.Width(120));
            GUILayout.Label(asset.parent, GUILayout.Width(90));
            GUILayout.Label(asset.path, GUILayout.MinWidth(220));
            if (asset.misplaced)
                GUILayout.Label("❗", GUILayout.Width(18));

            if (GUILayout.Button("Ping", GUILayout.Width(40)))
            {
                var obj = AssetDatabase.LoadAssetAtPath<Object>(asset.path);
                EditorGUIUtility.PingObject(obj);
                Selection.activeObject = obj;
            }
            GUILayout.EndHorizontal();
            GUI.backgroundColor = defaultColor;
        }
        GUILayout.EndScrollView();

        GUILayout.Space(6);
        EditorGUILayout.HelpBox("Edit/add rules above. 'Extensions' is a comma-separated list (without dot), e.g. 'fbx, obj'.\nScan checks parent folder for each rule. 'Exclude Folders' is a comma-separated list of folders to ignore.", MessageType.Info);
    }

    void ScanAssets()
    {
        foundAssets.Clear();
        string rootPath = folderToSearch ? AssetDatabase.GetAssetPath(folderToSearch) : "Assets";

        var allFiles = Directory.GetFiles(rootPath, "*.*", SearchOption.AllDirectories)
            .Where(f => !f.EndsWith(".meta")).ToList();

        foreach (var path in allFiles)
        {
            string ext = Path.GetExtension(path).TrimStart('.').ToLower();
            string parent = Path.GetFileName(Path.GetDirectoryName(path));
            string grandParent = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(path)));

            bool matchedRule = false;
            bool misplaced = false;
            foreach (var rule in rules)
            {
                if (rule.extensions.Any(e => e.Trim().ToLower() == ext))
                {
                    // Check if file is in excluded folder
                    bool inExcludedFolder = rule.excludeFolders.Any(ef => 
                        !string.IsNullOrEmpty(ef) && path.Contains(Path.DirectorySeparatorChar + ef + Path.DirectorySeparatorChar));
                    
                    if (inExcludedFolder)
                        continue;

                    matchedRule = true;
                    if (!parent.Equals(rule.parentFolder, System.StringComparison.OrdinalIgnoreCase) && 
                        !grandParent.Equals(rule.parentFolder, System.StringComparison.OrdinalIgnoreCase))
                        misplaced = true;
                    break;
                }
            }

            if (matchedRule)
            {
                foundAssets.Add(new AssetResult
                {
                    path = path,
                    fileName = Path.GetFileName(path),
                    ext = ext,
                    parent = parent,
                    misplaced = misplaced,
                });
            }
        }
    }

    Texture GetIconForExt(string ext)
    {
        switch (ext)
        {
            case "fbx": return EditorGUIUtility.IconContent("PrefabModel Icon").image;
            case "prefab": return EditorGUIUtility.IconContent("Prefab Icon").image;
            case "png":
            case "jpg":
            case "jpeg": return EditorGUIUtility.IconContent("Texture2D Icon").image;
            case "mat": return EditorGUIUtility.IconContent("Material Icon").image;
            default: return EditorGUIUtility.IconContent("DefaultAsset Icon").image;
        }
    }
}