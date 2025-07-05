using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace PCG
{
    /// <summary>
    /// Handles the instantiation of prefabs and undo support for the PCG tool.
    /// </summary>
    public class PCGBakeManager
    {
        private PCGSettings settings;
        private List<GameObject> bakedObjects = new List<GameObject>();
        private GameObject rootObject;
        private Dictionary<GameObject, GameObject> prefabParents = new Dictionary<GameObject, GameObject>();

        /// <summary>
        /// Initialize the bake manager with settings
        /// </summary>
        public PCGBakeManager(PCGSettings settings)
        {
            this.settings = settings;
        }

        /// <summary>
        /// Clear all previously baked objects
        /// </summary>
        public void Clear()
        {
            // Use Undo system to allow reverting the clear operation
            Undo.IncrementCurrentGroup();
            
            foreach (var obj in bakedObjects)
            {
                if (obj != null)
                {
                    Undo.DestroyObjectImmediate(obj);
                }
            }
            
            bakedObjects.Clear();
            prefabParents.Clear();
            rootObject = null;
            
            Undo.SetCurrentGroupName("PCG Clear");
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
        }

        /// <summary>
        /// Bake the placement data into actual GameObjects in the scene
        /// </summary>
        public void Bake(List<PCGPlacementEngine.PlacementData> placements)
        {
            if (placements == null || placements.Count == 0)
                return;

            // Clear previous objects if they exist
            Clear();

            // Start a new undo group
            Undo.IncrementCurrentGroup();

            // Create a root object to hold all baked objects
            rootObject = new GameObject("PCG_Generated");
            Undo.RegisterCreatedObjectUndo(rootObject, "PCG Root Object");
            bakedObjects.Add(rootObject);

            // Create prefab parent objects if grouping is enabled
            if (settings.groupByPrefab)
            {
                foreach (var prefabSetting in settings.prefabs)
                {
                    if (prefabSetting.prefab == null)
                        continue;

                    string prefabName = prefabSetting.prefab.name;
                    GameObject prefabParent = new GameObject(prefabName + "_Group");
                    prefabParent.transform.parent = rootObject.transform;
                    
                    Undo.RegisterCreatedObjectUndo(prefabParent, "PCG Prefab Group");
                    bakedObjects.Add(prefabParent);
                    prefabParents[prefabSetting.prefab] = prefabParent;
                }
            }

            // Instantiate all prefabs
            foreach (var placement in placements)
            {
                if (placement.prefab == null)
                    continue;

                // Instantiate the prefab
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(placement.prefab);
                if (instance == null)
                    continue;

                // Register for undo
                Undo.RegisterCreatedObjectUndo(instance, "PCG Place Prefab");

                // Set parent
                if (settings.groupByPrefab && prefabParents.ContainsKey(placement.prefab))
                {
                    instance.transform.parent = prefabParents[placement.prefab].transform;
                }
                else
                {
                    instance.transform.parent = rootObject.transform;
                }

                // Set transform
                instance.transform.position = placement.position;
                instance.transform.rotation = placement.rotation;
                instance.transform.localScale = placement.scale;

                bakedObjects.Add(instance);
            }

            // Name the undo group and collapse operations
            Undo.SetCurrentGroupName("PCG Bake");
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
        }

        /// <summary>
        /// Get all currently baked objects
        /// </summary>
        public List<GameObject> GetBakedObjects()
        {
            return new List<GameObject>(bakedObjects);
        }

        /// <summary>
        /// Check if there are any baked objects
        /// </summary>
        public bool HasBakedObjects()
        {
            return bakedObjects.Count > 0 && bakedObjects.Any(obj => obj != null);
        }

        /// <summary>
        /// Export baked objects to a prefab
        /// </summary>
        public void ExportToPrefab(string path)
        {
            if (rootObject == null)
                return;

            // Create a temporary parent to hold a clone of the hierarchy
            GameObject tempRoot = new GameObject("Temp_PCG_Export");
            GameObject clone = Object.Instantiate(rootObject);
            clone.transform.parent = tempRoot.transform;

            // Create the prefab
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(clone, path);
            
            // Clean up
            Object.DestroyImmediate(tempRoot);
            
            if (prefab != null)
            {
                Debug.Log("PCG objects exported to prefab: " + path);
                EditorGUIUtility.PingObject(prefab);
            }
        }

        /// <summary>
        /// Select all baked objects in the hierarchy
        /// </summary>
        public void SelectAllInHierarchy()
        {
            if (bakedObjects.Count == 0)
                return;

            List<Object> validObjects = new List<Object>();
            foreach (var obj in bakedObjects)
            {
                if (obj != null)
                    validObjects.Add(obj);
            }

            Selection.objects = validObjects.ToArray();
        }
    }
}