using Unity.VRTemplate;
using UnityEngine;

public class Tutorial : MonoBehaviour {
    [SerializeField]
    public Walk_Marker TeleportMarkerToActivateWhenFinished;

    [SerializeField]
    public Callout LeftControllerSelectCallout;
    [SerializeField]
    public Callout RightControllerSelectCallout;

    [SerializeField]
    public GameObject UIGameObject;

    private bool TutorialActive;
    void Awake() {
        TutorialActive = false;
        UIGameObject.SetActive(false);
    }

    void Update() {
        if (TutorialActive) {
            LeftControllerSelectCallout.SetAsPermanentlyActive();
            RightControllerSelectCallout.SetAsPermanentlyActive();
        }
    }

    public void ShowUI() {
        UIGameObject.SetActive(true);
        LeftControllerSelectCallout.SetAsPermanentlyActive();
        RightControllerSelectCallout.SetAsPermanentlyActive();
        TutorialActive = true;
    }

    public void FinishTutorial() {
        UIGameObject.SetActive(false);
        LeftControllerSelectCallout.UnsetAsPermanentlyActive();
        RightControllerSelectCallout.UnsetAsPermanentlyActive();
        TeleportMarkerToActivateWhenFinished.ActivateMarkerFromTutorial();
        TutorialActive = false;
    }
}
