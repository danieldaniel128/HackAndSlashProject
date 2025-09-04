using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RunnerBootstrap : MonoBehaviour
{
    public static NetworkRunner Runner; // single shared runner
    public static NetworkSceneManagerDefault SceneManager;
    [SerializeField] NetworkRunner _networkRunner;
    void Awake()
    {
        // If a runner already exists, reuse it and destroy this duplicate GO
        if (Runner != null && Runner != GetComponent<NetworkRunner>())
        {
            Destroy(gameObject);
            return;
        }

        // Create the runner once and keep it alive across scenes
        Runner = _networkRunner;
        if (Runner == null) Runner = gameObject.AddComponent<NetworkRunner>();

        Runner.ProvideInput = true;
        if (GetComponent<NetworkSceneManagerDefault>() == null)
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();

        DontDestroyOnLoad(gameObject); // critical
    }
}
