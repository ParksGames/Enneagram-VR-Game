using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.Rendering.HDROutputUtils;

public class Enneagram_End : MonoBehaviour
{
    [SerializeField]
    public float FullTimeLimit;
    [SerializeField]
    public float TimeToStartLoadScene;

    private bool LoadStarted;
    private float TimeElapsed;
    private AsyncOperation StartSceneLoadAsyncOperation;

    void Start() {
        LoadStarted = false;
        TimeElapsed = 0;
    }

    void Update() {
        TimeElapsed += Time.deltaTime;
        if (!LoadStarted && TimeElapsed >= TimeToStartLoadScene) {
            LoadStarted = true;
            StartSceneLoadAsyncOperation = SceneManager.LoadSceneAsync("ArtScene");
            StartSceneLoadAsyncOperation.allowSceneActivation = false;
        }
        if (TimeElapsed >= FullTimeLimit) {
            if (LoadStarted) {
                StartSceneLoadAsyncOperation.allowSceneActivation = true;
            }
        }
    }
}
