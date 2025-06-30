using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Walk_Marker : MonoBehaviour
{
    [SerializeField]
    public Portal AttachedPortal;
    [SerializeField]
    public Tutorial AttachedTutorial;

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
    private bool HideMarkersUntilNarratorFinished = false;
    private bool NarratorAudioAlreadyPlayed = false;
    private bool SecondaryAudioAlreadyPlayed = false;

    static private List<Walk_Marker> ActiveMarkers = new List<Walk_Marker>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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
                
                if (AttachedPortal != null) {
                    AttachedPortal.ShowUI();
                }

                if (AttachedTutorial != null) {
                    AttachedTutorial.ShowUI();
                }

                HideMarkersUntilNarratorFinished = false;
                NarratorAudioAlreadyPlayed = true;
                if (!Enneagram.Instance.IsPlayingVideo) {
                    Enneagram.Instance.InTransition = false;
                }
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

    public bool HasPrimaryAudio() {
        return (PlaysNarratorAudio && AttachedNarratorAudio != null && !Enneagram.Instance.DEBUG_DisableNarrator);
    }
    public bool WillPlayPrimaryAudio() {
        return (HasPrimaryAudio() && !NarratorAudioAlreadyPlayed);
    }
    public bool HasSecondaryAudio() {
        return (HasPrimaryAudio() && SecondaryAudioClipAfterEngagementSpaceIsActivated != null);
    }
    public bool WillPlaySecondaryAudio() {
        return (HasSecondaryAudio() && Enneagram.Instance.EngagementSpaceActivated && !SecondaryAudioAlreadyPlayed);
    }
    public bool WillPlayTransitionOnNextClick() {
        return (WillPlayPrimaryAudio() || WillPlaySecondaryAudio());
    }

    public void ActivateMarkerFromTutorial() {
        foreach (Walk_Marker Marker in ActiveMarkers) {
            Marker.Hide();
        }

        ActiveMarkers.Clear();
        Show();
        ActiveMarkers.Add(this);
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

        if (WillPlaySecondaryAudio()) {
            AttachedNarratorAudio.clip = SecondaryAudioClipAfterEngagementSpaceIsActivated;
            NarratorAudioAlreadyPlayed = false;
            SecondaryAudioAlreadyPlayed = true;
        }

        Enneagram.Instance.XROrigin.transform.position = gameObject.transform.position;
        Enneagram.Instance.XROrigin.transform.rotation = gameObject.transform.rotation;

        Hide();

        if (!WillPlayPrimaryAudio() && !WillPlaySecondaryAudio()) {
            if (AttachedPortal != null) {
                AttachedPortal.ShowUI();
            }
            if (AttachedTutorial != null) {
                AttachedTutorial.ShowUI();
            }
        }

        foreach (Walk_Marker Marker in ActiveMarkers) {
            Marker.Hide();
        }

        ActiveMarkers.Clear();
        foreach (Walk_Marker Marker in ConnectedMarkers) {
            if (WillPlayPrimaryAudio() || WillPlaySecondaryAudio()) {
                Marker.Hide();
            } else {
                Marker.Show();
            }
            
            ActiveMarkers.Add(Marker);
        }

        if (Enneagram.Instance.EngagementSpaceActivated) {
            foreach (Walk_Marker Marker in ExtraConnectedMarkersWhenEngagementSpaceIsActivated) {
                if (WillPlayPrimaryAudio() || WillPlaySecondaryAudio()) {
                    Marker.Hide();
                } else {
                    Marker.Show();
                }

                ActiveMarkers.Add(Marker);
            }
        }

        if (WillPlayPrimaryAudio() || WillPlaySecondaryAudio()) {
            AttachedNarratorAudio.Play();
            Enneagram.Instance.InTransition = true;
            HideMarkersUntilNarratorFinished = true;
        }
    }
}
