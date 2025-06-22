using UnityEditor;
using UnityEngine;

public static class GridSnappingTool
{
    public static Vector3 SnapToGrid(Vector3 position, float gridSize)
    {
        return new Vector3(
            Mathf.Round(position.x / gridSize) * gridSize,
            Mathf.Round(position.y / gridSize) * gridSize,
            Mathf.Round(position.z / gridSize) * gridSize
        );
    }

    public static void ApplySnapping(GameObject[] objects, float gridSize)
    {
        foreach (var obj in objects)
        {
            Undo.RecordObject(obj.transform, "Snap to Grid");
            obj.transform.position = SnapToGrid(obj.transform.position, gridSize);
        }
    }
}