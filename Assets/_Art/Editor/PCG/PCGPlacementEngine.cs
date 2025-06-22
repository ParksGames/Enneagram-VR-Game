using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;
using UnityEngine.Splines;

namespace PCG
{
    /// <summary>
    /// Core engine for PCG placement calculations.
    /// Handles mesh sampling, filtering, and placement generation.
    /// Uses Unity Jobs/Burst for performance when available.
    /// </summary>
    public class PCGPlacementEngine
    {
        // Placement result data
        public struct PlacementData
        {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
            public GameObject prefab;
            public int prefabIndex;
        }

        // Cache for mesh data
        private class MeshCache
        {
            public Mesh mesh;
            public Vector3[] vertices;
            public int[] triangles;
            public Vector3[] normals;
            public Color[] colors;
            public Transform transform;
            public float[] triangleAreas;
            public float totalArea;
        }

        private PCGSettings settings;
        private List<MeshCache> meshCaches = new List<MeshCache>();
        private System.Random random;
        private List<Vector3> placedPositions = new List<Vector3>();

        /// <summary>
        /// Initialize the placement engine with settings
        /// </summary>
        public PCGPlacementEngine(PCGSettings settings)
        {
            this.settings = settings;
            random = new System.Random(settings.randomSeed);
            CacheMeshData();
        }

        /// <summary>
        /// Cache mesh data for faster processing
        /// </summary>
        private void CacheMeshData()
        {
            meshCaches.Clear();

            foreach (var targetObj in settings.targetMeshes)
            {
                if (targetObj == null) continue;

                // Get all mesh filters (including children if they exist)
                var meshFilters = targetObj.GetComponentsInChildren<MeshFilter>();
                foreach (var mf in meshFilters)
                {
                    if (mf == null || mf.sharedMesh == null) continue;

                    var cache = new MeshCache
                    {
                        mesh = mf.sharedMesh,
                        vertices = mf.sharedMesh.vertices,
                        triangles = mf.sharedMesh.triangles,
                        normals = mf.sharedMesh.normals,
                        colors = mf.sharedMesh.colors,
                        transform = mf.transform
                    };

                    // Precalculate triangle areas for weighted sampling
                    cache.triangleAreas = new float[cache.triangles.Length / 3];
                    cache.totalArea = 0;

                    for (int i = 0; i < cache.triangles.Length; i += 3)
                    {
                        Vector3 v0 = cache.vertices[cache.triangles[i]];
                        Vector3 v1 = cache.vertices[cache.triangles[i + 1]];
                        Vector3 v2 = cache.vertices[cache.triangles[i + 2]];

                        float area = Vector3.Cross(v1 - v0, v2 - v0).magnitude * 0.5f;
                        cache.triangleAreas[i / 3] = area;
                        cache.totalArea += area;
                    }

                    meshCaches.Add(cache);
                }
            }
        }

        /// <summary>
        /// Generate placement data based on settings
        /// </summary>
        public List<PlacementData> GeneratePlacements()
        {
            if (meshCaches.Count == 0 || settings.prefabs.Count == 0)
                return new List<PlacementData>();

            List<PlacementData> placements = new List<PlacementData>();
            placedPositions.Clear();

            int attempts = 0;
            int maxAttempts = settings.instanceCount * settings.maxPlacementAttempts;

            // Normalize prefab weights
            float totalWeight = settings.prefabs.Sum(p => p.weight);
            float[] normalizedWeights = settings.prefabs.Select(p => p.weight / totalWeight).ToArray();
            float[] cumulativeWeights = new float[normalizedWeights.Length];
            float sum = 0;
            for (int i = 0; i < normalizedWeights.Length; i++)
            {
                sum += normalizedWeights[i];
                cumulativeWeights[i] = sum;
            }

            while (placements.Count < settings.instanceCount && attempts < maxAttempts)
            {
                attempts++;
                
                // Sample a random point on the mesh
                Vector3 position;
                Vector3 normal;
                Color vertexColor = Color.white;
                if (!SampleMeshPoint(out position, out normal, out vertexColor))
                    continue;

                // Check if the point passes all filters
                if (!PassesAllFilters(position, normal, vertexColor, placements))
                    continue;

                // Select a prefab based on weights
                int prefabIndex = SelectPrefabIndex(cumulativeWeights);
                if (prefabIndex < 0 || prefabIndex >= settings.prefabs.Count)
                    continue;

                var prefabSettings = settings.prefabs[prefabIndex];
                
                // Generate random rotation
                Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal);
                Vector3 randomRotation = new Vector3(
                    RandomRange(prefabSettings.minRotation.x, prefabSettings.maxRotation.x),
                    RandomRange(prefabSettings.minRotation.y, prefabSettings.maxRotation.y),
                    RandomRange(prefabSettings.minRotation.z, prefabSettings.maxRotation.z)
                );
                rotation *= Quaternion.Euler(randomRotation);

                // Generate random scale
                float scale = RandomRange(prefabSettings.minScale, prefabSettings.maxScale);
                Vector3 scaleVector = Vector3.one * scale;

                // Create placement data
                PlacementData placement = new PlacementData
                {
                    position = position,
                    rotation = rotation,
                    scale = scaleVector,
                    prefab = prefabSettings.prefab,
                    prefabIndex = prefabIndex
                };

                placements.Add(placement);
                placedPositions.Add(position);
            }

