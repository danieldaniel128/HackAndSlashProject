using UnityEngine;

public class JumpAction : PlayerAction
{
    [Header("Jump Settings")]
    [Header("Multipliers")]
    [SerializeField] private float _fallGravityMultiplier = 2f; // Faster fall for snappy jumps
    [SerializeField] private float _jumpMultiplier = 2f;     // Extra gravity if player releases jump early

    [SerializeField, Range(0, 1)] private float _secondJumpMultiplier;
    [Header("Settings")]
    [SerializeField] private float _jumpForce = 12f;        // Initial upward force
    [SerializeField] private int _maxJumps = 1;             // e.g., 2 for double jump
    [SerializeField] private float _coyoteTime = 0.15f;     // Grace time after leaving ground
    [SerializeField] private float _timeBeforeFalling = 3f; // Time after jump before falling animation
    [SerializeField] float _stopEpsilon = 0.3f;
    [SerializeField] private LayerMask _groundLayer; // assign in inspector
    [SerializeField] private LayerMask _ceilingLayer;

    [Header("Runtime")]
    [SerializeField, ReadOnly] private int _jumpsRemaining;
    [SerializeField, ReadOnly] private float _coyoteTimer;
    [SerializeField, ReadOnly] private float _timeBeforeFallingTimer;

    [SerializeField, ReadOnly] bool _isBeforeFalling = false;
    [SerializeField, ReadOnly] bool _isFalling = false;

    [SerializeField, ReadOnly] bool _isJumpHeld;
    [SerializeField ,ReadOnly] bool _isGrounded;
    [SerializeField ,ReadOnly] bool _isJumping;
    private bool _jumpHeldLastFrame;
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
        if (!_isGrounded && _coyoteTimer > 0f)
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
        if (!_isBeforeFalling && ((!_isJumpHeld && _jumpHeldLastFrame) || (0 <= rb.linearVelocityY && rb.linearVelocityY <= _stopEpsilon)) && _isJumping)
        {
            Debug.Log($"<color=green>{(!_isJumpHeld && _jumpHeldLastFrame)}</color>");
            _jumpHeldLastFrame = false;
            rb.linearVelocityY = 0;
            Physics2D.gravity = new Vector2(Physics2D.gravity.x, 0);
            _isBeforeFalling = true;
            _playerLocomotionState.Anim.SetTrigger("JumpToFall");
            _timeBeforeFallingTimer = _timeBeforeFalling;
            Debug.Log("<color=blue>before falling</color>");
        }
        // --- Apply variable gravity for better feel ---
        else if (_isFalling) // falling
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * _fallGravityMultiplier * dt;
            Debug.Log("<color=red>falling</color>");
        }
        else if (rb.linearVelocityY > 0)//v= v0 +at
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * _jumpMultiplier * dt;
            Debug.Log($"<color=yellow>low jump</color>");
           // Debug.Log("<color=yellow>low jump</color>");
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
        _isJumping = true;
        _timeBeforeFallingTimer = 0f; // clamp
        _isBeforeFalling = false;
        _isFalling = false;
        _coyoteTimer = 0f;     // consume coyote time
        _jumpsRemaining--;     // consume jump
        _playerLocomotionState.Anim.SetTrigger("Jump");
        //Debug.Log("jumped");
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // --- Grounded reset ---
        // Check if the collided object's layer is in the ground mask
        if (((1 << collision.gameObject.layer) & _groundLayer) != 0)
        {
            // It's ground
            _isGrounded = true;
            _isJumping = false;
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
            //Debug.Log("<color=blue>touch ceiling</color>");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & _groundLayer) != 0)
        {
            // No longer touching ground
            _isGrounded = false;
            _playerLocomotionState.Anim.SetBool("OnGround", false);
            if(!_isJumping)
            {
                _playerLocomotionState.Rb.linearVelocityY = 0;
                Physics2D.gravity = new Vector2(Physics2D.gravity.x, 0);
                _isBeforeFalling = true;
                _playerLocomotionState.Anim.SetTrigger("JumpToFall");
                _timeBeforeFallingTimer = _timeBeforeFalling;
                //Debug.Log("<color=blue>before falling</color>");
            }
        }
    }
    public void SetJumpHeld(bool isJumpHeld) 
    {
        _isJumpHeld = isJumpHeld;
        if(_isJumpHeld)
            _jumpHeldLastFrame = true;
    }
}
