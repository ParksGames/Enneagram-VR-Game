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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UIGameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowUI() {
        UIGameObject.SetActive(true);
    }

    public void EnterPortal()
    {
        if (XROrigin != null)
        {
            XROrigin.transform.position = Enneagram.Instance.TheaterSpaceTeleportLocation.position;
#if false
            Vector3 Rot = XROrigin.transform.rotation.eulerAngles;
            Rot.y += 180;
            XROrigin.transform.rotation = Quaternion.Euler(Rot);
#endif
            XROrigin.transform.rotation = Enneagram.Instance.TheaterSpaceTeleportLocation.rotation;

            UIGameObject.SetActive(false);

            Enneagram.Instance.StartVideo(SacredType);
        }
    }

    public void TurnBack() {
        if (XROrigin != null) {
            Vector3 Rot = XROrigin.transform.rotation.eulerAngles;
            Rot.y += 180;
            XROrigin.transform.rotation = Quaternion.Euler(Rot);
        }

        UIGameObject.SetActive(false);
    }
}