            return placements;
        }

        /// <summary>
        /// Sample a random point on the mesh surface
        /// </summary>
        private bool SampleMeshPoint(out Vector3 position, out Vector3 normal, out Color vertexColor)
        {
            position = Vector3.zero;
            normal = Vector3.up;
            vertexColor = Color.white;

            if (meshCaches.Count == 0)
                return false;

            // Select a random mesh based on total area
            float totalArea = meshCaches.Sum(m => m.totalArea);
            float randomArea = (float)(random.NextDouble() * totalArea);
            
            MeshCache selectedMesh = null;
            float areaSum = 0;
            
            foreach (var mesh in meshCaches)
            {
                areaSum += mesh.totalArea;
                if (randomArea <= areaSum)
                {
                    selectedMesh = mesh;
                    break;
                }
            }

            if (selectedMesh == null)
                selectedMesh = meshCaches[0];

            // Select a random triangle weighted by area
            float meshRandomArea = (float)(random.NextDouble() * selectedMesh.totalArea);
            int triangleIndex = -1;
            float triangleAreaSum = 0;
            
            for (int i = 0; i < selectedMesh.triangleAreas.Length; i++)
            {
                triangleAreaSum += selectedMesh.triangleAreas[i];
                if (meshRandomArea <= triangleAreaSum)
                {
                    triangleIndex = i;
                    break;
                }
            }

            if (triangleIndex < 0)
                return false;

            // Get triangle vertices
            int i0 = selectedMesh.triangles[triangleIndex * 3];
            int i1 = selectedMesh.triangles[triangleIndex * 3 + 1];
            int i2 = selectedMesh.triangles[triangleIndex * 3 + 2];

            Vector3 v0 = selectedMesh.vertices[i0];
            Vector3 v1 = selectedMesh.vertices[i1];
            Vector3 v2 = selectedMesh.vertices[i2];

            // Get vertex colors if available
            if (selectedMesh.colors != null && selectedMesh.colors.Length > 0)
            {
                Color c0 = (i0 < selectedMesh.colors.Length) ? selectedMesh.colors[i0] : Color.white;
                Color c1 = (i1 < selectedMesh.colors.Length) ? selectedMesh.colors[i1] : Color.white;
                Color c2 = (i2 < selectedMesh.colors.Length) ? selectedMesh.colors[i2] : Color.white;
                vertexColor = c0; // Simplified - could interpolate based on barycentric coords
            }

            // Get normals
            Vector3 n0 = (selectedMesh.normals != null && selectedMesh.normals.Length > i0) ? 
                selectedMesh.normals[i0] : Vector3.up;
            Vector3 n1 = (selectedMesh.normals != null && selectedMesh.normals.Length > i1) ? 
                selectedMesh.normals[i1] : Vector3.up;
            Vector3 n2 = (selectedMesh.normals != null && selectedMesh.normals.Length > i2) ? 
                selectedMesh.normals[i2] : Vector3.up;

            // Generate random barycentric coordinates
            float r1 = Mathf.Sqrt((float)random.NextDouble());
            float r2 = (float)random.NextDouble();
            
            // Calculate position using barycentric coordinates
            Vector3 localPos = (1 - r1) * v0 + r1 * (1 - r2) * v1 + r1 * r2 * v2;
            position = selectedMesh.transform.TransformPoint(localPos);
            
            // Calculate interpolated normal
            Vector3 localNormal = (1 - r1) * n0 + r1 * (1 - r2) * n1 + r1 * r2 * n2;
            normal = selectedMesh.transform.TransformDirection(localNormal).normalized;

            return true;
        }

