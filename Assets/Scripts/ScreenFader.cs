using System;
using System.Collections;
using UnityEngine;

public class ScreenFader : MonoBehaviour {
    [SerializeField]
    public float DefaultFadeTime;
    [SerializeField]
    public Color DefaultFadeColor;
    [SerializeField]
    public bool ActivateFadeInOnStart;
    [SerializeField]
    public bool ActivateFadeOutOnStart;

    public Color FadeColor;

    private Renderer Renderer;

    public bool IsActive;
    private float CurrentTimer;
    private float CurrentFadeTime;
    private float CurrentAlphaStart;
    private float CurrentAlphaEnd;

    void Awake() {
        Renderer = GetComponent<Renderer>();

        IsActive = false;
        CurrentTimer = 0;
        CurrentFadeTime = DefaultFadeTime;
        FadeColor = DefaultFadeColor;
        CurrentAlphaStart = 0;
        CurrentAlphaEnd = 1;
    }

    void Start() {
        if (ActivateFadeInOnStart) {
            ActivateFadeIn();
        }
        if (ActivateFadeOutOnStart) {
            ActivateFadeOut();
        }
    }

    public void ActivateFade(float AlphaStart, float AlphaEnd, float FadeTime) {
        IsActive = true;
        CurrentTimer = 0;
        CurrentFadeTime = FadeTime;
        CurrentAlphaStart = AlphaStart;
        CurrentAlphaEnd = AlphaEnd;
        StartCoroutine(FadeRoutine());
    }

    public void ActivateFadeIn() {
        ActivateFade(0, 1, DefaultFadeTime);
    }

    public void ActivateFadeOut() {
        ActivateFade(1, 0, DefaultFadeTime);
    }

    public IEnumerator FadeRoutine() {
        while (IsActive) {
            CurrentTimer += Time.deltaTime;

            FadeColor.a = Mathf.Lerp(CurrentAlphaStart, CurrentAlphaEnd, CurrentTimer / CurrentFadeTime);

            if (CurrentTimer >= CurrentFadeTime) {
                IsActive = false;
                CurrentTimer = CurrentFadeTime;
            }

            Renderer.material.SetColor("_BaseColor", FadeColor);

            if (IsActive) {
                yield return null;
            }
        }
    }

    void Update() {

    }
}
