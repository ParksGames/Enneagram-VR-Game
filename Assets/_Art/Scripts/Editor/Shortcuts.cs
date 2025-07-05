using UnityEngine;
using UnityEditor;
using System;

public class ComponentCopyPasteShortcut
{
    private static Component copiedComponent;

    [MenuItem("CT//Copy Component %#c", false, 1)]
    static void CopyComponentShortcut()
    {
        if (Selection.activeGameObject == null)
        {
            Debug.LogWarning("No GameObject selected!");
            return;
        }

        var rectTransform = Selection.activeGameObject.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            copiedComponent = rectTransform;
        }
        else
        {
            copiedComponent = Selection.activeGameObject.GetComponent<Transform>();
        }

        Debug.Log($"Copied component: {copiedComponent.GetType().Name}");
    }

    [MenuItem("CT//Paste Component %#v", false, 2)]
    static void PasteComponentShortcut()
    {
        if (!ValidateSelection()) return;

        if (copiedComponent == null)
        {
            Debug.LogWarning("No valid component copied!");
            return;
        }

        Type type = copiedComponent.GetType();
        Component existingComp = Selection.activeGameObject.GetComponent(type);
        Component targetComp;

        if (existingComp != null)
        {
            targetComp = existingComp;
        }
        else
        {
            targetComp = Selection.activeGameObject.AddComponent(type);
        }

        SerializedObject src = new SerializedObject(copiedComponent);
        SerializedObject dst = new SerializedObject(targetComp);

        SerializedProperty srcProp = src.GetIterator();
        SerializedProperty dstProp = dst.GetIterator();

        while (srcProp.NextVisible(true))
        {
            dstProp.NextVisible(true);
            dst.CopyFromSerializedProperty(srcProp);
        }
        
        dst.ApplyModifiedProperties();
        Debug.Log($"Pasted component: {type.Name} to {Selection.activeGameObject.name}");
    }
    private static Component GetComponentUnderMouse()
    {
        if (EditorWindow.mouseOverWindow == null) return null;

        var inspectorWindow = EditorWindow.mouseOverWindow;
        var propertyInfo = inspectorWindow.GetType().GetProperty("inspectorMode", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        return propertyInfo?.GetValue(inspectorWindow) as Component;
    }

    private static bool ValidateSelection()
    {
        if (Selection.activeGameObject == null)
        {
            Debug.LogWarning("No GameObject selected!");
            return false;
        }
        return true;
    }
}