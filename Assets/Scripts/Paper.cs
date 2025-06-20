using Unity.XR.CoreUtils;
using UnityEngine;

public class Paper : MonoBehaviour
{
    [SerializeField] 
    public ParticleSystem BurnParticles;
    [SerializeField]
    public GameObject PaperMeshObject;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Fire")
        {
            Vector3 Rot = gameObject.transform.rotation.eulerAngles;
            Rot = -Rot;
            Rot.x -= 90;
            BurnParticles.transform.rotation = Quaternion.Euler(Rot);
            BurnParticles.Play();
            PaperMeshObject.SetActive(false);
        }
    }
}
