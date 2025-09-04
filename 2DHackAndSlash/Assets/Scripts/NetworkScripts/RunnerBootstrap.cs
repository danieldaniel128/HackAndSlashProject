using Fusion;
using UnityEngine;

public class RunnerBootstrap : MonoBehaviour
{
    public static NetworkRunner Runner; // single shared runner
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
            gameObject.AddComponent<NetworkSceneManagerDefault>();

        DontDestroyOnLoad(gameObject); // critical
    }
}
