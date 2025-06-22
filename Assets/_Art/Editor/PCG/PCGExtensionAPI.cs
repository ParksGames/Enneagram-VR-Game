using UnityEngine;
using System;
using System.Collections.Generic;

namespace PCG
{
    /// <summary>
    /// Provides an API for custom placement logic and extensions to the PCG tool.
    /// </summary>
    public static class PCGExtensionAPI
    {
        /// <summary>
        /// Delegate for custom placement validation
        /// </summary>
        /// <param name="position">The position to validate</param>
        /// <param name="normal">The surface normal at the position</param>
        /// <param name="prefab">The prefab that would be placed</param>
        /// <param name="settings">The current PCG settings</param>
        /// <returns>True if the placement is valid, false otherwise</returns>
        public delegate bool PlacementValidationDelegate(Vector3 position, Vector3 normal, GameObject prefab, PCGSettings settings);
        
        /// <summary>
        /// Delegate for custom placement transformation
        /// </summary>
        /// <param name="data">The placement data to transform</param>
        /// <param name="settings">The current PCG settings</param>
        /// <returns>The transformed placement data</returns>
        public delegate PCGPlacementEngine.PlacementData PlacementTransformDelegate(PCGPlacementEngine.PlacementData data, PCGSettings settings);
        
        /// <summary>
        /// Delegate for post-processing placed objects
        /// </summary>
        /// <param name="placedObject">The instantiated GameObject</param>
        /// <param name="originalData">The original placement data</param>
        /// <param name="settings">The current PCG settings</param>
        public delegate void PostProcessDelegate(GameObject placedObject, PCGPlacementEngine.PlacementData originalData, PCGSettings settings);
        
        // Lists of registered callbacks
        private static List<PlacementValidationDelegate> validationCallbacks = new List<PlacementValidationDelegate>();
        private static List<PlacementTransformDelegate> transformCallbacks = new List<PlacementTransformDelegate>();
        private static List<PostProcessDelegate> postProcessCallbacks = new List<PostProcessDelegate>();
        
        /// <summary>
        /// Register a custom placement validation callback
        /// </summary>
        public static void RegisterValidationCallback(PlacementValidationDelegate callback)
        {
            if (callback != null && !validationCallbacks.Contains(callback))
            {
                validationCallbacks.Add(callback);
            }
        }
        
        /// <summary>
        /// Unregister a custom placement validation callback
        /// </summary>
        public static void UnregisterValidationCallback(PlacementValidationDelegate callback)
        {
            if (callback != null)
            {
                validationCallbacks.Remove(callback);
            }
        }
        
        /// <summary>
        /// Register a custom placement transformation callback
        /// </summary>
        public static void RegisterTransformCallback(PlacementTransformDelegate callback)
        {
            if (callback != null && !transformCallbacks.Contains(callback))
            {
                transformCallbacks.Add(callback);
            }
        }
        
        /// <summary>
        /// Unregister a custom placement transformation callback
        /// </summary>
        public static void UnregisterTransformCallback(PlacementTransformDelegate callback)
        {
            if (callback != null)
            {
                transformCallbacks.Remove(callback);
            }
        }
        
        /// <summary>
        /// Register a custom post-processing callback
        /// </summary>
        public static void RegisterPostProcessCallback(PostProcessDelegate callback)
        {
            if (callback != null && !postProcessCallbacks.Contains(callback))
            {
                postProcessCallbacks.Add(callback);
            }
        }
        
        /// <summary>
        /// Unregister a custom post-processing callback
        /// </summary>
        public static void UnregisterPostProcessCallback(PostProcessDelegate callback)
        {
            if (callback != null)
            {
                postProcessCallbacks.Remove(callback);
            }
        }
        
        /// <summary>
        /// Clear all registered callbacks
        /// </summary>
        public static void ClearAllCallbacks()
        {
            validationCallbacks.Clear();
            transformCallbacks.Clear();
            postProcessCallbacks.Clear();
        }
        
        /// <summary>
        /// Validate a placement using all registered validation callbacks
        /// </summary>
        /// <returns>True if all callbacks validate the placement, false otherwise</returns>
        public static bool ValidatePlacement(Vector3 position, Vector3 normal, GameObject prefab, PCGSettings settings)
        {
            foreach (var callback in validationCallbacks)
            {
                if (!callback(position, normal, prefab, settings))
                {
                    return false;
                }
            }
            return true;
        }
        
