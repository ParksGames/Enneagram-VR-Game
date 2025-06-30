using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour {
    [SerializeField]
    public Sacred_Type SacredType;
    [SerializeField]
    public bool IsEntrancePortal;
    [SerializeField]
    public Walk_Marker EntrancePortalTeleportMarker;

    [SerializeField]
    public GameObject UIGameObject;
    [SerializeField]
    public GameObject AlreadyEnteredUIGameObject;

    private bool AlreadyEntered = false;

    void Awake() {
        UIGameObject.SetActive(false);
        AlreadyEnteredUIGameObject.SetActive(false);
    }

    public void ShowUI() {
        if (AlreadyEntered) {
            AlreadyEnteredUIGameObject.SetActive(true);
        } else {
            UIGameObject.SetActive(true);
        }
    }

    public void EnterPortal() {
        if (!IsEntrancePortal) {
            Enneagram.Instance.TheaterSpaceTeleportMarker.Clicked();
        } else {
            EntrancePortalTeleportMarker.Clicked();
        }

        UIGameObject.SetActive(false);

        AlreadyEntered = true;

        if (!IsEntrancePortal) {
            Enneagram.Instance.StartVideo(SacredType);
        }
    }

    public void TurnBack() {
        Vector3 Rot = Enneagram.Instance.XROrigin.transform.rotation.eulerAngles;
        Rot.y += 180;
        Enneagram.Instance.XROrigin.transform.rotation = Quaternion.Euler(Rot);

        if (AlreadyEntered) {
            AlreadyEnteredUIGameObject.SetActive(false);
        } else {
            UIGameObject.SetActive(false);
        }
    }
}
