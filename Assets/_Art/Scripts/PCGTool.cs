using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Splines;

[ExecuteInEditMode]
public class SimplePCGComponent : MonoBehaviour 
{
    [Header("Setup")]
    public GameObject targetMeshObj;
    public List<GameObject> prefabs = new List<GameObject>();
    public LayerMask allowedLayerMask = ~0;
    public LayerMask blockLayerMask = 0;
    public int instanceCount = 100;
    public float minScale = 1f, maxScale = 1f;
    
    [Header("Randomize")] 
    public int randomSeed = 12345;

    [Header("Spline Masking (optional)")]
    public GameObject[] splineObjects;
    public float splineInfluenceRadius = 5f;
    public bool restrictToSplines = false;

    private List<GameObject> spawnedObjects = new List<GameObject>();
    private int lastSeed;
    private float lastMinScale, lastMaxScale;
    private int lastInstanceCount;
    private LayerMask lastAllowedMask, lastBlockMask;
    private bool lastRestrictToSplines;
    private float lastSplineRadius;
    private List<GameObject> lastPrefabs = new List<GameObject>();
    private GameObject lastTarget;
    private GameObject[] lastSplines;

    void OnValidate()
    {
        bool needsUpdate = false;

        if (lastSeed != randomSeed || lastMinScale != minScale || lastMaxScale != maxScale ||
            lastInstanceCount != instanceCount || lastAllowedMask != allowedLayerMask || 
            lastBlockMask != blockLayerMask || lastRestrictToSplines != restrictToSplines ||
            lastSplineRadius != splineInfluenceRadius || lastTarget != targetMeshObj ||
            !prefabs.SequenceEqual(lastPrefabs) ||
            (splineObjects != null && lastSplines != null && !splineObjects.SequenceEqual(lastSplines)))
        {
            needsUpdate = true;
        }

        if (needsUpdate)
        {
            EditorApplication.delayCall += () => {
                RegenerateObjects();
                UpdateLastValues();
            };
        }
    }
    void UpdateLastValues()
    {
        lastSeed = randomSeed;
        lastMinScale = minScale;
        lastMaxScale = maxScale;
        lastInstanceCount = instanceCount;
        lastAllowedMask = allowedLayerMask;
        lastBlockMask = blockLayerMask;
        lastRestrictToSplines = restrictToSplines;  
        lastSplineRadius = splineInfluenceRadius;
        lastTarget = targetMeshObj;
        lastPrefabs = new List<GameObject>(prefabs);
        lastSplines = splineObjects != null ? splineObjects.ToArray() : null;
    }
[ContextMenu("Regenerate Objects")]
    void RegenerateObjects()
    {
        if(!targetMeshObj || prefabs.Count == 0) return;

        // Generate preview transforms
        List<Matrix4x4> transforms = GenerateTransforms();
        
        // Create or update objects
        int existingCount = spawnedObjects.Count;
        int targetCount = transforms.Count;
        int index = 0;

        // Reuse existing objects first
        for(; index < Mathf.Min(existingCount, targetCount); index++)
        {
            if(spawnedObjects[index] == null) continue;
            
            Random.InitState(randomSeed + index);
            GameObject prefab = prefabs[Random.Range(0, prefabs.Count)];
            
            var transform = transforms[index];
            spawnedObjects[index].transform.position = transform.GetColumn(3);
            spawnedObjects[index].transform.rotation = transform.rotation;
            
            Random.InitState(randomSeed + spawnedObjects[index].GetInstanceID());
            float scale = Random.Range(minScale, maxScale);
            spawnedObjects[index].transform.localScale = Vector3.one * scale;
        }

        // Remove excess objects
        for(int i = targetCount; i < existingCount; i++)
        {
            if(spawnedObjects[i] != null)
            {
                EditorApplication.delayCall += () => {
                    for (int i = 0; i < 3; i++)
                    {
                        EditorApplication.delayCall += () => DestroyImmediate(spawnedObjects[i]);
                    }
                };
            }
        }
        if(existingCount > targetCount)
        {
            spawnedObjects.RemoveRange(targetCount, existingCount - targetCount);
        }

        // Create new objects if needed
        for(; index < targetCount; index++)
        {
            Random.InitState(randomSeed + index);
            GameObject prefab = prefabs[Random.Range(0, prefabs.Count)];
            
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.parent = this.transform;
            go.transform.position = transforms[index].GetColumn(3);
            go.transform.rotation = transforms[index].rotation;
            
            Random.InitState(randomSeed + go.GetInstanceID());
            float scale = Random.Range(minScale, maxScale);
            go.transform.localScale = Vector3.one * scale;
            
            spawnedObjects.Add(go);
        }
    }
    List<Matrix4x4> GenerateTransforms()
    {
        List<Matrix4x4> transforms = new List<Matrix4x4>();
        if (!targetMeshObj) return transforms;

        MeshFilter mf = targetMeshObj.GetComponent<MeshFilter>();
        if (!mf || !mf.sharedMesh) return transforms;
        
        Mesh mesh = mf.sharedMesh;
        var verts = mesh.vertices;
        var tris = mesh.triangles;
        var objTr = targetMeshObj.transform;

        int tries = 0, placed = 0;
        while (placed < instanceCount && tries < instanceCount * 10)
        {
            tries++;
            Random.InitState(randomSeed + tries);
            
            int triIndex = Random.Range(0, tris.Length / 3) * 3;
            Vector3 v0 = objTr.TransformPoint(verts[tris[triIndex]]);
            Vector3 v1 = objTr.TransformPoint(verts[tris[triIndex + 1]]);
            Vector3 v2 = objTr.TransformPoint(verts[tris[triIndex + 2]]);

            float r1 = Mathf.Sqrt(Random.value);
            float r2 = Random.value;
            Vector3 pos = (1 - r1) * v0 + r1 * (1 - r2) * v1 + r1 * r2 * v2;

            Ray ray = new Ray(pos + Vector3.up * 10f, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, 20f, allowedLayerMask))
            {
                if (blockLayerMask != 0 && ((1 << hit.collider.gameObject.layer) & blockLayerMask) != 0)
                    continue;

                if (restrictToSplines && splineObjects != null && splineObjects.Length > 0)
                {
                    bool nearSpline = false;
                    foreach (var s in splineObjects)
                    {
                        if (!s) continue;
                        var spline = s.GetComponent<SplineContainer>();
                        if (spline != null)
                        {
                            float closestDist = float.MaxValue;
                            foreach (var knot in spline.Spline.Knots)
                            {
                                float d = Vector3.Distance(hit.point, s.transform.TransformPoint(knot.Position));
                                if (d < closestDist) closestDist = d;
                            }
                            if (closestDist < splineInfluenceRadius) nearSpline = true;
                        }
                    }
                    if (!nearSpline) continue;
                }

                Vector3 up = hit.normal;
                Quaternion rot = Quaternion.FromToRotation(Vector3.up, up) * Quaternion.Euler(0, Random.Range(0, 360f), 0);
                float scale = Random.Range(minScale, maxScale);
                transforms.Add(Matrix4x4.TRS(hit.point, rot, Vector3.one * scale));
                placed++;
            }
        }

        return transforms;
    }
}