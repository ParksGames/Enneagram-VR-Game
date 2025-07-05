// ChannelMaskProjectorTool.cs
// This Editor tool projects sprites onto a mesh and bakes them into separate RGBA channels based on tag or layer.
// Place the shader code below into: Assets/Shaders/Hidden/ChannelMaskProjector.shader

using UnityEngine;
using UnityEditor;
using System.IO;

public class ChannelMaskProjectorWindow1 : EditorWindow {
    [MenuItem("Pause9/Channel Mask Projector")]
    public static void ShowWindow() {
        GetWindow<ChannelMaskProjectorWindow1>("Channel Mask Projector");
    }

    enum FilterMode { Tag, Layer }
    enum Channel { R = 0, G = 1, B = 2, A = 3 }

    class ChannelSetting {
        public FilterMode mode = FilterMode.Tag;
        public string tag = "Untagged";
        public LayerMask layerMask = ~0;
    }

    ChannelSetting[] channels = new ChannelSetting[4];
    int resolution = 1024;
    GameObject targetObject;
    string savePath = "Assets/MaskTexture.png";
    Material projectorMat;

    void OnEnable() {
        for (int i = 0; i < 4; i++) channels[i] = new ChannelSetting();
        Shader sh = Shader.Find("Hidden/ChannelMaskProjector");
        if (sh != null) projectorMat = new Material(sh);
    }

    void OnGUI() {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("1. Select Target Mesh", EditorStyles.boldLabel);
        targetObject = (GameObject)EditorGUILayout.ObjectField("Mesh Object", targetObject, typeof(GameObject), true);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("2. Resolution & Save Path", EditorStyles.boldLabel);
        resolution = EditorGUILayout.IntPopup("Resolution", resolution,
            new string[]{"512","1024","2048","4096"}, new int[]{512,1024,2048,4096});
        savePath = EditorGUILayout.TextField("Save Path", savePath);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("3. Channel Settings", EditorStyles.boldLabel);
        string[] channelLabels = {"Red (R)","Green (G)","Blue (B)","Alpha (A)"};
        for (int i = 0; i < 4; i++) {
            var ch = channels[i];
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(channelLabels[i], EditorStyles.boldLabel);
            ch.mode = (FilterMode)EditorGUILayout.EnumPopup("Filter By", ch.mode);
            if (ch.mode == FilterMode.Tag) ch.tag = EditorGUILayout.TagField("Tag", ch.tag);
            else ch.layerMask = EditorGUILayout.MaskField("LayerMask",
                LayerMaskToFieldIndex(ch.layerMask), UnityEditorInternal.InternalEditorUtility.layers);
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Bake Masks")) {
            if (targetObject == null) {
                EditorUtility.DisplayDialog("Error","Assign a target mesh!","OK");
            } else BakeMasks();
        }
    }

    int LayerMaskToFieldIndex(LayerMask mask) {
        for (int i = 0; i < 32; i++) if ((mask & (1 << i)) != 0) return i;
        return 0;
    }

    void BakeMasks() {
        // Setup RenderTextures
        var targetRT = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.ARGB32);
        var accumRT = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.ARGB32);
        Graphics.SetRenderTarget(accumRT);
        GL.Clear(true, true, Color.clear);
        Graphics.SetRenderTarget(null);

        // Create projection camera
        var camGO = new GameObject("MaskCam");
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.Color;
        cam.backgroundColor = Color.clear;
        cam.targetTexture = targetRT;
        cam.enabled = false;

        // Compute bounds and setup camera
        var rend = targetObject.GetComponent<Renderer>();
        Bounds b = rend.bounds;
        cam.transform.position = b.center + Vector3.back * (b.extents.magnitude + 1);
        cam.orthographicSize = Mathf.Max(b.extents.x, b.extents.y, b.extents.z);

// Process each channel
        for (int i = 0; i < channels.Length; i++) {
            var ch = channels[i];
    
            // Find sprite renderer by tag
            SpriteRenderer spriteRenderer = null;
            if (ch.mode == FilterMode.Tag) {
                GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(ch.tag);
                foreach(GameObject obj in taggedObjects) {
                    SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
                    if(sr != null) {
                        spriteRenderer = sr;
                        break;
                    }
                }
            }
    
            if (spriteRenderer == null || spriteRenderer.sprite == null) continue;

            // Set culling
            if (ch.mode == FilterMode.Tag) cam.cullingMask = 1 << LayerMask.NameToLayer(ch.tag);
            else cam.cullingMask = ch.layerMask;

            // Assign sprite to material 
            projectorMat.SetTexture("_ProjTex", spriteRenderer.sprite.texture);
            projectorMat.SetMatrix("_ProjMatrix", cam.projectionMatrix * cam.worldToCameraMatrix);

            // Render to targetRT
            cam.SetReplacementShader(projectorMat.shader, null);
            cam.Render();

            // Blend into accumRT only into the selected channel
            Graphics.Blit(targetRT, accumRT, projectorMat, i);
        }

        // Read back and save
        RenderTexture.active = accumRT;
        Texture2D outTex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        outTex.ReadPixels(new Rect(0,0,resolution,resolution),0,0);
        outTex.Apply();
        RenderTexture.active = null;

        // Write PNG
        File.WriteAllBytes(savePath, outTex.EncodeToPNG());
        AssetDatabase.Refresh();

        // Cleanup
        DestroyImmediate(camGO);
        targetRT.Release(); accumRT.Release();

        EditorUtility.DisplayDialog("Done", $"Mask saved to {savePath}", "OK");
    }
}