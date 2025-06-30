using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class Paper : MonoBehaviour
{
    [SerializeField]
    public XRGrabInteractable GrabInteractable;
    [SerializeField]
    public MeshRenderer PaperMeshRenderer;
    [SerializeField]
    public AudioSource NarratorSoundSource;
    [SerializeField]
    public AudioSource HoverAudioSource;
    [SerializeField]
    public AudioSource SelectAudioSource;

    private bool GrabbedBeforeCollision;
    private bool IsGrabbing;
    private Rigidbody RigidBody;
    private Vector3 StartingPosition;
    private Quaternion StartingRotation;

    static Color WhiteColor = new Color(1, 1, 1);
    static Color BlackColor = new Color(0, 0, 0);

    void Start() {
        RigidBody = GetComponent<Rigidbody>();
        StartingPosition = gameObject.transform.position;
        StartingRotation = gameObject.transform.rotation;
        RigidBody.isKinematic = true;
        GrabbedBeforeCollision = false;
        IsGrabbing = false;
    }

    private void OnEnable() {
        GrabInteractable.selectEntered.AddListener(OnSelectEntered);
        GrabInteractable.selectExited.AddListener(OnSelectExited);
        GrabInteractable.hoverEntered.AddListener(OnHoverEntered);
        GrabInteractable.hoverExited.AddListener(OnHoverExited);

        GrabInteractable.selectEntered.AddListener(OnSelectEntered);
        GrabInteractable.hoverEntered.AddListener(OnHoverEntered);
        GrabInteractable.hoverExited.AddListener(OnHoverExited);
    }

    private void OnDisable() {
        GrabInteractable.selectEntered.RemoveListener(OnSelectEntered);
        GrabInteractable.selectExited.RemoveListener(OnSelectExited);
        GrabInteractable.hoverEntered.RemoveListener(OnHoverEntered);
        GrabInteractable.hoverExited.RemoveListener(OnHoverExited);
    }

    private void OnSelectEntered(SelectEnterEventArgs Args) {
        SelectAudioSource.Play();
        if (!Enneagram.Instance.PaperBurningAudioAlreadyPlayed) {
            NarratorSoundSource.Play();
            Enneagram.Instance.PaperBurningAudioAlreadyPlayed = true;
        }
        RigidBody.isKinematic = false;
        GrabbedBeforeCollision = true;
        IsGrabbing = true;
    }
    private void OnSelectExited(SelectExitEventArgs Args) {
        IsGrabbing = false;
        if (GrabbedBeforeCollision) {
            RigidBody.isKinematic = false;
        }
    }
    private void OnHoverEntered(HoverEnterEventArgs Args) {
        HoverAudioSource.Play();
        PaperMeshRenderer.material.SetColor("_RimColor", WhiteColor);
    }
    private void OnHoverExited(HoverExitEventArgs Args) {
        PaperMeshRenderer.material.SetColor("_RimColor", BlackColor);
    }

    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.tag == "Fire") {
            Enneagram.Instance.FirePaperBurnSound.Play();
            Enneagram.Instance.FirePaperBurnParticles.Play();
            PaperMeshRenderer.gameObject.SetActive(false);
        }
    }

    private void OnCollisionEnter(Collision collision) {
        if (GrabbedBeforeCollision && !IsGrabbing) {
            RigidBody.isKinematic = true;
            gameObject.transform.position = StartingPosition;
            gameObject.transform.rotation = StartingRotation;
        }
    }
}
