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
    public AudioSource FootstepSound;

    private XRSimpleInteractable SimpleInteractable;
    private Collider Collider;

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
            Marker.Show();
            ActiveMarkers.Add(Marker);
        }
    }
}
