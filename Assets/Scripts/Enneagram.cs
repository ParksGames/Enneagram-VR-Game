using UnityEngine;
using UnityEngine.Video;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Threading;

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
    public Transform InteractionSpaceTeleportLocation;
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

    public static Enneagram Instance = null;

    [SerializeField]
    public float FullTimeLimit;

    private float TimeElapsed;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Instance = this;

        VideoMeshRenderer.enabled = false;

        TimeElapsed = 0;

        if (StartWalkMarker != null) {
            StartWalkMarker.Clicked();
        }
    }

    // Update is called once per frame
    void Update()
    {
        TimeElapsed += Time.deltaTime;
        if (TimeElapsed >= FullTimeLimit) {
            TimeElapsed = float.NegativeInfinity;
            StartCoroutine(GoToEndSceneAsyncRoutine());
        }
    }

    public IEnumerator GoToEndSceneAsyncRoutine() {
        // Disable interactions:
        LeftNearFarInteractor.interactionLayers = new InteractionLayerMask();
        RightNearFarInteractor.interactionLayers = new InteractionLayerMask();

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
            VideoPlayer.loopPointReached += OnVideoFinished;
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

            VideoMeshRenderer.enabled = true;
            VideoPlayer.Play();
        }
    }

    public void OnVideoFinished(VideoPlayer VideoPlayer)
    {
        VideoMeshRenderer.enabled = false;
        XROrigin.transform.position = InteractionSpaceTeleportLocation.position;
        //Vector3 Rot = XROrigin.transform.rotation.eulerAngles;
        //Rot.y += 180;
        XROrigin.transform.rotation = InteractionSpaceTeleportLocation.rotation;
    }
}
