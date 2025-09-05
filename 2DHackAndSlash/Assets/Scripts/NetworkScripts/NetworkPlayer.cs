using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class NetworkPlayer : NetworkBehaviour
{
    [SerializeField] private PlayerController _controller;  // your existing script
    [SerializeField] private Camera _camera;


    public override void FixedUpdateNetwork()
    {
        if (Object.HasInputAuthority)
        {
            if (GetInput<PlayerInputData>(out var input))
                // This is like Unity Update but per Fusion render frame
                _controller.ApplyNetworkInput(input);
        _controller.ApplyPhysicsActions(Runner.DeltaTime);
        }
    }
   
    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            // This is *my* local player
            _camera.gameObject.SetActive(true); // enable local camera
            _controller.enabled = true; // let input drive it
        }
        else
        {
            Runner.SetIsSimulated(Object, false); // disable client sim
            _camera.gameObject.SetActive(false);
            _controller.enabled = false; // remote players simulated by state
        }

    }

}
