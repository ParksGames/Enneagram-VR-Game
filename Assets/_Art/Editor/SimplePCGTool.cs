using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Splines; 

public class SimplePCGTool : EditorWindow
{
    [Header("Setup")]
    public GameObject targetMeshObj;
    public List<GameObject> prefabs = new List<GameObject>();
    public LayerMask allowedLayerMask = ~0;
    public LayerMask blockLayerMask = 0; 
    public int instanceCount = 100;
    public float minScale = 1f, maxScale = 1f;
    public bool autoRecreate = true;
    public bool directInstantiate = false;

    [Header("Randomize")]
    public int randomSeed = 12345;

    [Header("Spline Masking (optional)")]
    public GameObject[] splineObjects;
    public float splineInfluenceRadius = 5f;
    public bool restrictToSplines = false;

    List<Matrix4x4> previewTransforms = new List<Matrix4x4>();
    List<GameObject> bakedObjects = new List<GameObject>();

    [MenuItem("Tools/Simple PCG Tool")]
    static void Open() => GetWindow<SimplePCGTool>("Simple PCG Tool");

    void OnGUI()
    {
        EditorGUI.BeginChangeCheck();
        
        EditorGUILayout.LabelField("PCG Placement Tool", EditorStyles.boldLabel);
        targetMeshObj = (GameObject)EditorGUILayout.ObjectField("Target Mesh", targetMeshObj, typeof(GameObject), true);

        SerializedObject so = new SerializedObject(this);
        SerializedProperty prefabsProp = so.FindProperty("prefabs");
        EditorGUILayout.PropertyField(prefabsProp, true);
        so.ApplyModifiedProperties();

        allowedLayerMask = EditorGUILayout.MaskField("Allowed Layer(s)", allowedLayerMask, GetLayerNames());
        blockLayerMask = EditorGUILayout.MaskField("Block Layer(s)", blockLayerMask, GetLayerNames());

        instanceCount = EditorGUILayout.IntField("Instance Count", instanceCount);
        minScale = EditorGUILayout.FloatField("Min Scale", minScale);
        maxScale = EditorGUILayout.FloatField("Max Scale", maxScale);

        randomSeed = EditorGUILayout.IntField("Random Seed", randomSeed);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Spline Masking", EditorStyles.boldLabel);
        SerializedProperty splineProp = so.FindProperty("splineObjects");
        EditorGUILayout.PropertyField(splineProp, true);
        so.ApplyModifiedProperties();

        splineInfluenceRadius = EditorGUILayout.FloatField("Spline Influence Radius", splineInfluenceRadius);
        restrictToSplines = EditorGUILayout.Toggle("Restrict To Splines", restrictToSplines);
        autoRecreate = EditorGUILayout.Toggle("Auto Recreate", autoRecreate);
        directInstantiate = EditorGUILayout.Toggle("Direct Instantiate", directInstantiate);

        EditorGUILayout.Space();
        if (GUILayout.Button("Create"))
        {
            if(directInstantiate)
            {
                GeneratePreview();
                BakePrefabs();
            }
            else
            {
                GeneratePreview(); 
            }
        }
        if (!directInstantiate && GUILayout.Button("Bake"))
            BakePrefabs();

        if (GUILayout.Button("Clear Preview"))
        {
            previewTransforms.Clear();
            foreach(var obj in bakedObjects)
            {
                if(obj != null) 
                    DestroyImmediate(obj);
            }
            bakedObjects.Clear();
        }

        if (EditorGUI.EndChangeCheck())
        {
            if(autoRecreate)
                GeneratePreview();
            
            // Update each baked object with its own random scale
            foreach(var obj in bakedObjects)
            {
                if(obj == null) continue;
                if(obj.name == "PCG_Generated" || obj.name.EndsWith("_Group")) continue;
                    
                Random.InitState(randomSeed + obj.GetInstanceID());
                float scale = Random.Range(minScale, maxScale);
                obj.transform.localScale = Vector3.one * scale;
            }
        }
        
        // Check for block layer changes
        if(targetMeshObj != null)
        {
            bool shouldRegenerate = false;
            var blockObjects = Physics.OverlapSphere(Vector3.zero, float.MaxValue).Where(c => ((1 << c.gameObject.layer) & blockLayerMask) != 0).Select(c => c.gameObject).ToArray();
            foreach(var blockObj in blockObjects)
            {
                if(((1 << blockObj.layer) & blockLayerMask) != 0)
                {
                    shouldRegenerate = true;
                    break;
                }
            }
            if(shouldRegenerate && autoRecreate)
            {
                GeneratePreview();
                if(directInstantiate) BakePrefabs();
            }
        }
    }

    void GeneratePreview()
    {
        previewTransforms.Clear();
        if (!targetMeshObj) return;

        MeshFilter mf = targetMeshObj.GetComponent<MeshFilter>();
        if (!mf || !mf.sharedMesh) return;
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
                previewTransforms.Add(Matrix4x4.TRS(hit.point, rot, Vector3.one * scale));
                placed++;
            }
        }
        SceneView.RepaintAll();
    }

    void BakePrefabs()
    {
        if (!targetMeshObj || prefabs.Count == 0) return;

        Undo.IncrementCurrentGroup();

        GameObject parentObj = new GameObject("PCG_Generated");
        Undo.RegisterCreatedObjectUndo(parentObj, "PCG Parent Object");
        bakedObjects.Add(parentObj);

        Dictionary<GameObject, GameObject> prefabParents = new Dictionary<GameObject, GameObject>();

        int index = 0;
        foreach (var mat in previewTransforms)
        {
            Random.InitState(randomSeed + index);
            GameObject prefab = prefabs[Random.Range(0, prefabs.Count)];

            if (!prefabParents.ContainsKey(prefab))
            {
                GameObject prefabParent = new GameObject(prefab.name + "_Group");
                prefabParent.transform.parent = parentObj.transform;
                prefabParents[prefab] = prefabParent;
                Undo.RegisterCreatedObjectUndo(prefabParent, "PCG Prefab Group");
                bakedObjects.Add(prefabParent);
            }

            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(go, "PCG Place Prefab");
            go.transform.parent = prefabParents[prefab].transform;
            go.transform.position = mat.GetColumn(3);
            go.transform.rotation = mat.rotation;
            
            Random.InitState(randomSeed + go.GetInstanceID());
            float scale = Random.Range(minScale, maxScale);
            go.transform.localScale = Vector3.one * scale;
            
            bakedObjects.Add(go);
            index++;
        }
    
        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
        previewTransforms.Clear();
    }

    void OnSceneGUI(SceneView scene)
    {
        Handles.color = Color.green;
        foreach (var mat in previewTransforms)
            Handles.SphereHandleCap(0, mat.GetColumn(3), Quaternion.identity, 0.25f, EventType.Repaint);
    }

    string[] GetLayerNames()
    {
        var names = new List<string>();
        for (int i = 0; i < 32; i++)
        {
            var name = LayerMask.LayerToName(i);
            if (!string.IsNullOrEmpty(name))
                names.Add(name);
            else
                names.Add($"Layer {i}");
        }
        return names.ToArray();
    }

    void OnEnable() => SceneView.duringSceneGui += OnSceneGUI;
    void OnDisable() => SceneView.duringSceneGui -= OnSceneGUI;
}