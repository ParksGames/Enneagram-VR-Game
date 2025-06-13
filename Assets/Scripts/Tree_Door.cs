using UnityEngine;

public class Tree_Door : MonoBehaviour
{
    [SerializeField]
    public float OpenDoorSpeed;

    private float OpenedAmount;
    private bool OpeningDoor;
    private bool DoorOpened;
    private Quaternion StartRotation;
    private Quaternion DesiredRotation;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartRotation = gameObject.transform.rotation;
        Vector3 Angles = StartRotation.eulerAngles;
        Angles.z -= 90;
        DesiredRotation = Quaternion.Euler(Angles);

        DoorOpened = false;
        OpeningDoor = false;
        OpenedAmount = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (OpeningDoor) {
            OpenedAmount += Time.deltaTime * OpenDoorSpeed;
            if (OpenedAmount >= 1) {
                OpenedAmount = 1;
                OpeningDoor = false;
                DoorOpened = true;
            }


            gameObject.transform.rotation = Quaternion.Lerp(StartRotation, DesiredRotation, OpenedAmount);
        }
    }

    public void OpenDoor()
    {
        if (!DoorOpened) {
            OpeningDoor = true;
        }
    }
}
