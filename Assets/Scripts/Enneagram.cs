using UnityEngine;
using UnityEngine.Video;

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
    public MeshRenderer VideoMeshRenderer;
    
    public static Enneagram Instance = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Instance = this;

        VideoMeshRenderer.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        
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
