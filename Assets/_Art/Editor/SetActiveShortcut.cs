using UnityEditor;
using UnityEngine;

public class SetActiveShortcut
{
    [MenuItem("CT/Toggle Active %#h", false, 0)] // Ctrl+Shift+H
    static void ToggleActive()
    {
        foreach (var obj in Selection.gameObjects)
        {
            Undo.RecordObject(obj, "Toggle Active State");
            obj.SetActive(!obj.activeSelf);
        }
    }
}