        /// <summary>
        /// Transform a placement using all registered transformation callbacks
        /// </summary>
        public static PCGPlacementEngine.PlacementData TransformPlacement(PCGPlacementEngine.PlacementData data, PCGSettings settings)
        {
            PCGPlacementEngine.PlacementData result = data;
            foreach (var callback in transformCallbacks)
            {
                result = callback(result, settings);
            }
            return result;
        }
        
        /// <summary>
        /// Post-process a placed object using all registered post-processing callbacks
        /// </summary>
        public static void PostProcessObject(GameObject placedObject, PCGPlacementEngine.PlacementData originalData, PCGSettings settings)
        {
            foreach (var callback in postProcessCallbacks)
            {
                callback(placedObject, originalData, settings);
            }
        }
        
        /// <summary>
        /// Example custom filter: Material-based filter
        /// </summary>
        public static bool MaterialFilter(Vector3 position, Vector3 normal, string materialNameContains)
        {
            // Cast a ray to find what material we're placing on
            Ray ray = new Ray(position + normal * 0.1f, -normal);
            if (Physics.Raycast(ray, out RaycastHit hit, 0.2f))
            {
                Renderer renderer = hit.collider.GetComponent<Renderer>();
                if (renderer != null && renderer.sharedMaterial != null)
                {
                    return renderer.sharedMaterial.name.Contains(materialNameContains);
                }
            }
            return false;
        }
        
        /// <summary>
        /// Example custom filter: Time-based filter (useful for runtime PCG)
        /// </summary>
        public static bool TimeOfDayFilter(float minHour, float maxHour)
        {
            // This would connect to a day/night system
            // For this example, we'll just use real time
            float currentHour = DateTime.Now.Hour + DateTime.Now.Minute / 60f;
            return currentHour >= minHour && currentHour <= maxHour;
        }
        
        /// <summary>
        /// Example custom transformation: Align to water surface
        /// </summary>
        public static PCGPlacementEngine.PlacementData AlignToWaterSurface(PCGPlacementEngine.PlacementData data, float waterHeight)
        {
            // If the object is below the water level, move it up and align it to the water surface
            if (data.position.y < waterHeight)
            {
                PCGPlacementEngine.PlacementData result = data;
                result.position.y = waterHeight;
                result.rotation = Quaternion.FromToRotation(Vector3.up, Vector3.up) * 
                                 Quaternion.Euler(0, data.rotation.eulerAngles.y, 0);
                return result;
            }
            return data;
        }
        
        /// <summary>
        /// Example custom post-processing: Add floating behavior to objects in water
        /// </summary>
        public static void AddFloatingBehavior(GameObject placedObject, float waterHeight)
        {
            if (placedObject.transform.position.y <= waterHeight)
            {
                // This would add a floating script to objects in water
                // For this example, we'll just add a tag
                placedObject.tag = "Floating";
                
                // In a real implementation, you might add a component:
                // placedObject.AddComponent<FloatingObject>();
            }
        }
        
        /// <summary>
        /// Store custom data in the PCGSettings extensionData field
        /// </summary>
        public static void StoreCustomData<T>(PCGSettings settings, string key, T data)
        {
            if (settings == null)
                return;
                
            // Create a simple key-value storage in JSON format
            Dictionary<string, string> extensionData;
            
            if (string.IsNullOrEmpty(settings.extensionData))
            {
                extensionData = new Dictionary<string, string>();
            }
            else
            {
                try
                {
                    extensionData = JsonUtility.FromJson<Dictionary<string, string>>(settings.extensionData);
                }
                catch
                {
                    extensionData = new Dictionary<string, string>();
                }
            }
            
            // Store the serialized data
            extensionData[key] = JsonUtility.ToJson(data);
            
            // Save back to settings
            settings.extensionData = JsonUtility.ToJson(extensionData);
        }
        
        /// <summary>
        /// Retrieve custom data from the PCGSettings extensionData field
        /// </summary>
        public static T GetCustomData<T>(PCGSettings settings, string key, T defaultValue = default)
        {
            if (settings == null || string.IsNullOrEmpty(settings.extensionData))
                return defaultValue;
                
            try
            {
                Dictionary<string, string> extensionData = JsonUtility.FromJson<Dictionary<string, string>>(settings.extensionData);
                if (extensionData.TryGetValue(key, out string json))
                {
                    return JsonUtility.FromJson<T>(json);
                }
            }
            catch
            {
                Debug.LogWarning("Failed to retrieve custom data for key: " + key);
            }
            
            return defaultValue;
        }
    }
}