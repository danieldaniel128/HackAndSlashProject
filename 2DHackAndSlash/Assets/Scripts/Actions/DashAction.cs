using UnityEngine;

public class DashAction : PlayerAction
{
    [Header("Dash Settings")]
    [SerializeField] float _dashPower = 10f;
    [SerializeField] float _dashDuration;
    [SerializeField] float _dashCooldown;
    [Header("Runtime")]
    [SerializeField, ReadOnly] int _dashDir;                // direction chosen for dash
    [SerializeField, ReadOnly] float _dashDurationTimer;
    [SerializeField, ReadOnly] float _dashInactiveTimer;
    [SerializeField, ReadOnly] bool _hasDashed = false;
    [SerializeField, ReadOnly] bool _canDash = true;        // Can dash if true, otherwise false
    [SerializeField, ReadOnly] private float _dashProgress; //for ui

    private void Start()
    {
        _dashDurationTimer = _dashDuration; // Reset dash duration timer when not dashing
        _dashInactiveTimer = _dashCooldown;
    }
    public void TryDash()
    {
        if (_canDash)
        {
            _canDash = false; // Start dash cooldown
            //Debug.Log("<color=white>dashed</color>");
            _hasDashed = true;
            _playerLocomotionState.Anim.SetTrigger("Dash");
            Dash();
        }
    }
    public void HandleDash()
    {
        PlayerLocomotion pL = _playerLocomotionState;
        // ---- Dash state update ----
        if (!_canDash)
            if (_dashInactiveTimer > 0f)
                _dashInactiveTimer -= Time.deltaTime;
            else
            {
                _dashInactiveTimer = _dashCooldown;
                _canDash = true; // Reset dash cooldown
            }
        if (_hasDashed)
            if (_dashDurationTimer > 0f)
            {
                _dashDurationTimer -= Time.deltaTime;
                if (_dashDurationTimer <= 0f)
                {
                    _dashDurationTimer = _dashDuration; // Start cooldown timer
                    _hasDashed = false; // Reset dash state
                    pL.InputLocked = false; // Unlock input when dash ends
                    //Debug.Log("<color=white>ended dash</color>");
                }
                //invert the progress so it goes from 0 to 1
                _dashProgress = 1f - (_dashDurationTimer / _dashDuration);
            }
    }
    public void Dash()
    {
        if (!_hasDashed)
            return;
        PlayerLocomotion pL = _playerLocomotionState;
        Rigidbody2D rb = pL.Rb;
        float moveInput = pL.MoveInput;
        // --- DASH DIRECTION ---
        // Decide which direction to dash:
        // 1. If there is live input, use that
        // 2. Else, use the last non-zero input
        // 4. Else, default to right (1)
        float dashDir =
            (moveInput != 0 ? moveInput :
            (pL.LastMoveInputNot0 != 0 ? pL.LastMoveInputNot0 : 1));


        // Store the dash direction as an integer (-1 or +1)
        _dashDir = (int)dashDir;

        // Pre-calculate dash velocity (impulse)
        float _dashVelocity = _dashDir * _dashPower;

        float vx = dashDir * _dashPower; //dash velocity vector

        rb.linearVelocity = new Vector2(vx, 0);
        Physics2D.gravity = new Vector2(Physics2D.gravity.x, 0);

        pL.InputLocked = true;  // set when dash starts, unset when dash ends
    }
}
