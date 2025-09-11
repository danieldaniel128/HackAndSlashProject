using UnityEngine;

public class JumpAction : PlayerAction
{
    [Header("Jump Settings")]
    [SerializeField] private float _jumpForce = 12f;        // Initial upward force
    [SerializeField] private int _maxJumps = 1;             // e.g., 2 for double jump
    [SerializeField] private float _coyoteTime = 0.15f;     // Grace time after leaving ground
    [SerializeField] private float _jumpBuffer = 0.1f;      // Buffer for input before landing
    [SerializeField] private float _fallGravityMultiplier = 2f; // Faster fall for snappy jumps
    [SerializeField] private float _lowJumpMultiplier = 2f;     // Extra gravity if player releases jump early
    [SerializeField, Range(0, 1)] private float _secondJumpMultiplier;
    [SerializeField] private float _timeBeforeFalling = 3f; // Time after jump before falling animation
    [SerializeField] private LayerMask _groundLayer; // assign in inspector
    [SerializeField] private LayerMask _ceilingLayer;
    [SerializeField] float _stopEpsilon = 0.3f;

    [Header("Runtime")]
    [SerializeField, ReadOnly] private int _jumpsRemaining;
    [SerializeField, ReadOnly] private float _coyoteTimer;
    [SerializeField, ReadOnly] private float _jumpBufferTimer;
    [SerializeField, ReadOnly] private float _timeBeforeFallingTimer;

    [SerializeField, ReadOnly] bool _isBeforeFalling = false;
    [SerializeField, ReadOnly] bool _isFalling = false;
    private float _gravityInitValue;
    private void Start()
    {
        _jumpsRemaining = _maxJumps;
        _coyoteTimer = _coyoteTime;
        _timeBeforeFallingTimer = _timeBeforeFalling;
        _gravityInitValue = Physics2D.gravity.y;
    }

    /// <summary>
    /// Called when jump input is pressed.
    /// Stores the intent to jump using buffer.
    /// </summary>
    public void TryJump()
    {
        if (((_coyoteTimer > 0f && _jumpsRemaining == 1) || (_maxJumps >= 2 && 1 <= _jumpsRemaining)))
        {
            DoJump();
        }
    }

    /// <summary>
    /// Called every Update().
    /// Handles buffer, coyote time, and variable jump.
    /// </summary>
    public void HandleJump(float dt)
    {
        if (_playerLocomotionState.InputLocked)
            return;
        else if (Physics2D.gravity.y == 0 && _isBeforeFalling == false)
            Physics2D.gravity = new Vector2(Physics2D.gravity.x, _gravityInitValue);
        Rigidbody2D rb = _playerLocomotionState.Rb;
        // --- Update timers ---
        if (!_playerLocomotionState.IsGrounded && _coyoteTimer > 0f)
        {
            _coyoteTimer -= dt;
            if (_coyoteTimer <= 0f)
            {
                _coyoteTimer = 0f; // clamp
            }
        }
        if (_isBeforeFalling)
        {
            _timeBeforeFallingTimer -= dt;
            rb.linearVelocityY = 0;
            Debug.Log("<color=lightblue>before falling</color>");
            if (_timeBeforeFallingTimer <= 0f)
            {
                _timeBeforeFallingTimer = 0f; // clamp
                _isBeforeFalling = false;
                _isFalling = true;
                _playerLocomotionState.Anim.SetTrigger("Falling");
                Physics2D.gravity = new Vector2(Physics2D.gravity.x, _gravityInitValue);
                Debug.Log("<color=red>start falling</color>");
            }
        }
        // --- Perform jump if buffered and allowed ---
    }
    public void HandleFall(float dt)
    {
        Rigidbody2D rb = _playerLocomotionState.Rb;
        if (!_isBeforeFalling &&  0 <= rb.linearVelocityY && rb.linearVelocityY <= _stopEpsilon && _playerLocomotionState.IsJumping == true)
        {
            rb.linearVelocityY = 0;
            Physics2D.gravity = new Vector2(Physics2D.gravity.x,0);
            _isBeforeFalling = true;
            _playerLocomotionState.Anim.SetTrigger("JumpToFall");
            _timeBeforeFallingTimer = _timeBeforeFalling;
            Debug.Log("<color=blue>before falling</color>");
        }
        // --- Apply variable gravity for better feel ---
        else if (_isFalling) // falling
        {
            Physics2D.gravity = new Vector2(Physics2D.gravity.x, _gravityInitValue);
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (_fallGravityMultiplier - 1f) * dt;
            Debug.Log("<color=red>falling</color>");
        }
        else if (rb.linearVelocityY > 0 && !_playerLocomotionState.IsJumpHeld) // rising but jump released
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (_lowJumpMultiplier - 1f) * dt;
            Debug.Log("<color=yellow>low jump</color>");
        }
    }

    /// <summary>
    /// Executes a jump.
    /// </summary>
    private void DoJump()
    {
        Rigidbody2D rb = _playerLocomotionState.Rb;
        rb.linearVelocity = new Vector2(rb.linearVelocityX, 0f); // reset vertical before jump
        rb.linearVelocity += Vector2.up * _jumpForce * ((_maxJumps == 2 && _jumpsRemaining == 1) ? _secondJumpMultiplier : 1);   // add jump impulse
        _playerLocomotionState.IsJumping = true;
        _timeBeforeFallingTimer = 0f; // clamp
        _isBeforeFalling = false;
        _isFalling = false;
        _coyoteTimer = 0f;     // consume coyote time
        _jumpsRemaining--;     // consume jump
        _playerLocomotionState.Anim.SetTrigger("Jump");
        Debug.Log("jumped");
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // --- Grounded reset ---
        // Check if the collided object's layer is in the ground mask
        if (((1 << collision.gameObject.layer) & _groundLayer) != 0)
        {
            // It's ground
            _playerLocomotionState.IsGrounded = true;
            _playerLocomotionState.IsJumping = false;
            _isFalling = false;
            _coyoteTimer = _coyoteTime;       // refresh coyote
            _jumpsRemaining = _maxJumps;
            _playerLocomotionState.Anim.SetBool("OnGround", true);
        }
        if(((1 << collision.gameObject.layer) & _ceilingLayer) != 0)
        {
            _playerLocomotionState.Rb.linearVelocityY = 0;
            //Physics2D.gravity = new Vector2(Physics2D.gravity.x,0);
            _isBeforeFalling = true;
            _playerLocomotionState.Anim.SetTrigger("JumpToFall");
            _timeBeforeFallingTimer = _timeBeforeFalling;
            _jumpBufferTimer = 0f;
            Debug.Log("<color=blue>touch ceiling</color>");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & _groundLayer) != 0)
        {
            // No longer touching ground
            _playerLocomotionState.IsGrounded = false;
            _playerLocomotionState.Anim.SetBool("OnGround", false);
            if(!_playerLocomotionState.IsJumping)
            {
                _playerLocomotionState.Rb.linearVelocityY = 0;
                Physics2D.gravity = new Vector2(Physics2D.gravity.x, 0);
                _isBeforeFalling = true;
                _playerLocomotionState.Anim.SetTrigger("JumpToFall");
                _timeBeforeFallingTimer = _timeBeforeFalling;
                Debug.Log("<color=blue>before falling</color>");
            }
        }
    }
}
