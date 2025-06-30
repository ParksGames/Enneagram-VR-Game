using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    [SerializeField]
    public GameObject XROrigin;

    [SerializeField]
    public Sacred_Type SacredType;

    [SerializeField]
    public GameObject UIGameObject;

    [SerializeField]
    public GameObject AlreadyEnteredUIGameObject;

    private bool AlreadyEntered;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        AlreadyEntered = false;
        UIGameObject.SetActive(false);
        AlreadyEnteredUIGameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowUI() {
        if (AlreadyEntered) {
            AlreadyEnteredUIGameObject.SetActive(true);
        } else {
            UIGameObject.SetActive(true);
        }
    }

    public void EnterPortal()
    {
        if (XROrigin != null)
        {
            Enneagram.Instance.TheaterSpaceTeleportMarker.Clicked();

            UIGameObject.SetActive(false);

            AlreadyEntered = true;

            Enneagram.Instance.StartVideo(SacredType);
        }
    }

    public void TurnBack() {
        if (XROrigin != null) {
            Vector3 Rot = XROrigin.transform.rotation.eulerAngles;
            Rot.y += 180;
            XROrigin.transform.rotation = Quaternion.Euler(Rot);
        }

        if (AlreadyEntered) {
            AlreadyEnteredUIGameObject.SetActive(false);
        } else {
            UIGameObject.SetActive(false);
        }
    }
}
