using UnityEngine;

public class DashAction : PlayerAction
{
    [SerializeField] float _dashDuration;
    [SerializeField] float _dashCooldown;
    [Header("Read-Only Params")]
    [SerializeField, ReadOnly] float _dashDurationTimer;
    [SerializeField, ReadOnly] float _dashInactiveTimer;
    [SerializeField, ReadOnly] bool _canDash = true; // Can dash if true, otherwise false
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField, ReadOnly] private float _dashProgress;
    private void Start()
    {
        _dashDurationTimer = _dashDuration; // Reset dash duration timer when not dashing
        _dashInactiveTimer = _dashCooldown;
    }
    public void TryDash()
    {
        if (_canDash)
        {
            _playerLocomotionState.HasDashed = true;
            _canDash = false; // Start dash cooldown
            _playerLocomotionState.Anim.SetTrigger("Dash");
        }
    }
    public void HandleDash()
    {
        // ---- Dash state update ----
        if(!_canDash)
            if (_dashInactiveTimer > 0f)
                _dashInactiveTimer -= Time.deltaTime;
            else
            {
                _dashInactiveTimer = _dashCooldown;
                _canDash = true; // Reset dash cooldown
            }
        if (_playerLocomotionState.HasDashed)
            if (_dashDurationTimer > 0f)
            {
                _dashDurationTimer -= Time.deltaTime;
                if (_dashDurationTimer <= 0f)
                {
                    _dashDurationTimer = _dashDuration; // Start cooldown timer
                    _playerLocomotionState.HasDashed = false; // Reset dash state
                }
                //invert the progress so it goes from 0 to 1
                _dashProgress = 1f - (_dashDurationTimer / _dashDuration);
            }
    }
}
