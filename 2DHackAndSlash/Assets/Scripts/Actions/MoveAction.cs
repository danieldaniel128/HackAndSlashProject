using UnityEngine;

public class MoveAction : PlayerAction
{
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
            _playerLocomotionState.CurrentVelocityX += moveInput * _playerLocomotionState.Acceleration * dt;

            // Clamp speed between Min and Max (allowing extra speed if dashed)
            float absCurrentVelocity = Mathf.Abs(_playerLocomotionState.CurrentVelocityX);
            float clampedMag = Mathf.Clamp(
                absCurrentVelocity,
                _playerLocomotionState.MinMaxSpeed.x,
                _playerLocomotionState.MinMaxSpeed.y
            );

            // Final velocity = input direction * clamped speed
            float desiredVel = moveInput * clampedMag;
            _playerLocomotionState.CurrentVelocityX = desiredVel;
        }
        else
        {
            float absCurrentVelocity = Mathf.Abs(_playerLocomotionState.CurrentVelocityX);
            // --- NO INPUT: DECELERATE ---
            if (absCurrentVelocity <= _playerLocomotionState.StopEpsilon)
            {
                // Snap to 0 when very slow (prevents sliding forever)
                _playerLocomotionState.CurrentVelocityX = 0;
            }
            else
            {
                // Gradually slow down
                _playerLocomotionState.CurrentVelocityX +=
                    (-_playerLocomotionState.LastMoveInputNot0 * _playerLocomotionState.Deceleration * dt);
            }
        }

        // --- APPLY VELOCITY TO RIGIDBODY ---
        // Keep current Y velocity from Rigidbody (jump/fall unaffected)
        // Add dash impulse if HasDashed is true
        _playerLocomotionState.Rb.linearVelocity =
            new Vector2(_playerLocomotionState.CurrentVelocityX, rb.linearVelocityY);

        // Update animator with movement speed
        _playerLocomotionState?.Animator?.GroundedMovementAnimationUpdate(_playerLocomotionState.Rb.linearVelocity.magnitude);

        // Save last non-zero input for dash direction fallback
        if (_playerLocomotionState.MoveInput != 0)
            _playerLocomotionState.LastMoveInputNot0 = moveInput;
        _playerLocomotionState.Anim.SetBool("IsRunning", Mathf.Abs(_playerLocomotionState.CurrentVelocityX) > 0 ? true : false);
    }
}
