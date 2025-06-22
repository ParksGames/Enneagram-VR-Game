using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[InitializeOnLoad]
public class ColoredFolders : AssetPostprocessor
{
    // Edit your folder names and colors here
    static Dictionary<string, Color> folderColors = new Dictionary<string, Color>
    {
        // Art Related
        { "Art",        new Color(0.0f, 0.5f, 1.0f) },     // Deeper Blue
        { "Animations", new Color(0.0f, 0.7f, 1.0f) },     // Light Blue
        { "Materials",  new Color(0.0f, 0.4f, 0.8f) },     // Dark Blue
        { "Textures",   new Color(0.0f, 0.8f, 0.8f) },     // Teal

        // Audio Related
        { "Audio",      new Color(0.6f, 0.0f, 1.0f) },     // Rich Purple
        { "Music",      new Color(0.5f, 0.0f, 0.8f) },     // Dark Purple
        { "SFX",        new Color(0.7f, 0.0f, 0.9f) },     // Light Purple

        // Data & Content
        { "Data",       new Color(1.0f, 0.8f, 0.0f) },     // Warmer Yellow
        { "Dialogues",  new Color(1.0f, 0.7f, 0.2f) },     // Orange Yellow
        { "GameFlow",   new Color(0.9f, 0.6f, 0.1f) },     // Dark Yellow

        // Game Structure
        { "Prefabs",    new Color(0.0f, 0.8f, 0.4f) },     // Forest Green
        { "Scripts",    new Color(1.0f, 0.2f, 0.2f) },     // Bright Red
        { "Story",      new Color(0.9f, 0.1f, 0.8f) },     // Pink
        { "Scenes",     new Color(1.0f, 0.5f, 0.0f) },     // Deep Orange
        { "Settings",   new Color(0.7f, 0.7f, 0.7f) }      // Medium Grey
    };

    static ColoredFolders()
    {
        EditorApplication.projectWindowItemOnGUI += HandleProjectWindowItemOnGUI;
    }

    static void HandleProjectWindowItemOnGUI(string guid, Rect rect)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        if (!AssetDatabase.IsValidFolder(path)) return;

        string folderName = System.IO.Path.GetFileName(path);
        if (!folderColors.ContainsKey(folderName)) return;

        Color color = folderColors[folderName];
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, rect.height), color * new Color(1,1,1,0.3f)); // 30% opacity
    }
}