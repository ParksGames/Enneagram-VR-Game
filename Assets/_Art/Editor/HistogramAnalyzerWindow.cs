


using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.EditorCoroutines.Editor;
using System.Collections;
using System.Collections.Generic;
using System.IO;


[System.Serializable]
public class QCPreset : ScriptableObject {
    public int bins;
    public float scale;
    public float exposure;
    public float contrast;
    public float saturation;
    public AnimationCurve curveR = AnimationCurve.Linear(0,0,1,1);
    public AnimationCurve curveG = AnimationCurve.Linear(0,0,1,1);
    public AnimationCurve curveB = AnimationCurve.Linear(0,0,1,1);
}



[Serializable]
public struct HistogramSnapshot {
    public int[] r;
    public int[] g;
    public int[] b;
    public float exposure;
    public float contrast;
    public float saturation;
    public DateTime time;
}
public class AdvancedHistogramQCWindow : EditorWindow
{

    [SerializeField] Camera        targetCamera;
    [SerializeField] Texture2D     referenceImage;
    [SerializeField] Volume        postProcessVolume;
    [SerializeField] ComputeShader histogramComputeShader;
    [SerializeField] bool          useGPUCompute = false;
    [SerializeField] int           bins = 16;
    [SerializeField] float         scale = 1f;


    [SerializeField] List<QCPreset> presets = new List<QCPreset>();

    private int[] gameR, gameG, gameB, refR, refG, refB;
    private float contrastGame, contrastRef, satGame, satRef, expGame, expRef;
    private float deltaEAvg;
    private Texture2D diffMap;
    private bool clipShadowGame, clipHighlightGame, clipShadowRef, clipHighlightRef;

    private List<HistogramSnapshot> history = new List<HistogramSnapshot>();
    private int compareIndex = -1;

    private int selectedTab = 0;
    private static readonly string[] tabNames = new[] {"Overview","Waveform","DeltaE","DiffMap","Grading","Presets","VersionDiff"};

    [MenuItem("CT/Advanced Histogram QC")]
    public static void ShowWindow() {
        GetWindow<AdvancedHistogramQCWindow>("Advanced QC");
    }
    
    void OnGUI() {

        selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
        EditorGUILayout.Space();

        DrawSettings();
        EditorGUILayout.Space();

        if (GUILayout.Button("Capture & Analyze")) {
            EditorCoroutineUtility.StartCoroutineOwnerless(CaptureAndAnalyze());
        }

        EditorGUILayout.Space();
        DrawTabContent();
    }

    void DrawSettings() {
        EditorGUILayout.LabelField("Capture Settings", EditorStyles.boldLabel);
        targetCamera       = (Camera)EditorGUILayout.ObjectField("Target Camera", targetCamera, typeof(Camera), true);
        referenceImage     = (Texture2D)EditorGUILayout.ObjectField("Reference Image", referenceImage, typeof(Texture2D), false);
        postProcessVolume  = (Volume)EditorGUILayout.ObjectField("Post-Process Volume", postProcessVolume, typeof(Volume), true);
        useGPUCompute      = EditorGUILayout.Toggle("Use GPU Compute", useGPUCompute);
        if (useGPUCompute) {
            histogramComputeShader = (ComputeShader)EditorGUILayout.ObjectField("Histogram ComputeShader", histogramComputeShader, typeof(ComputeShader), false);
        }
        bins = EditorGUILayout.IntSlider("Bins", bins, 2, 256);
        scale = EditorGUILayout.FloatField("Scale", scale);
    }
    
    void DrawTabContent() {
        switch (selectedTab) {
            case 0: DrawOverview(); break;
            case 1: DrawWaveform(); break;
            case 2: DrawDeltaE(); break;
            case 3: DrawDiffMap(); break;

            case 5: DrawPresets(); break;
            case 6: DrawVersionDiff(); break;
        }
    }
    
