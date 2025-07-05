using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MeshSaver : MonoBehaviour
{
#if UNITY_EDITOR
    [ContextMenu("Save Mesh As Asset")]
    void SaveMesh()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
        {
            Debug.LogWarning("No MeshFilter or mesh found on this GameObject.");
            return;
        }

        Mesh originalMesh = mf.sharedMesh;
        Mesh newMesh = new Mesh();
        newMesh.vertices = originalMesh.vertices;
        newMesh.triangles = originalMesh.triangles;
        newMesh.normals = originalMesh.normals;
        newMesh.uv = originalMesh.uv;
        newMesh.tangents = originalMesh.tangents;
        newMesh.colors = originalMesh.colors;

        newMesh.RecalculateBounds();

        string path = "Assets/ConvertedMesh.asset";

        AssetDatabase.CreateAsset(newMesh, path);
        AssetDatabase.SaveAssets();

        Debug.Log("Mesh saved at " + path);
    }
#endif
}