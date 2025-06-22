using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Splines;

namespace PCG
{
    /// <summary>
    /// ScriptableObject that stores all settings for the PCG tool.
    /// This allows saving and loading PCG setups.
    /// </summary>
    [CreateAssetMenu(fileName = "New PCG Setup", menuName = "PCG/PCG Setup", order = 1)]
    public class PCGSettings : ScriptableObject
    {
        [Serializable]
        public class PrefabSettings
        {
            public GameObject prefab;
            [Range(0, 100)]
            [Tooltip("Relative chance of this prefab being selected")]
            public float weight = 1f;
            [Tooltip("Minimum scale multiplier")]
            public float minScale = 0.8f;
            [Tooltip("Maximum scale multiplier")]
            public float maxScale = 1.2f;
            [Tooltip("Minimum rotation in degrees on each axis")]
            public Vector3 minRotation = Vector3.zero;
            [Tooltip("Maximum rotation in degrees on each axis")]
            public Vector3 maxRotation = new Vector3(0, 360, 0);
        }

        [Serializable]
        public class PlacementFilter
        {
            public bool enabled = true;
            [Tooltip("Name of this filter for organization")]
            public string name = "New Filter";
        }

        [Serializable]
        public class SlopeFilter : PlacementFilter
        {
            [Range(0, 90)]
            [Tooltip("Minimum surface angle in degrees")]
            public float minAngle = 0f;
            [Range(0, 90)]
            [Tooltip("Maximum surface angle in degrees")]
            public float maxAngle = 45f;
        }

        [Serializable]
        public class AltitudeFilter : PlacementFilter
        {
            [Tooltip("Minimum height (Y-coordinate)")]
            public float minHeight = float.MinValue;
            [Tooltip("Maximum height (Y-coordinate)")]
            public float maxHeight = float.MaxValue;
        }

        [Serializable]
        public class ProximityFilter : PlacementFilter
        {
            [Tooltip("Minimum distance between placed objects")]
            public float minDistance = 1f;
            [Tooltip("Maximum distance between placed objects (0 = no limit)")]
            public float maxDistance = 0f;
        }

        [Serializable]
        public class SplineFilter : PlacementFilter
        {
            public enum SplineMode
            {
                Within,
                Beyond
            }

            [Tooltip("Spline objects to use for filtering")]
            public List<GameObject> splineObjects = new List<GameObject>();
            [Tooltip("Distance from spline to filter")]
            public float distance = 5f;
            [Tooltip("Whether to place objects within or beyond the distance")]
            public SplineMode mode = SplineMode.Within;
        }

        [Serializable]
        public class MaskFilter : PlacementFilter
        {
            public enum MaskChannel
            {
                Red,
                Green,
                Blue,
                Alpha
            }

            [Tooltip("Texture to use as a mask")]
            public Texture2D maskTexture;
            [Tooltip("Which channel to use from the mask texture")]
            public MaskChannel channel = MaskChannel.Red;
            [Range(0, 1)]
            [Tooltip("Minimum value in the mask to allow placement (0-1)")]
            public float threshold = 0.5f;
            [Tooltip("Invert the mask")]
            public bool invert = false;
        }

        [Serializable]
        public class VertexColorFilter : PlacementFilter
        {
            public enum ColorChannel
            {
                Red,
                Green,
                Blue,
                Alpha
            }

            [Tooltip("Which vertex color channel to use")]
            public ColorChannel channel = ColorChannel.Red;
            [Range(0, 1)]
            [Tooltip("Minimum value in the vertex color to allow placement (0-1)")]
            public float threshold = 0.5f;
            [Tooltip("Invert the filter")]
            public bool invert = false;
        }

        // Target Mesh Settings
        [Header("Target Mesh")]
        [Tooltip("GameObjects with meshes to use as placement targets")]
        public List<GameObject> targetMeshes = new List<GameObject>();

        // Prefab Settings
        [Header("Prefabs")]
        [Tooltip("Prefabs to place on the target meshes")]
        public List<PrefabSettings> prefabs = new List<PrefabSettings>();
        [Tooltip("Group placed objects by prefab type under parent objects")]
        public bool groupByPrefab = true;