    IEnumerator CaptureAndAnalyze() {

        const int TEX = 256;
        var rt = new RenderTexture(TEX, TEX, 0, RenderTextureFormat.ARGB32);
        if (useGPUCompute) rt.enableRandomWrite = true;
        rt.Create();

        targetCamera.targetTexture = rt;
        targetCamera.Render();
        RenderTexture.active = rt;


        if (useGPUCompute && histogramComputeShader != null) {
            RunGPUHistogram(rt);
        } else {
            var snap = new Texture2D(TEX, TEX, TextureFormat.RGBA32, false);
            snap.ReadPixels(new Rect(0, 0, TEX, TEX), 0, 0);
            snap.Apply();
            ComputeCPU(snap, out gameR, out gameG, out gameB, out contrastGame, out satGame, out expGame, out clipShadowGame, out clipHighlightGame);
            ComputeCPU(referenceImage, out refR, out refG, out refB, out contrastRef, out satRef, out expRef, out clipShadowRef, out clipHighlightRef);
            deltaEAvg = ComputeDeltaE(snap, referenceImage);
            diffMap   = GenerateDiffMap(snap, referenceImage, 128, 128);
            SaveSnapshot();
            DestroyImmediate(snap);
        }

        targetCamera.targetTexture = null;
        RenderTexture.active      = null;
        DestroyImmediate(rt);
        yield return null;
    }    
    void RunGPUHistogram(RenderTexture rt) {
        int kernel = histogramComputeShader.FindKernel("CSMain");
        int threadGroups = Mathf.CeilToInt(rt.width / 8f);
        var bufR = new ComputeBuffer(bins, sizeof(uint));
        var bufG = new ComputeBuffer(bins, sizeof(uint));
        var bufB = new ComputeBuffer(bins, sizeof(uint));

        histogramComputeShader.SetBuffer(kernel, "ResultR", bufR);
        histogramComputeShader.SetBuffer(kernel, "ResultG", bufG);
        histogramComputeShader.SetBuffer(kernel, "ResultB", bufB);
        histogramComputeShader.SetTexture(kernel, "_InputTex", rt);
        histogramComputeShader.SetInt("_Bins", bins);

        histogramComputeShader.Dispatch(kernel, threadGroups, threadGroups, 1);

        uint[] histR = new uint[bins], histG = new uint[bins], histB = new uint[bins];
        bufR.GetData(histR);
        bufG.GetData(histG);
        bufB.GetData(histB);

        gameR = Array.ConvertAll(histR, i => (int)i);
        gameG = Array.ConvertAll(histG, i => (int)i);
        gameB = Array.ConvertAll(histB, i => (int)i);


        var snapRef = referenceImage;
        var tmp = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        Graphics.CopyTexture(snapRef, tmp);
        ComputeCPU(tmp, out refR, out refG, out refB, out contrastRef, out satRef, out expRef, out clipShadowRef, out clipHighlightRef);
        deltaEAvg = ComputeDeltaE(tmp, referenceImage);
        diffMap = GenerateDiffMap(tmp, referenceImage, 128, 128);
        SaveSnapshot();

        bufR.Release(); bufG.Release(); bufB.Release();
        DestroyImmediate(tmp);
    }
    void DrawOverview() {
        EditorGUILayout.LabelField("Metrics", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Contrast — Game: {contrastGame:F2}   Ref: {contrastRef:F2}");
        EditorGUILayout.LabelField($"Saturation — Game: {satGame:F2}   Ref: {satRef:F2}");
        EditorGUILayout.LabelField($"Exposure — Game: {expGame:F2}   Ref: {expRef:F2}");
        EditorGUILayout.Space();
        DrawGradingControls();
        if (clipShadowGame)   EditorGUILayout.HelpBox("Game shadows are clipped", MessageType.Warning);
        if (clipHighlightGame)EditorGUILayout.HelpBox("Game highlights are clipped", MessageType.Warning);


        Rect rect = GUILayoutUtility.GetRect(position.width, 200);
        if (Event.current.type == EventType.Repaint)
            DrawHistogramInto(rect, gameR, gameG, gameB, refR, refG, refB);
    }

    void DrawWaveform() {
        EditorGUILayout.LabelField("Waveform (Luma) & RGB Parade", EditorStyles.boldLabel);
        Rect rect = GUILayoutUtility.GetRect(position.width, 200);
        if (Event.current.type == EventType.Repaint)
            DrawHistogramInto(rect, gameR, gameG, gameB, refR, refG, refB);
    }

    void DrawDeltaE() {
        EditorGUILayout.LabelField("Perceptual ΔE (CIE76)", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Avg ΔE: {deltaEAvg:F2}");
    }

    void DrawDiffMap() {
        EditorGUILayout.LabelField("Difference Map", EditorStyles.boldLabel);
        if (diffMap != null) GUILayout.Label(diffMap);
    }
    void DrawGradingControls() {
        EditorGUILayout.LabelField("Interactive Grading & Auto-Match", EditorStyles.boldLabel);
        if (!postProcessVolume.profile.TryGet<ColorAdjustments>(out var ca)) {
            EditorGUILayout.HelpBox("Add ColorAdjustments to Volume profile", MessageType.Error);
        } else {

            ca.postExposure.Override(EditorGUILayout.Slider("Exposure", ca.postExposure.value, -5f, 5f));
            ca.contrast.Override    (EditorGUILayout.Slider("Contrast",   ca.contrast.value,   -100f,100f));
            ca.saturation.Override  (EditorGUILayout.Slider("Saturation", ca.saturation.value, -100f,100f));
            EditorGUILayout.Space();

            if (GUILayout.Button("Auto-Match Color Adjustments")) {
                AutoMatchColorAdjustments(ca);
            }
            EditorGUILayout.Space();
            if (GUILayout.Button("Export 3D LUT")) Export3DLUT("Assets/gradeLUT.asset", 16);
        }
    }
    
    void DrawPresets() {
        EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);
        for (int i = 0; i < presets.Count; i++) {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(presets[i].name)) ApplyPreset(presets[i]);
            if (GUILayout.Button("X", GUILayout.Width(20))) { presets.RemoveAt(i); break; }
            EditorGUILayout.EndHorizontal();
        }
        if (GUILayout.Button("Save Current as Preset")) CreatePreset();
    }
    void DrawVersionDiff() {
        EditorGUILayout.LabelField("Version Diff", EditorStyles.boldLabel);
        if (history.Count < 2) {
            EditorGUILayout.HelpBox("Need at least two snapshots for comparison", MessageType.Info);
            return;
        }
        compareIndex = EditorGUILayout.Popup("Compare to", compareIndex,
            history.ConvertAll(h => h.time.ToString()).ToArray());
        if (compareIndex >= 0 && compareIndex < history.Count) {
            var h0 = history[compareIndex];
            Rect rect = GUILayoutUtility.GetRect(position.width, 200);
            if (Event.current.type == EventType.Repaint)
                DrawHistogramInto(rect, gameR, gameG, gameB, h0.r, h0.g, h0.b);
        }
    }
    
