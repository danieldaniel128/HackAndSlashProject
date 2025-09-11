using UnityEngine;

public class MoveAction : PlayerAction
{
    [Header("Movement Settings")]
    [SerializeField] private float _acceleration;
    [SerializeField] private float _deceleration;
    [SerializeField] private float _stopEpsilon;
    [SerializeField] private Vector2 _minMaxSpeed;
    [Header("Runtime")]
    [SerializeField, ReadOnly] float CurrentVelocityX;       // move velocity (no dash)

    public void MovementFixed(float dt)
    {
        PlayerLocomotion pL = _playerLocomotionState;
        // If input is locked (dash ,stunned, paused, etc.), stop movement
        if (pL.InputLocked)
            return;

        Rigidbody2D rb = pL.Rb;
        float moveInput = pL.MoveInput;

        

        // --- MOVEMENT ---
        if (Mathf.Abs(moveInput) > 0f)
        {
            // Accelerate horizontally
            CurrentVelocityX += moveInput * _acceleration * dt;

            // Clamp speed between Min and Max (allowing extra speed if dashed)
            float absCurrentVelocity = Mathf.Abs(CurrentVelocityX);
            float clampedMag = Mathf.Clamp(
                absCurrentVelocity,
                _minMaxSpeed.x,
                _minMaxSpeed.y
            );

            // Final velocity = input direction * clamped speed
            float desiredVel = moveInput * clampedMag;
            CurrentVelocityX = desiredVel;
        }
        else
        {
            float absCurrentVelocity = Mathf.Abs(CurrentVelocityX);
            // --- NO INPUT: DECELERATE ---
            if (absCurrentVelocity <= _stopEpsilon)
            {
                // Snap to 0 when very slow (prevents sliding forever)
                CurrentVelocityX = 0;
            }
            else
            {
                // Gradually slow down
                CurrentVelocityX +=
                    (-_playerLocomotionState.LastMoveInputNot0 * _deceleration * dt);
            }
        }

        // --- APPLY VELOCITY TO RIGIDBODY ---
        // Keep current Y velocity from Rigidbody (jump/fall unaffected)
        // Add dash impulse if HasDashed is true
        _playerLocomotionState.Rb.linearVelocity =
            new Vector2(CurrentVelocityX, rb.linearVelocityY);

        // Update animator with movement speed
        _playerLocomotionState?.Animator?.GroundedMovementAnimationUpdate(_playerLocomotionState.Rb.linearVelocity.magnitude);

        // Save last non-zero input for dash direction fallback
        if (_playerLocomotionState.MoveInput != 0)
            _playerLocomotionState.LastMoveInputNot0 = moveInput;
        _playerLocomotionState.Anim.SetBool("IsRunning", Mathf.Abs(CurrentVelocityX) > 0 ? true : false);
    }
    public void SetCurrentVelocity0()
    {

    }
}
