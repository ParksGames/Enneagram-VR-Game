using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Walk_Marker : MonoBehaviour
{
    [SerializeField]
    public Portal AttachedPortal;

    [SerializeField]
    public List<GameObject> HideObjects;

    [SerializeField]
    public List<Walk_Marker> ConnectedMarkers;

    [SerializeField]
    public List<Walk_Marker> ExtraConnectedMarkersWhenEngagementSpaceIsActivated;

    [SerializeField]
    public AudioSource FootstepSound;

    [SerializeField]
    public bool PlaysNarratorAudio;
    [SerializeField]
    public AudioSource AttachedNarratorAudio;
    [SerializeField]
    public AudioClip SecondaryAudioClipAfterEngagementSpaceIsActivated;

    [SerializeField]
    public bool IsFlowersWalkMarker;
    [SerializeField]
    public bool IsPaperBurningWalkMarker;

    private XRSimpleInteractable SimpleInteractable;
    private Collider Collider;
    private bool HideMarkersUntilNarratorFinished;
    private bool NarratorAudioAlreadyPlayed;
    private bool SecondaryAudioAlreadyPlayed;

    static private List<Walk_Marker> ActiveMarkers = new List<Walk_Marker>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        NarratorAudioAlreadyPlayed = false;
        SecondaryAudioAlreadyPlayed = false;
        HideMarkersUntilNarratorFinished = false;
        Collider = GetComponent<Collider>();
        SimpleInteractable = GetComponent<XRSimpleInteractable>();
        Hide();
    }

    // Update is called once per frame
    void Update()
    {
        if (HideMarkersUntilNarratorFinished) {
            if (AttachedNarratorAudio == null || !AttachedNarratorAudio.isPlaying) {
                foreach (Walk_Marker Marker in ConnectedMarkers) {
                    Marker.Show();
                }
                if (Enneagram.Instance.EngagementSpaceActivated) {
                    foreach (Walk_Marker Marker in ExtraConnectedMarkersWhenEngagementSpaceIsActivated) {
                        Marker.Show();
                    }
                }
                HideMarkersUntilNarratorFinished = false;
                NarratorAudioAlreadyPlayed = true;
            }
        }
    }

    public void Hide() {
        foreach (GameObject Obj in HideObjects) {
            Obj.SetActive(false);
        }
        if (SimpleInteractable != null) {
            SimpleInteractable.enabled = false;
        }
        if (Collider != null) {
            Collider.enabled = false;
        }
    }

    public void Show() {
        foreach (GameObject Obj in HideObjects) {
            Obj.SetActive(true);
        }
        if (SimpleInteractable != null) {
            SimpleInteractable.enabled = true;
        }
        if (Collider != null) {
            Collider.enabled = true;
        }
    }

    public void ClickedWithSound() {
        FootstepSound.Play();
        Clicked();
    }

    public void Clicked()
    {
        if (Enneagram.Instance.DEBUG_DisableNarrator) {
            NarratorAudioAlreadyPlayed = true;
            SecondaryAudioAlreadyPlayed = true;
        }

        if (IsFlowersWalkMarker) {
            Enneagram.Instance.SetFlowerInteractionMask();
        } else if (IsPaperBurningWalkMarker) {
            Enneagram.Instance.SetPaperBurningInteractionMask();
        } else {
            Enneagram.Instance.SetDefaultInteractionMask();
        }

        if (PlaysNarratorAudio && AttachedNarratorAudio != null && SecondaryAudioClipAfterEngagementSpaceIsActivated != null) {
            if (Enneagram.Instance.EngagementSpaceActivated && !SecondaryAudioAlreadyPlayed) {
                AttachedNarratorAudio.clip = SecondaryAudioClipAfterEngagementSpaceIsActivated;
                NarratorAudioAlreadyPlayed = false;
                SecondaryAudioAlreadyPlayed = true;
            }
        }

        Enneagram.Instance.XROrigin.transform.position = gameObject.transform.position;
        Enneagram.Instance.XROrigin.transform.rotation = gameObject.transform.rotation;

        Hide();

        if (AttachedPortal != null) {
            AttachedPortal.ShowUI();
        }

        foreach (Walk_Marker Marker in ActiveMarkers) {
            Marker.Hide();
        }

        ActiveMarkers.Clear();
        foreach (Walk_Marker Marker in ConnectedMarkers) {
            if (PlaysNarratorAudio && AttachedNarratorAudio != null && !NarratorAudioAlreadyPlayed) {
                Marker.Hide();
            } else {
                Marker.Show();
            }
            
            ActiveMarkers.Add(Marker);
        }

        if (Enneagram.Instance.EngagementSpaceActivated) {
            foreach (Walk_Marker Marker in ExtraConnectedMarkersWhenEngagementSpaceIsActivated) {
                if (PlaysNarratorAudio && AttachedNarratorAudio != null && !NarratorAudioAlreadyPlayed) {
                    Marker.Hide();
                } else {
                    Marker.Show();
                }

                ActiveMarkers.Add(Marker);
            }
        }

        if (PlaysNarratorAudio && AttachedNarratorAudio != null && !NarratorAudioAlreadyPlayed) {
            AttachedNarratorAudio.Play();
            Enneagram.Instance.InTransition = true;
            HideMarkersUntilNarratorFinished = true;
        }
    }
}