    void AutoMatchColorAdjustments(ColorAdjustments ca) {
        Undo.RecordObject(postProcessVolume.profile, "Auto-Match CC");
        float deltaExp  = expRef   - expGame;
        float deltaContr= (contrastRef - contrastGame) * 100f;
        float deltaSat  = (satRef       - satGame)       * 100f;
        ca.postExposure.Override(Mathf.Clamp(deltaExp,   -5f, 5f));
        ca.contrast.Override    (Mathf.Clamp(deltaContr, -100f,100f));
        ca.saturation.Override  (Mathf.Clamp(deltaSat,   -100f,100f));
        EditorUtility.SetDirty(postProcessVolume.profile);
    }
    
    void ComputeCPU(Texture2D tex, out int[] hR, out int[] hG, out int[] hB,
                    out float contrast, out float avgSat, out float avgExp,
                    out bool clipSh, out bool clipHi)
    {
        hR = new int[bins]; hG = new int[bins]; hB = new int[bins];
        float sumL = 0, sumL2 = 0, sumS = 0, sumV = 0; var px = tex.GetPixels(); int total = px.Length;
        clipSh = clipHi = false;
        foreach (var c in px) {
            if (c.r == 0 || c.g == 0 || c.b == 0) clipSh = true;
            if (c.r == 1 || c.g == 1 || c.b == 1) clipHi = true;
            int iR = Mathf.Clamp((int)(c.r * bins), 0, bins - 1);
            int iG = Mathf.Clamp((int)(c.g * bins), 0, bins - 1);
            int iB = Mathf.Clamp((int)(c.b * bins), 0, bins - 1);
            hR[iR]++; hG[iG]++; hB[iB]++;
            float lum = 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
            sumL += lum; sumL2 += lum * lum;
            Color.RGBToHSV(c, out _, out float s, out float v);
            sumS += s; sumV += v;
        }
        float mL = sumL / total;
        contrast = mL > 0 ? Mathf.Sqrt(Mathf.Max(0, sumL2 / total - mL * mL)) : 0;
        avgSat = sumS / total; avgExp = sumV / total;
    }

    float ComputeDeltaE(Texture2D a, Texture2D b) {
        var pa = a.GetPixels(); var pb = b.GetPixels(); int ct = Mathf.Min(pa.Length, pb.Length);
        float sum = 0;
        for (int i = 0; i < ct; i++) sum += DeltaE76(pa[i], pb[i]);
        return sum / ct;
    }

    Texture2D GenerateDiffMap(Texture2D a, Texture2D b, int w, int h) {
        var outT = new Texture2D(w, h);
        for (int y = 0; y < h; y++) {
            for (int x = 0; x < w; x++) {
                float u = x / (float)w, v = y / (float)h;
                Color ca = a.GetPixelBilinear(u, v);
                Color cb = b.GetPixelBilinear(u, v);
                float da = (ca - cb).grayscale;
                outT.SetPixel(x, y, da > 0 ? Color.magenta : Color.cyan);
            }
        }
        outT.Apply(); return outT;
    }