        // Placement Settings
        [Header("Placement")]
        [Tooltip("Number of objects to place")]
        public int instanceCount = 100;
        [Tooltip("Maximum number of placement attempts per object")]
        public int maxPlacementAttempts = 1000;
        [Tooltip("Random seed for deterministic placement")]
        public int randomSeed = 12345;
        [Tooltip("Layers that are valid for placement")]
        public LayerMask allowedLayerMask = ~0;
        [Tooltip("Layers that block placement")]
        public LayerMask blockLayerMask = 0;
        [Tooltip("Radius around objects to prevent overlapping")]
        public float collisionRadius = 0.5f;

        // Filters
        [Header("Filters")]
        [Tooltip("Filter by surface slope")]
        public SlopeFilter slopeFilter = new SlopeFilter();
        [Tooltip("Filter by altitude (Y-coordinate)")]
        public AltitudeFilter altitudeFilter = new AltitudeFilter();
        [Tooltip("Filter by proximity to other placed objects")]
        public ProximityFilter proximityFilter = new ProximityFilter();
        [Tooltip("Filter by distance to splines")]
        public SplineFilter splineFilter = new SplineFilter();
        [Tooltip("Filter by texture mask")]
        public MaskFilter maskFilter = new MaskFilter();
        [Tooltip("Filter by vertex colors")]
        public VertexColorFilter vertexColorFilter = new VertexColorFilter();

        // Custom extension data
        [HideInInspector]
        public string extensionData;

        /// <summary>
        /// Creates a copy of the settings
        /// </summary>
        public PCGSettings Clone()
        {
            PCGSettings clone = CreateInstance<PCGSettings>();
            clone.targetMeshes = new List<GameObject>(targetMeshes);
            clone.prefabs = new List<PrefabSettings>();
            foreach (var prefab in prefabs)
            {
                clone.prefabs.Add(new PrefabSettings
                {
                    prefab = prefab.prefab,
                    weight = prefab.weight,
                    minScale = prefab.minScale,
                    maxScale = prefab.maxScale,
                    minRotation = prefab.minRotation,
                    maxRotation = prefab.maxRotation
                });
            }
            clone.groupByPrefab = groupByPrefab;
            clone.instanceCount = instanceCount;
            clone.maxPlacementAttempts = maxPlacementAttempts;
            clone.randomSeed = randomSeed;
            clone.allowedLayerMask = allowedLayerMask;
            clone.blockLayerMask = blockLayerMask;
            clone.collisionRadius = collisionRadius;
            
            // Clone filters
            clone.slopeFilter = new SlopeFilter
            {
                enabled = slopeFilter.enabled,
                name = slopeFilter.name,
                minAngle = slopeFilter.minAngle,
                maxAngle = slopeFilter.maxAngle
            };
            
            clone.altitudeFilter = new AltitudeFilter
            {
                enabled = altitudeFilter.enabled,
                name = altitudeFilter.name,
                minHeight = altitudeFilter.minHeight,
                maxHeight = altitudeFilter.maxHeight
            };
            
            clone.proximityFilter = new ProximityFilter
            {
                enabled = proximityFilter.enabled,
                name = proximityFilter.name,
                minDistance = proximityFilter.minDistance,
                maxDistance = proximityFilter.maxDistance
            };
            
            clone.splineFilter = new SplineFilter
            {
                enabled = splineFilter.enabled,
                name = splineFilter.name,
                splineObjects = new List<GameObject>(splineFilter.splineObjects),
                distance = splineFilter.distance,
                mode = splineFilter.mode
            };
            
            clone.maskFilter = new MaskFilter
            {
                enabled = maskFilter.enabled,
                name = maskFilter.name,
                maskTexture = maskFilter.maskTexture,
                channel = maskFilter.channel,
                threshold = maskFilter.threshold,
                invert = maskFilter.invert
            };
            
            clone.vertexColorFilter = new VertexColorFilter
            {
                enabled = vertexColorFilter.enabled,
                name = vertexColorFilter.name,
                channel = vertexColorFilter.channel,
                threshold = vertexColorFilter.threshold,
                invert = vertexColorFilter.invert
            };
            
            clone.extensionData = extensionData;
            
            return clone;
        }

        /// <summary>
        /// Export settings to JSON
        /// </summary>
        public string ToJson()
        {
            return JsonUtility.ToJson(this, true);
        }

        /// <summary>
        /// Import settings from JSON
        /// </summary>
        public static PCGSettings FromJson(string json)
        {
            return JsonUtility.FromJson<PCGSettings>(json);
        }
    }
}