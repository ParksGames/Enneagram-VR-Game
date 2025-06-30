using UnityEngine;
using UnityEngine.Video;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Threading;
using NUnit.Framework;
using System.Collections.Generic;

/* TODO LIST:
 * 
 * -Tutorial
 * -Implement view rotation
 *   -Controller based view rotation
 *   -World space arrows for view rotation
 * 
 * 
 */

public enum Sacred_Type
{
    CONNECTION,
    HOLDING,
    TRUST,
};

public class Enneagram : MonoBehaviour {
    [SerializeField]
    public VideoPlayer VideoPlayer;

    [SerializeField]
    public VideoClip SacredConnectionVideoClip;
    [SerializeField]
    public VideoClip SacredHoldingVideoClip;
    [SerializeField]
    public VideoClip SacredTrustVideoClip;

    [SerializeField]
    public Walk_Marker TheaterSpaceTeleportMarker;
    [SerializeField]
    public Walk_Marker InteractionSpaceTeleportMarker;
    [SerializeField]
    public GameObject XROrigin;
    [SerializeField]
    public Camera XRCamera;

    [SerializeField]
    public NearFarInteractor LeftNearFarInteractor;
    [SerializeField]
    public NearFarInteractor RightNearFarInteractor;

    [SerializeField]
    public MeshRenderer VideoMeshRenderer;

    [SerializeField]
    public ParticleSystem FirePaperBurnParticles;
    [SerializeField]
    public AudioSource FirePaperBurnSound;

    [SerializeField]
    public Walk_Marker StartWalkMarker;

    [SerializeField]
    public ScreenFader ScreenFader;

    [SerializeField]
    public List<GameObject> InteractionSpaceObjects;

    public static Enneagram Instance = null;

    [SerializeField]
    public float FullTimeLimit;

    [SerializeField]
    public bool DEBUG_DisableNarrator;
    [SerializeField]
    public bool DEBUG_EnableEngagementSpaceBeforeVideo;

    private float TimeElapsed;
    private float TimeElapsedSinceLastTransition;
    private bool InteractionDisabled;
    public bool InTransition;
    public bool IsPlayingVideo;
    public bool EngagementSpaceActivated;
    public bool PaperBurningAudioAlreadyPlayed;

    public InteractionLayerMask EmptyLayerMask;
    public InteractionLayerMask DefaultLayerMask;
    public InteractionLayerMask PaperBurningMask;
    public InteractionLayerMask FlowerMask;

    public void SetInteractionsDisabled() {
        LeftNearFarInteractor.interactionLayers = EmptyLayerMask;
        RightNearFarInteractor.interactionLayers = EmptyLayerMask;
        InteractionDisabled = true;
    }

    public void SetDefaultInteractionMask() {
        if (!InteractionDisabled) {
            LeftNearFarInteractor.interactionLayers = DefaultLayerMask;
            RightNearFarInteractor.interactionLayers = DefaultLayerMask;
        }
    }

    public void SetPaperBurningInteractionMask() {
        if (!InteractionDisabled) {
            LeftNearFarInteractor.interactionLayers = PaperBurningMask;
            RightNearFarInteractor.interactionLayers = PaperBurningMask;
        }
    }

    public void SetFlowerInteractionMask() {
        if (!InteractionDisabled) {
            LeftNearFarInteractor.interactionLayers = FlowerMask;
            RightNearFarInteractor.interactionLayers = FlowerMask;
        }
    }

    void Awake() {
        EmptyLayerMask = new InteractionLayerMask();
        DefaultLayerMask = InteractionLayerMask.GetMask(new[] { "Default" });
        PaperBurningMask = InteractionLayerMask.GetMask(new[] { "Default", "Paper" });
        FlowerMask = InteractionLayerMask.GetMask(new[] { "Default", "Flower" });

        IsPlayingVideo = false;
        InTransition = false;
        Instance = this;
        EngagementSpaceActivated = false;

        VideoMeshRenderer.enabled = false;

        PaperBurningAudioAlreadyPlayed = false;
        InteractionDisabled = false;
        TimeElapsed = 0;
        TimeElapsedSinceLastTransition = 0;

        VideoPlayer.loopPointReached += OnVideoFinished;
    }

    void Start() {
        if (DEBUG_EnableEngagementSpaceBeforeVideo) {
            EngagementSpaceActivated = true;
        } else {
            foreach (GameObject Obj in InteractionSpaceObjects) {
                Obj.SetActive(false);
            }
            EngagementSpaceActivated = false;
        }

        if (StartWalkMarker != null) {
            StartWalkMarker.Clicked();
        }
    }

    void Update() {
        TimeElapsed += Time.deltaTime;
        if (InTransition) {
            TimeElapsedSinceLastTransition = 0;
        } else {
            TimeElapsedSinceLastTransition += Time.deltaTime;

            if (TimeElapsed >= FullTimeLimit) {
                if (!InteractionDisabled) {
                    SetInteractionsDisabled();
                }
                if (TimeElapsedSinceLastTransition >= 0.5) {
                    TimeElapsed = float.NegativeInfinity;
                    StartCoroutine(GoToEndSceneAsyncRoutine());
                }
            }
        }
    }

    public IEnumerator GoToEndSceneAsyncRoutine() {
        // Activate fade to white:
        ScreenFader.FadeColor = new Color(1, 1, 1, 0);
        ScreenFader.ActivateFadeIn();

        // Load new scene:
        AsyncOperation Operation = SceneManager.LoadSceneAsync("End");
        Operation.allowSceneActivation = false;

        while (ScreenFader.IsActive && !Operation.isDone) {
            yield return null;
        }

        Operation.allowSceneActivation = true;
    }

    public void StartVideo(Sacred_Type SacredType)
    {
        if (VideoPlayer != null)
        {
            switch (SacredType)
            {
                case Sacred_Type.CONNECTION:
                    VideoPlayer.clip = SacredConnectionVideoClip;
                    break;
                case Sacred_Type.HOLDING:
                    VideoPlayer.clip = SacredHoldingVideoClip;
                    break;
                case Sacred_Type.TRUST:
                    VideoPlayer.clip = SacredTrustVideoClip;
                    break;
            }

            InTransition = true;
            IsPlayingVideo = true;
            VideoMeshRenderer.enabled = true;
            VideoPlayer.Play();
        }
    }

    public void OnVideoFinished(VideoPlayer VideoPlayer)
    {
        VideoMeshRenderer.enabled = false;
        PaperBurningAudioAlreadyPlayed = false;

        IsPlayingVideo = false;
        bool DoesInteractionSpaceTeleportMarkerHaveTransition = false;
        if (InteractionSpaceTeleportMarker != null) {
            DoesInteractionSpaceTeleportMarkerHaveTransition = InteractionSpaceTeleportMarker.WillPlayTransitionOnNextClick();
        }
        if (!DoesInteractionSpaceTeleportMarkerHaveTransition) {
            InTransition = false;
        }

        EngagementSpaceActivated = true;
        foreach (GameObject Obj in InteractionSpaceObjects) {
            Obj.SetActive(true);
        }

        if (InteractionSpaceTeleportMarker != null) {
            InteractionSpaceTeleportMarker.Clicked();
        }
    }
}
