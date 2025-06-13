using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    [SerializeField]
    public Transform TeleportLocation;

    [SerializeField]
    public GameObject XROrigin;

    [SerializeField]
    public Sacred_Type SacredType;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void EnterPortal()
    {
        if (XROrigin != null)
        {
            XROrigin.transform.position = TeleportLocation.position;
#if false
            Vector3 Rot = XROrigin.transform.rotation.eulerAngles;
            Rot.y += 180;
            XROrigin.transform.rotation = Quaternion.Euler(Rot);
#endif
            XROrigin.transform.rotation = TeleportLocation.rotation;

            Enneagram.Instance.StartVideo(SacredType);
        }
    }
}
