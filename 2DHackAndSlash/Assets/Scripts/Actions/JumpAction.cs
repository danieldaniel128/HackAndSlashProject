using UnityEngine;
using Fusion;

public class JumpAction : PlayerAction
{
    [Header("Jump Settings")]
    [SerializeField] private float _jumpForce = 12f;
    [SerializeField] private int _maxJumps = 1;
    [SerializeField] private float _coyoteTime = 0.15f;
    [SerializeField] private float _jumpBuffer = 0.1f;
    [SerializeField] private float _fallGravityMultiplier = 2f;
    [SerializeField] private float _lowJumpMultiplier = 2f;
    [SerializeField] private float _timeBeforeFalling = 3f;
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private LayerMask _ceilingLayer;
    [SerializeField] private float _stopEpsilon = 0.3f;

    [Header("Runtime")]
    [SerializeField, ReadOnly] private int _jumpsRemaining;
    [SerializeField, ReadOnly] private float _coyoteTimer;
    [SerializeField, ReadOnly] private float _jumpBufferTimer;
    [SerializeField, ReadOnly] private float _timeBeforeFallingTimer;

    [SerializeField, ReadOnly] private bool _isBeforeFalling = false;
    [SerializeField, ReadOnly] private bool _isFalling = false;

    private float _defaultGravityScale;

    private void Start()
    {
        _jumpsRemaining = _maxJumps;
        _coyoteTimer = _coyoteTime;
        _timeBeforeFallingTimer = _timeBeforeFalling;

        if (_playerLocomotionState.Rb != null)
        {
            _defaultGravityScale = _playerLocomotionState.Rb.gravityScale;
        }
    }

    public void TryJump()
    {
        _jumpBufferTimer = _jumpBuffer;
    }

    public void HandleJump(float dt)
    {
        if (!HasAuthority()) return; // only state authority executes

        var rb = _playerLocomotionState.Rb;

        // timers
        if (!_playerLocomotionState.IsGrounded && _coyoteTimer > 0f)
        {
            _coyoteTimer -= dt;
            if (_coyoteTimer <= 0f)
            {
                _coyoteTimer = 0f;
                if (_jumpsRemaining > 1)
                    _jumpsRemaining--;
            }
        }

        if (_jumpBufferTimer > 0f)
        {
            _jumpBufferTimer -= dt;
            if (_jumpBufferTimer <= 0f)
                _jumpBufferTimer = 0f;
        }

        if (_isBeforeFalling)
        {
            _timeBeforeFallingTimer -= dt;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.gravityScale = 0f;

            if (_timeBeforeFallingTimer <= 0f)
            {
                _timeBeforeFallingTimer = 0f;
                _isBeforeFalling = false;
                _isFalling = true;

                SetAnimTrigger("Falling");
                rb.gravityScale = _defaultGravityScale;
            }
        }

        // perform jump
        if (_jumpBufferTimer > 0f &&
            ((_coyoteTimer > 0f && _jumpsRemaining == 1) || (_maxJumps >= 2 && _jumpsRemaining >= 1)))
        {
            DoJump();
            _jumpBufferTimer = 0f;
        }
    }

    public void HandleFall(float dt)
    {
        if (!HasAuthority()) return;

        var rb = _playerLocomotionState.Rb;

        if (!_isBeforeFalling &&
            rb.linearVelocity.y > 0 &&
            rb.linearVelocity.y <= _stopEpsilon &&
            _playerLocomotionState.IsJumping)
        {

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.gravityScale = 0f;
            _isBeforeFalling = true;

            SetAnimTrigger("JumpToFall");
            _timeBeforeFallingTimer = _timeBeforeFalling;
        }
        else if (_isFalling)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y *
                           (_fallGravityMultiplier - 1f) * dt;
        }
        else if (rb.linearVelocity.y > 0 && !_playerLocomotionState.IsJumpHeld)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y *
                           (_lowJumpMultiplier - 1f) * dt;
        }
    }

    private void DoJump()
    {
        var rb = _playerLocomotionState.Rb;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.linearVelocity += Vector2.up * _jumpForce;

        _playerLocomotionState.IsJumping = true;
        _coyoteTimer = 0f;
        _jumpsRemaining--;

        SetAnimTrigger("Jump");
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!HasAuthority()) return;

        var rb = _playerLocomotionState.Rb;

        if (((1 << collision.gameObject.layer) & _groundLayer) != 0)
        {
            _playerLocomotionState.IsGrounded = true;
            _playerLocomotionState.IsJumping = false;
            _isFalling = false;
            _coyoteTimer = _coyoteTime;
            _jumpsRemaining = _maxJumps;

            SetAnimBool("OnGround", true);
        }

        if (((1 << collision.gameObject.layer) & _ceilingLayer) != 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            _isBeforeFalling = true;

            SetAnimTrigger("JumpToFall");
            _timeBeforeFallingTimer = _timeBeforeFalling;
            _jumpBufferTimer = 0f;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!HasAuthority()) return;

        var rb = _playerLocomotionState.Rb;

        if (((1 << collision.gameObject.layer) & _groundLayer) != 0)
        {
            _playerLocomotionState.IsGrounded = false;
            SetAnimBool("OnGround", false);

            if (!_playerLocomotionState.IsJumping)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                rb.gravityScale = 0f;
                _isBeforeFalling = true;

                SetAnimTrigger("JumpToFall");
                _timeBeforeFallingTimer = _timeBeforeFalling;
            }
        }
    }

    // --- helpers ---
    private void SetAnimTrigger(string trigger)
    {
        if (_playerLocomotionState.Anim != null)
            _playerLocomotionState.Anim.SetTrigger(trigger);
    }

    private void SetAnimBool(string param, bool value)
    {
        if (_playerLocomotionState.Anim != null)
            _playerLocomotionState.Anim.SetBool(param, value);
    }

    private bool HasAuthority()
    {
        // If this script is run under a NetworkBehaviour wrapper,
        // check Object.HasStateAuthority. For now return true in single-player.
        return true;
    }
}
