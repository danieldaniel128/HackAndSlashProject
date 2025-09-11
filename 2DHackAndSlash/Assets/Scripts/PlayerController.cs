using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] PlayerLocomotion _playerLocomotionState;
    [SerializeField] MoveAction _moveAction;
    [SerializeField] JumpAction _jumpAction;
    [SerializeField] DashAction _dashAction;
    [SerializeField] AttackAction _attackAction;
    private void Awake()
    {
        _moveAction.InitAction(_playerLocomotionState);
        _jumpAction.InitAction(_playerLocomotionState);
        _dashAction.InitAction(_playerLocomotionState);
        _attackAction.InitAction(_playerLocomotionState);
    }
    void Update()
    {
        _dashAction.HandleDash();
        _attackAction.HandleAttack();
        RotatePlayer();
    }

    void FixedUpdate()
    {
        _dashAction.Dash();
        _moveAction.MovementFixed(Time.fixedDeltaTime);
        _jumpAction.HandleFall(Time.fixedDeltaTime);
        _jumpAction.HandleJump(Time.fixedDeltaTime);
    }


    public void OnMove(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            _playerLocomotionState.MoveInput = ctx.ReadValue<float>();
            if (Math.Abs(_playerLocomotionState.MoveInput) < 0.35f)
                _playerLocomotionState.MoveInput = 0; // Ignore small inputs to prevent jitter
        }
        else
            _playerLocomotionState.MoveInput = 0;
    }
    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
            _playerLocomotionState.IsJumpHeld = true;
        else if (ctx.performed)
            _jumpAction.TryJump();
        else if (ctx.canceled)
            _playerLocomotionState.IsJumpHeld = false;
    }
    public void OnDash(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
            return;
        _dashAction.TryDash();
    }
    public void OnAttack(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
            return;
        _attackAction.Attack();
    }
    private void RotatePlayer()
    {
        Vector3 newScaleRotated = transform.localScale;
        bool rotateLeft = _playerLocomotionState.MoveInput < 0f;
        //is idle?
        if (_playerLocomotionState.MoveInput == 0)
        {
            //has moved before?
            if (_playerLocomotionState.LastMoveInputNot0 != 0)
            {
                rotateLeft = _playerLocomotionState.LastMoveInputNot0 < 0f;
                newScaleRotated.x = rotateLeft ? -1f : 1f;
            }
        }
        else//isnt idle
        {
            newScaleRotated.x = rotateLeft ? -1f : 1f;
        }
        transform.localScale = newScaleRotated;
    }
}
[System.Serializable]
public class PlayerLocomotion
{
    [Header("References")]
    // External refs
    public Rigidbody2D Rb;
    public SpineAnimationController Animator;
    public Animator Anim;
    [Header("Movement Settings")]
    public float Acceleration;
    public float Deceleration;
    public float StopEpsilon;
    public Vector2 MinMaxSpeed;
    public float YSpeedModifier = 1f;

    // Dash config

    // Runtime
    [ReadOnly] public bool InputLocked;
    [ReadOnly] public float MoveInput;             // current frame input
    [ReadOnly] public float LastMoveInputNot0;     // cached non-zero input
    [ReadOnly] public float CurrentVelocityX;       // move velocity (no dash)

    [Header("Dash Settings")]
    public float DashPower = 10f;
    [ReadOnly] public int DashDir;              // direction chosen for dash
    [ReadOnly] public bool HasDashed = false;        // “is dashing this frame?”

    [Header("Jump Settings")]
    [ReadOnly] public bool IsJumpHeld;
    [ReadOnly] public bool IsGrounded;
    [ReadOnly] public bool IsJumping;
    //[ReadOnly] public ProcessStation ProcessedStation;
}