    void DrawHistogramInto(Rect rect, int[] gr, int[] gg, int[] gb, int[] rr, int[] rg, int[] rb) {
        GUI.BeginClip(rect);
        float wSeg = rect.width / bins, subW = wSeg / 3;
        for (int i = 0; i < bins; i++) {
            int[] gH = { gr[i], gg[i], gb[i] };
            int[] rH = { rr[i], rg[i], rb[i] };
            for (int ch = 0; ch < 3; ch++) {
                float hG = gH[ch] * scale, hR = rH[ch] * scale;
                float x0 = i * wSeg + ch * subW;
                Color baseCol = ch == 0 ? Color.red : ch == 1 ? Color.green : Color.blue;
                Color barCol = hG > hR ? Color.red : baseCol;
                EditorGUI.DrawRect(new Rect(x0, rect.height - hG, subW - 1, hG), barCol);
                EditorGUI.DrawRect(new Rect(x0, rect.height - hR, subW - 1, 1), baseCol);
            }
        }
        GUI.EndClip();
    }

    float DeltaE76(Color c1, Color c2) {
        var lab1 = RGBToLab(c1);
        var lab2 = RGBToLab(c2);
        return Mathf.Sqrt(
            (lab1.x - lab2.x) * (lab1.x - lab2.x) +
            (lab1.y - lab2.y) * (lab1.y - lab2.y) +
            (lab1.z - lab2.z) * (lab1.z - lab2.z)
        );
    }

    Vector3 RGBToLab(Color c) {
        float r = PivotRGB(c.r), g = PivotRGB(c.g), b = PivotRGB(c.b);
        float x = r * 0.4124f + g * 0.3576f + b * 0.1805f;
        float y = r * 0.2126f + g * 0.7152f + b * 0.0722f;
        float z = r * 0.0193f + g * 0.1192f + b * 0.9505f;
        x /= 0.95047f; z /= 1.08883f;
        x = PivotXYZ(x); y = PivotXYZ(y); z = PivotXYZ(z);
        return new Vector3(116f * y - 16f, 500f * (x - y), 200f * (y - z));
    }

    float PivotRGB(float n) => n > 0.04045f ? Mathf.Pow((n + 0.055f) / 1.055f, 2.4f) : n / 12.92f;
    float PivotXYZ(float n) => n > 0.008856f ? Mathf.Pow(n, 1f / 3f) : (7.787f * n + 16f / 116f);

    void Export3DLUT(string path, int size) {
        var tex = new Texture3D(size, size, size, TextureFormat.RGBA32, false);
        for (int z = 0; z < size; z++)
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++) {
                    float r = x / (float)(size - 1);
                    float g = y / (float)(size - 1);
                    float b = z / (float)(size - 1);
                    Color c = new Color(r, g, b);
                    tex.SetPixel(x, y, z, c);
                }
        tex.Apply();
        AssetDatabase.CreateAsset(tex, path);
        AssetDatabase.SaveAssets();
    }

    void CreatePreset() {
        var preset = ScriptableObject.CreateInstance<QCPreset>();
        preset.bins = bins;
        preset.scale = scale;
        if (postProcessVolume.profile.TryGet<ColorAdjustments>(out var ca)) {
            preset.exposure = ca.postExposure.value;
            preset.contrast = ca.contrast.value;
            preset.saturation = ca.saturation.value;
        }
        string pth = EditorUtility.SaveFilePanelInProject("Save Preset", "QC Preset", "asset", "");
        if (!string.IsNullOrEmpty(pth)) {
            AssetDatabase.CreateAsset(preset, pth);
            AssetDatabase.SaveAssets();
            presets.Add(preset);
        }
    }

    void ApplyPreset(QCPreset preset) {
        bins = preset.bins;
        scale = preset.scale;
        if (postProcessVolume.profile.TryGet<ColorAdjustments>(out var ca)) {
            ca.postExposure.Override(preset.exposure);
            ca.contrast.Override(preset.contrast);
            ca.saturation.Override(preset.saturation);
        }
    }

    void SaveSnapshot() {
        var snap = new HistogramSnapshot {
            r = (int[])gameR.Clone(),
            g = (int[])gameG.Clone(),
            b = (int[])gameB.Clone(),
            exposure = expGame,
            contrast = contrastGame,
            saturation = satGame,
            time = DateTime.Now
        };
        history.Add(snap);
    }
}
