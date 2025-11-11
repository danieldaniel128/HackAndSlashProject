using System;
using Stateless;
using UnityEngine;

public class JumpAction : PlayerAction
{
    private enum State
    {
        Grounded,
        Airborne
    }

    private enum Trigger
    {
        Update,
        JumpRequested,
        Landed,
        LeftGround   // NEW: step off ledge
    }

    private StateMachine<State, Trigger> _fsm;

    [Header("Jump Settings")]
    [SerializeField] private float _jumpForce = 12f;
    [SerializeField] private int _maxJumps = 1;
    [SerializeField] private float _coyoteTime = 0.15f;
    [SerializeField] private float _jumpBufferTime = 0.1f;

    [SerializeField] private float _fallGravityMultiplier = 2.5f;
    [SerializeField] private float _lowJumpGravityMultiplier = 3.5f;
    [SerializeField] private float _terminalFallSpeed = -22f;

    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private LayerMask _ceilingLayer;

    [Header("Runtime (debug)")]
    [SerializeField, ReadOnly] private int _jumpsRemaining;
    [SerializeField, ReadOnly] private float _coyoteTimer;
    [SerializeField, ReadOnly] private float _jumpBufferTimer;
    [SerializeField, ReadOnly] private bool _isGrounded;
    [SerializeField, ReadOnly] private bool _isJumpHeld;

    [SerializeField, ReadOnly] private bool _playedJumpToFall;
    [SerializeField, ReadOnly] private bool _playedFalling;

    private Rigidbody2D _rb;
    private Animator _anim;
    private float _baseGravityScale;

    private void Start()
    {
        _rb = _playerLocomotionState.Rb;
        _anim = _playerLocomotionState.Anim;

        _baseGravityScale = _rb.gravityScale;
        _jumpsRemaining = _maxJumps;

        BuildStateMachine();
    }

    private void BuildStateMachine()
    {
        _fsm = new StateMachine<State, Trigger>(State.Grounded);

        _fsm.Configure(State.Grounded)
            .OnEntry(() =>
            {
                _isGrounded = true;
                _jumpsRemaining = _maxJumps;
                _coyoteTimer = 0f;
                _rb.gravityScale = _baseGravityScale;
                _anim.SetBool("OnGround", true);

                _playedJumpToFall = false;
                _playedFalling = false;
            })
            .InternalTransition(Trigger.Update, GroundedUpdate)
            .Permit(Trigger.JumpRequested, State.Airborne)
            .Permit(Trigger.LeftGround, State.Airborne);  // step off ledge -> airborne (no jump force)

        _fsm.Configure(State.Airborne)
            .OnEntryFrom(Trigger.JumpRequested, PerformJump)          // jump
            .OnEntryFrom(Trigger.LeftGround, EnterAirborneFromFall) // just walked off
            .InternalTransition(Trigger.Update, AirborneUpdate)
            .Permit(Trigger.Landed, State.Grounded)
            .PermitReentryIf(Trigger.JumpRequested, () => _jumpsRemaining > 0);

        _fsm.OnUnhandledTrigger((s, trig) =>
            Debug.Log($"[JumpFSM] Ignored {trig} in {s}"));

        // >>> Transition logger you asked for <<<
        _fsm.OnTransitioned(t =>
        {
            if (t.Source != t.Destination) // skip reentry if you want
                Debug.Log($"[FSM] <color=red>{t.Source}</color> --{t.Trigger}--> <color=green>{t.Destination}</color> @ {Time.time:F3}s");
        });
    }

    // ========== Public API ==========

    public void SetJumpHeld(bool isHeld)
    {
        _isJumpHeld = isHeld;
    }

    public void TryJump()
    {
        // buffer the request – consumed in Tick
        _jumpBufferTimer = _jumpBufferTime;
    }

    // call from FixedUpdate
    public void Tick(float dt)
    {
        if (!_isGrounded && _coyoteTimer > 0f)
            _coyoteTimer -= dt;

        if (_jumpBufferTimer > 0f)
        {
            _jumpBufferTimer -= dt;
            TryConsumeBufferedJump();
        }

        _fsm.Fire(Trigger.Update);
    }

    // ========== Collision ==========

    private void OnTriggerEnter2D(Collider2D collision)
    {
        int layer = collision.gameObject.layer;

        if (IsInLayerMask(layer, _groundLayer))
        {
            _isGrounded = true;
            _coyoteTimer = 0f;
            _fsm.Fire(Trigger.Landed);
        }

        if (IsInLayerMask(layer, _ceilingLayer))
        {
            // head bonk – zero upward velocity
            if (_rb.linearVelocityY > 0f)
                _rb.linearVelocityY = 0f;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        int layer = collision.gameObject.layer;
        if (IsInLayerMask(layer, _groundLayer))
        {
            _isGrounded = false;
            _coyoteTimer = _coyoteTime;
            _anim.SetBool("OnGround", false);

            // NEW: walking off a ledge -> go to Airborne (falling) state
            if (_fsm.State == State.Grounded)
                _fsm.Fire(Trigger.LeftGround);
        }
    }

    // ========== Internal FSM logic ==========

    private void GroundedUpdate()
    {
        // Grounded: nothing special – jump is handled via buffered request
    }

    private void AirborneUpdate()
    {
        var v = _rb.linearVelocity;

        bool rising = v.y > 0.01f;
        bool falling = v.y <= 0.01f;

        if (rising)
        {
            // Held = high jump, released = short jump
            _rb.gravityScale = _isJumpHeld ? _baseGravityScale : _lowJumpGravityMultiplier;
        }
        else if (falling)
        {
            // First time we start to really fall: "before falling" anim
            if (!_playedJumpToFall)
            {
                _anim.SetTrigger("JumpToFall");   // your "before falling" animation
                _playedJumpToFall = true;
            }

            // Strong fall anim after some downward speed
            if (!_playedFalling && v.y < -0.5f)
            {
                _anim.SetTrigger("Falling");
                _playedFalling = true;
            }

            _rb.gravityScale = _fallGravityMultiplier;

            if (v.y < _terminalFallSpeed)
            {
                v.y = _terminalFallSpeed;
                _rb.linearVelocity = v;
            }
        }
    }

    private void TryConsumeBufferedJump()
    {
        if (_jumpBufferTimer <= 0f)
            return;

        bool onGround = _isGrounded;
        bool canUseCoyote = !onGround && _coyoteTimer > 0f;
        bool hasExtraJump = !onGround && _jumpsRemaining > 0;

        if (!onGround && !canUseCoyote && !hasExtraJump)
            return;

        // we can jump now
        _jumpBufferTimer = 0f;
        _coyoteTimer = 0f;

        _fsm.Fire(Trigger.JumpRequested);
    }

    private void PerformJump()
    {
        _playedJumpToFall = false;
        _playedFalling = false;

        // reset vertical velocity then add jump impulse
        _rb.linearVelocityY = 0f;
        _rb.linearVelocity += Vector2.up * _jumpForce;

        _jumpsRemaining = Mathf.Max(0, _jumpsRemaining - 1);

        _rb.gravityScale = _baseGravityScale;

        _anim.SetTrigger("Jump");
        _anim.SetBool("OnGround", false);
    }

    private void EnterAirborneFromFall()
    {
        // step off platform – just start tracking as airborne
        _playedJumpToFall = false;
        _playedFalling = false;

        _rb.gravityScale = _baseGravityScale;
        _anim.SetBool("OnGround", false);
    }

    private static bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask & (1 << layer)) != 0;
    }
}
