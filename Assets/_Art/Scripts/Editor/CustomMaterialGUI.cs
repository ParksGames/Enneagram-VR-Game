using UnityEditor;
using UnityEngine;

public class CustomMaterialGUI : ShaderGUI
{
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        var material = materialEditor.target as Material;

        var threshold = FindProperty("_Threshold", properties);
        FloatProperty(threshold, threshold.displayName);
    }

    private float FloatProperty(MaterialProperty prop, string label)
    {
        EditorGUI.BeginChangeCheck();
        EditorGUI.showMixedValue = prop.hasMixedValue;
        var threshold  = EditorGUILayout.FloatField(label, prop.floatValue);
        EditorGUI.showMixedValue = false;
        if (EditorGUI.EndChangeCheck())
        {
            prop.floatValue = threshold;
        }
        return prop.floatValue;
    }
}