        /// <summary>
        /// Check if a point passes all filters
        /// </summary>
        private bool PassesAllFilters(Vector3 position, Vector3 normal, Color vertexColor, List<PlacementData> existingPlacements)
        {
            // Layer mask check
            Ray ray = new Ray(position + normal * 0.1f, -normal);
            if (!Physics.Raycast(ray, out RaycastHit hit, 0.2f, settings.allowedLayerMask))
                return false;

            // Block layer check
            if (settings.blockLayerMask != 0 && 
                Physics.CheckSphere(position, settings.collisionRadius, settings.blockLayerMask))
                return false;

            // Slope filter
            if (settings.slopeFilter.enabled)
            {
                float angle = Vector3.Angle(normal, Vector3.up);
                if (angle < settings.slopeFilter.minAngle || angle > settings.slopeFilter.maxAngle)
                    return false;
            }

            // Altitude filter
            if (settings.altitudeFilter.enabled)
            {
                if (position.y < settings.altitudeFilter.minHeight || 
                    position.y > settings.altitudeFilter.maxHeight)
                    return false;
            }

            // Proximity filter
            if (settings.proximityFilter.enabled && settings.proximityFilter.minDistance > 0)
            {
                foreach (var pos in placedPositions)
                {
                    float distance = Vector3.Distance(position, pos);
                    if (distance < settings.proximityFilter.minDistance)
                        return false;
                }

                if (settings.proximityFilter.maxDistance > 0)
                {
                    bool hasNearbyObject = false;
                    foreach (var pos in placedPositions)
                    {
                        float distance = Vector3.Distance(position, pos);
                        if (distance <= settings.proximityFilter.maxDistance)
                        {
                            hasNearbyObject = true;
                            break;
                        }
                    }
                    
                    if (!hasNearbyObject && placedPositions.Count > 0)
                        return false;
                }
            }

            // Spline filter
            if (settings.splineFilter.enabled && settings.splineFilter.splineObjects.Count > 0)
            {
                bool nearSpline = false;
                foreach (var splineObj in settings.splineFilter.splineObjects)
                {
                    if (splineObj == null) continue;
                    
                    var spline = splineObj.GetComponent<SplineContainer>();
                    if (spline == null) continue;

                    float closestDistance = float.MaxValue;
                    foreach (var knot in spline.Spline.Knots)
                    {
                        float distance = Vector3.Distance(
                            position, 
                            splineObj.transform.TransformPoint(knot.Position)
                        );
                        closestDistance = Mathf.Min(closestDistance, distance);
                    }

                    if (closestDistance < settings.splineFilter.distance)
                    {
                        nearSpline = true;
                        break;
                    }
                }

                bool shouldBeNear = settings.splineFilter.mode == PCGSettings.SplineFilter.SplineMode.Within;
                if (nearSpline != shouldBeNear)
                    return false;
            }

            // Vertex color filter
            if (settings.vertexColorFilter.enabled)
            {
                float value = 0;
                switch (settings.vertexColorFilter.channel)
                {
                    case PCGSettings.VertexColorFilter.ColorChannel.Red: value = vertexColor.r; break;
                    case PCGSettings.VertexColorFilter.ColorChannel.Green: value = vertexColor.g; break;
                    case PCGSettings.VertexColorFilter.ColorChannel.Blue: value = vertexColor.b; break;
                    case PCGSettings.VertexColorFilter.ColorChannel.Alpha: value = vertexColor.a; break;
                }

                bool passes = value >= settings.vertexColorFilter.threshold;
                if (settings.vertexColorFilter.invert)
                    passes = !passes;

                if (!passes)
                    return false;
            }

            // Mask texture filter
            if (settings.maskFilter.enabled && settings.maskFilter.maskTexture != null)
            {
                // This is a simplified implementation - in a real tool you'd need proper UV mapping
                // For now, we'll use world XZ coordinates mapped to 0-1 range based on bounds
                Bounds bounds = CalculateBounds();
                float u = Mathf.InverseLerp(bounds.min.x, bounds.max.x, position.x);
                float v = Mathf.InverseLerp(bounds.min.z, bounds.max.z, position.z);

                Color pixelColor = settings.maskFilter.maskTexture.GetPixelBilinear(u, v);
                float value = 0;
                
                switch (settings.maskFilter.channel)
                {
                    case PCGSettings.MaskFilter.MaskChannel.Red: value = pixelColor.r; break;
                    case PCGSettings.MaskFilter.MaskChannel.Green: value = pixelColor.g; break;
                    case PCGSettings.MaskFilter.MaskChannel.Blue: value = pixelColor.b; break;
                    case PCGSettings.MaskFilter.MaskChannel.Alpha: value = pixelColor.a; break;
                }

                bool passes = value >= settings.maskFilter.threshold;
                if (settings.maskFilter.invert)
                    passes = !passes;

                if (!passes)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Calculate bounds of all target meshes
        /// </summary>
        private Bounds CalculateBounds()
        {
            Bounds bounds = new Bounds();
            bool initialized = false;

            foreach (var mesh in meshCaches)
            {
                if (!initialized)
                {
                    bounds = new Bounds(mesh.transform.TransformPoint(mesh.mesh.bounds.center), Vector3.zero);
                    initialized = true;
                }

                Bounds meshBounds = mesh.mesh.bounds;
                Vector3 min = mesh.transform.TransformPoint(meshBounds.min);
                Vector3 max = mesh.transform.TransformPoint(meshBounds.max);

                bounds.Encapsulate(min);
                bounds.Encapsulate(max);
            }

            return bounds;
        }

        /// <summary>
        /// Select a prefab index based on weights
        /// </summary>
        private int SelectPrefabIndex(float[] cumulativeWeights)
        {
            float value = (float)random.NextDouble();
            
            for (int i = 0; i < cumulativeWeights.Length; i++)
            {
                if (value <= cumulativeWeights[i])
                    return i;
            }
            
            return cumulativeWeights.Length - 1;
        }

        /// <summary>
        /// Generate a random float between min and max
        /// </summary>
        private float RandomRange(float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }
    }
}