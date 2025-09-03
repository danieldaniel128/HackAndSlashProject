using UnityEngine;

public class MoveAction : PlayerAction
{
    public void MovementFixed(float dt)
    {
        // If input is locked (stunned, paused, etc.), stop movement
        if (_playerLocomotionState.InputLocked)
            return;

        Rigidbody2D rb = _playerLocomotionState.Rb;
        float moveInput = _playerLocomotionState.MoveInput;

        // --- DASH DIRECTION ---
        // Decide which direction to dash:
        // 1. If there is live input, use that
        // 2. Else, use the last non-zero input
        // 3. Else, if still moving, use current velocity sign
        // 4. Else, default to right (1)
        float dashDir =
            (_playerLocomotionState.MoveInput != 0 ? _playerLocomotionState.MoveInput :
            (_playerLocomotionState.LastMoveInputNot0 != 0 ? _playerLocomotionState.LastMoveInputNot0 :
            (_playerLocomotionState.CurrentVelocityX > 0.0001f ? _playerLocomotionState.CurrentVelocityX : 1)));

        // Store the dash direction as an integer (-1 or +1)
        _playerLocomotionState.DashDir = (int)dashDir;

        // Pre-calculate dash velocity (impulse)
        float _dashVelocity = dashDir * _playerLocomotionState.DashPower;

        // --- MOVEMENT ---
        if (Mathf.Abs(moveInput) > 0f)
        {
            // Accelerate horizontally
            _playerLocomotionState.CurrentVelocityX += moveInput * _playerLocomotionState.Acceleration * dt;

            // Apply extra deceleration if already dashed
            _playerLocomotionState.CurrentVelocityX +=
            _playerLocomotionState.HasDashed ? (dashDir * _playerLocomotionState.Deceleration * dt) : 0;

            // Clamp speed between Min and Max (allowing extra speed if dashed)
            float absCurrentVelocity = Mathf.Abs(_playerLocomotionState.CurrentVelocityX);
            float clampedMag = Mathf.Clamp(
                absCurrentVelocity,
                _playerLocomotionState.MinMaxSpeed.x,
                _playerLocomotionState.MinMaxSpeed.y + (_playerLocomotionState.HasDashed ? _playerLocomotionState.DashMaxSpeedBoost : 0)
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
            new Vector2(_playerLocomotionState.CurrentVelocityX, rb.linearVelocityY) +
            (_playerLocomotionState.HasDashed ? new Vector2(_dashVelocity, 0) : Vector2.zero);

        // Update animator with movement speed
        _playerLocomotionState?.Animator?.GroundedMovementAnimationUpdate(_playerLocomotionState.Rb.linearVelocity.magnitude);

        // Save last non-zero input for dash direction fallback
        if (_playerLocomotionState.MoveInput != 0)
            _playerLocomotionState.LastMoveInputNot0 = moveInput;
        _playerLocomotionState.Anim.SetBool("IsRunning", Mathf.Abs(_playerLocomotionState.CurrentVelocityX) > 0 ? true : false);
    }
}
