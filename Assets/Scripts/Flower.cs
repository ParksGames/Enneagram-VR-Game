using UnityEngine;

using TMPro;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;

public class Flower : MonoBehaviour {
    [SerializeField]
    public TextMeshPro TextMesh;

    [SerializeField]
    public ParticleSystem FlowerParticles;

    [SerializeField]
    public XRGrabInteractable GrabInteractable;

    bool UsingLeftHand;
    private IXRSelectInteractor CurrentInteractor;
    private IXRSelectInteractable CurrentInteractable;
    private XRInteractionManager CurrentInteractionManager;
    private InteractionLayerMask PrevControllerLayerMask;

    private bool Grabbed;
    private string StartString;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() {
        Grabbed = false;
        UsingLeftHand = false;
        StartString = TextMesh.text;
        CurrentInteractor = null;
        CurrentInteractable = null;
        CurrentInteractionManager = null;
        PrevControllerLayerMask = new InteractionLayerMask();
    }

    // Update is called once per frame
    void Update() {
        if (Enneagram.Instance != null) {
            TextMesh.gameObject.transform.LookAt(Enneagram.Instance.XRCamera.transform);
            TextMesh.gameObject.transform.rotation *= Quaternion.Euler(0, 180, 0);
        }

        if (Grabbed) {
            CurrentInteractionManager.SelectEnter(CurrentInteractor, GrabInteractable);
        }
    }

    private void OnEnable() {
        GrabInteractable.selectEntered.AddListener(OnSelectEntered);
    }

    private void OnDisable() {
        GrabInteractable.selectEntered.RemoveListener(OnSelectEntered);
    }

    public IEnumerator InhaleExhale() {
        yield return new WaitForSeconds(3);
        TextMesh.text = "Inhale...";
        yield return new WaitForSeconds(3);
        TextMesh.text = "\nExhale";
        FlowerParticles.Play();
        yield return new WaitForSeconds(3);
        if (UsingLeftHand) {
            Enneagram.Instance.LeftNearFarInteractor.EndManualInteraction();
            Enneagram.Instance.RightNearFarInteractor.interactionLayers = PrevControllerLayerMask;
        } else {
            Enneagram.Instance.LeftNearFarInteractor.interactionLayers = PrevControllerLayerMask;
            Enneagram.Instance.RightNearFarInteractor.EndManualInteraction();
        }
        gameObject.SetActive(false);
        //Enneagram.Instance.LeftNearFarInteractor.allowSelect  = true;
        //Enneagram.Instance.RightNearFarInteractor.interactionLayers = ;
    }

    private void OnSelectEntered(SelectEnterEventArgs Args) {
        if (!Grabbed) {
            Grabbed = true;
            CurrentInteractor = Args.interactorObject;
            CurrentInteractable = Args.interactableObject;
            CurrentInteractionManager = Args.manager;
            if (CurrentInteractor.handedness == InteractorHandedness.Left) {
                UsingLeftHand = true;
                
                Enneagram.Instance.LeftNearFarInteractor.StartManualInteraction(CurrentInteractable);

                PrevControllerLayerMask = Enneagram.Instance.RightNearFarInteractor.interactionLayers;
                Enneagram.Instance.RightNearFarInteractor.interactionLayers = new InteractionLayerMask();
            } else {
                UsingLeftHand = false;

                PrevControllerLayerMask = Enneagram.Instance.LeftNearFarInteractor.interactionLayers;
                Enneagram.Instance.LeftNearFarInteractor.interactionLayers = new InteractionLayerMask();

                Enneagram.Instance.RightNearFarInteractor.StartManualInteraction(CurrentInteractable);
            }

            StartCoroutine(InhaleExhale());
        }
    }
}
