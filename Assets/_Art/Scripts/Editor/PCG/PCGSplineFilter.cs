using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Splines;

namespace PCG
{
    /// <summary>
    /// Provides advanced spline-based filtering and visualization for the PCG tool.
    /// </summary>
    public class PCGSplineFilter
    {
        private PCGSettings settings;
        private List<SplineContainer> cachedSplines = new List<SplineContainer>();
        private bool showSplineVisualization = true;
        
        // Visualization settings
        private Color splineColor = new Color(0.2f, 0.6f, 1.0f, 0.8f);
        private Color influenceColor = new Color(0.2f, 0.6f, 1.0f, 0.2f);
        
        /// <summary>
        /// Initialize the spline filter with settings
        /// </summary>
        public PCGSplineFilter(PCGSettings settings)
        {
            this.settings = settings;
            CacheSplines();
        }
        
        /// <summary>
        /// Cache spline data for faster processing
        /// </summary>
        public void CacheSplines()
        {
            cachedSplines.Clear();
            
            if (settings.splineFilter.splineObjects == null)
                return;
                
            foreach (var splineObj in settings.splineFilter.splineObjects)
            {
                if (splineObj == null)
                    continue;
                    
                var splineContainer = splineObj.GetComponent<SplineContainer>();
                if (splineContainer != null)
                {
                    cachedSplines.Add(splineContainer);
                }
            }
        }
        
        /// <summary>
        /// Check if a point passes the spline filter
        /// </summary>
        public bool PassesFilter(Vector3 position)
        {
            if (!settings.splineFilter.enabled || cachedSplines.Count == 0)
                return true;
                
            bool nearSpline = false;
            float closestDistance = float.MaxValue;
            
            foreach (var splineContainer in cachedSplines)
            {
                if (splineContainer == null)
                    continue;
                    
                // Find the closest point on the spline
                float distance = GetDistanceToSpline(position, splineContainer);
                closestDistance = Mathf.Min(closestDistance, distance);
                
                if (distance < settings.splineFilter.distance)
                {
                    nearSpline = true;
                    break;
                }
            }
            
            // Check if the point should be within or beyond the spline influence
            bool shouldBeNear = settings.splineFilter.mode == PCGSettings.SplineFilter.SplineMode.Within;
            return nearSpline == shouldBeNear;
        }
        
        /// <summary>
        /// Get the distance from a point to a spline
        /// </summary>
        private float GetDistanceToSpline(Vector3 position, SplineContainer splineContainer)
        {
            float closestDistance = float.MaxValue;
            
            // Simple implementation: check distance to each knot
            // A more accurate implementation would find the closest point on the spline curve
            foreach (var knot in splineContainer.Spline.Knots)
            {
                Vector3 knotPosition = splineContainer.transform.TransformPoint(knot.Position);
                float distance = Vector3.Distance(position, knotPosition);
                closestDistance = Mathf.Min(closestDistance, distance);
            }
            
            return closestDistance;
        }
        
        /// <summary>
        /// Draw spline visualizations in the scene view
        /// </summary>
        public void OnSceneGUI(SceneView sceneView)
        {
            if (!showSplineVisualization || !settings.splineFilter.enabled || cachedSplines.Count == 0)
                return;
                
            foreach (var splineContainer in cachedSplines)
            {
                if (splineContainer == null)
                    continue;
                    
                DrawSpline(splineContainer);
                DrawSplineInfluence(splineContainer); 
            }
        }
        
        /// <summary>
        /// Draw the spline curve
        /// </summary>
        private void DrawSpline(SplineContainer splineContainer)
        {
            List<BezierKnot> knots = splineContainer.Spline.Knots.ToList();
        }
        
        /// <summary>
        /// Draw the spline influence area
        /// </summary>
        private void DrawSplineInfluence(SplineContainer splineContainer)
        {
            Handles.color = influenceColor;
            
            // Draw influence area around each knot
            foreach (var knot in splineContainer.Spline.Knots)
            {
                Vector3 position = splineContainer.transform.TransformPoint(knot.Position);
                
                // Draw a disc showing the influence radius
                Handles.DrawSolidDisc(
                    position,
                    Vector3.up,
                    settings.splineFilter.distance
                );
            }
            
            // For a more accurate visualization, we should draw discs along the entire curve
            // This is a simplified visualization just at the knot points
        }
        
        /// <summary>
        /// Toggle spline visualization
        /// </summary>
        public void ToggleVisualization(bool show)
        {
            showSplineVisualization = show;
            SceneView.RepaintAll();
        }
        
        /// <summary>
        /// Create a new spline at the specified position
        /// </summary>
        public GameObject CreateSplineAtPosition(Vector3 position)
        {
            GameObject splineObject = new GameObject("PCG_Spline");
            splineObject.transform.position = position;
            
            SplineContainer splineContainer = splineObject.AddComponent<SplineContainer>();
            
            // Add the spline to the filter
            if (settings.splineFilter.splineObjects == null)
            {
                settings.splineFilter.splineObjects = new List<GameObject>();
            }
            
            settings.splineFilter.splineObjects.Add(splineObject);
            CacheSplines();
            
            return splineObject;
        }
        
        /// <summary>
        /// Add a knot to the specified spline at the given position
        /// </summary>
        public void AddKnotToSpline(SplineContainer splineContainer, Vector3 position)
        {
            if (splineContainer == null)
                return;
                
            // Convert position to local space
            Vector3 localPosition = splineContainer.transform.InverseTransformPoint(position);
            
            // Create a new knot
            BezierKnot knot = new BezierKnot(localPosition);
            
            // Add the knot to the spline
            var spline = splineContainer.Spline;
            spline.Add(knot);
            
            // Update the spline
            EditorUtility.SetDirty(splineContainer);
        }
        
        /// <summary>
        /// Remove the specified spline from the filter
        /// </summary>
        public void RemoveSpline(SplineContainer splineContainer)
        {
            if (splineContainer == null || settings.splineFilter.splineObjects == null)
                return;
                
            settings.splineFilter.splineObjects.Remove(splineContainer.gameObject);
            CacheSplines();
        }
        
        /// <summary>
        /// Get all cached splines
        /// </summary>
        public List<SplineContainer> GetCachedSplines()
        {
            return new List<SplineContainer>(cachedSplines);
        }
    }
}