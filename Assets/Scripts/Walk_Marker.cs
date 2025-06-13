using UnityEngine;

public class Walk_Marker : MonoBehaviour
{
    static private Walk_Marker LastHiddenMarker = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void Hide() {
        gameObject.SetActive(false);
    }

    public void Show() {
        gameObject.SetActive(true);
    }

    public void Clicked()
    {
        if (LastHiddenMarker != null) {
            LastHiddenMarker.Show();
        }

        Enneagram.Instance.XROrigin.transform.position = gameObject.transform.position;
        Enneagram.Instance.XROrigin.transform.rotation = gameObject.transform.rotation;

        LastHiddenMarker = this;
        LastHiddenMarker.Hide();
    }
}
