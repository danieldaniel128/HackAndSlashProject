using UnityEngine;

public class PlayerAction : MonoBehaviour
{
    [SerializeField, ReadOnly] protected PlayerLocomotion _playerLocomotionState;
    public void InitAction(PlayerLocomotion playerLocomotionState)
    {
        _playerLocomotionState = playerLocomotionState;
    }
}
