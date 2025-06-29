using UnityEngine;
using UnityEngine.Video;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Threading;
using NUnit.Framework;

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

public class Enneagram : MonoBehaviour
{
    [SerializeField]
    public VideoPlayer VideoPlayer;

    [SerializeField]
    public VideoClip SacredConnectionVideoClip;
    [SerializeField]
    public VideoClip SacredHoldingVideoClip;
    [SerializeField]
    public VideoClip SacredTrustVideoClip;

    [SerializeField]
    public Transform TheaterSpaceTeleportLocation;
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
    public Walk_Marker StartWalkMarker;

    [SerializeField]
    public ScreenFader ScreenFader;

    [SerializeField]
    public GameObject InteractionSpace;

    public static Enneagram Instance = null;

    [SerializeField]
    public float FullTimeLimit;

    private float TimeElapsed;
    private float TimeElapsedSinceLastTransition;
    private bool InteractionDisabled;
    public bool InTransition;
    public bool EngagementSpaceActivated;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InTransition = false;
        Instance = this;

        if (InteractionSpace != null) {
            InteractionSpace.SetActive(false);
        }

        VideoMeshRenderer.enabled = false;

        EngagementSpaceActivated = false;
        InteractionDisabled = false;
        TimeElapsed = 0;
        TimeElapsedSinceLastTransition = 0;

        VideoPlayer.loopPointReached += OnVideoFinished;

        if (StartWalkMarker != null) {
            StartWalkMarker.Clicked();
        }
    }

    // Update is called once per frame
    void Update()
    {
        TimeElapsed += Time.deltaTime;
        if (InTransition) {
            TimeElapsedSinceLastTransition = 0;
        } else {
            TimeElapsedSinceLastTransition += Time.deltaTime;

            if (TimeElapsed >= FullTimeLimit) {
                if (!InteractionDisabled) {
                    // Disable interactions:
                    LeftNearFarInteractor.interactionLayers = new InteractionLayerMask();
                    RightNearFarInteractor.interactionLayers = new InteractionLayerMask();
                    InteractionDisabled = true;
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
            VideoMeshRenderer.enabled = true;
            VideoPlayer.Play();
        }
    }

    public void OnVideoFinished(VideoPlayer VideoPlayer)
    {
        VideoMeshRenderer.enabled = false;

        InTransition = false;

        EngagementSpaceActivated = true;
        if (InteractionSpace != null) {
            InteractionSpace.SetActive(true);
        }

        if (InteractionSpaceTeleportMarker != null) {
            InteractionSpaceTeleportMarker.Clicked();
        }
    }
}
