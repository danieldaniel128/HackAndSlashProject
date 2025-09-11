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
            _canDash = false; // Start dash cooldown
            Debug.Log("<color=white>dashed</color>");
            _playerLocomotionState.HasDashed = true;
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
        if (pL.HasDashed)
            if (_dashDurationTimer > 0f)
            {
                _dashDurationTimer -= Time.deltaTime;
                if (_dashDurationTimer <= 0f)
                {
                    _dashDurationTimer = _dashDuration; // Start cooldown timer
                    pL.HasDashed = false; // Reset dash state
                    pL.InputLocked = false; // Unlock input when dash ends
                    pL.CurrentVelocityX = 0;
                    Debug.Log("<color=white>ended dash</color>");
                }
                //invert the progress so it goes from 0 to 1
                _dashProgress = 1f - (_dashDurationTimer / _dashDuration);
            }
    }
    public void Dash()
    {
        if (!_playerLocomotionState.HasDashed)
            return;
        PlayerLocomotion pL = _playerLocomotionState;
        Rigidbody2D rb = pL.Rb;
        float moveInput = pL.MoveInput;
        // --- DASH DIRECTION ---
        // Decide which direction to dash:
        // 1. If there is live input, use that
        // 2. Else, use the last non-zero input
        // 3. Else, if still moving, use current velocity sign
        // 4. Else, default to right (1)
        float dashDir =
            (moveInput != 0 ? moveInput :
            (pL.LastMoveInputNot0 != 0 ? pL.LastMoveInputNot0 :
            (pL.CurrentVelocityX > 0.0001f ? pL.CurrentVelocityX : 1)));

        // Store the dash direction as an integer (-1 or +1)
        pL.DashDir = (int)dashDir;

        // Pre-calculate dash velocity (impulse)
        float _dashVelocity = dashDir * pL.DashPower;

        float vx = dashDir * pL.DashPower; //dash velocity vector
        pL.CurrentVelocityX = vx;

        rb.linearVelocity = new Vector2(vx, 0);
        Physics2D.gravity = new Vector2(Physics2D.gravity.x, 0);

        pL.InputLocked = true;  // set when dash starts, unset when dash ends
    }
}
