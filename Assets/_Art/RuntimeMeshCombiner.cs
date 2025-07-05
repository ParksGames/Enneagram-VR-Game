using UnityEngine;

public class RuntimeMeshCombiner : MonoBehaviour
{
    public bool disableChildren = true; // Birleştirdikten sonra çocukları kapat

    void Awake()
    {
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
        if (meshFilters.Length < 2)
        {
            Debug.LogWarning("Birleştirmek için en az 2 mesh olmalı.");
            return;
        }

        CombineInstance[] combine = new CombineInstance[meshFilters.Length];
        Material sharedMat = null;

        for (int i = 0; i < meshFilters.Length; i++)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;

            if (sharedMat == null)
            {
                var renderer = meshFilters[i].GetComponent<MeshRenderer>();
                if (renderer != null)
                    sharedMat = renderer.sharedMaterial;
            }
        }

        Mesh combinedMesh = new Mesh();
        combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        combinedMesh.CombineMeshes(combine);

        // Yeni bir mesh objesi oluştur
        GameObject combinedObject = new GameObject("CombinedRuntimeMesh");
        combinedObject.transform.SetParent(transform, false);
        combinedObject.AddComponent<MeshFilter>().sharedMesh = combinedMesh;
        combinedObject.AddComponent<MeshRenderer>().sharedMaterial = sharedMat;

        // Çocuk meshleri kapat
        if (disableChildren)
        {
            foreach (var mf in meshFilters)
            {
                if (mf != null && mf.gameObject != combinedObject)
                    mf.gameObject.SetActive(false);
            }
        }
    }
}