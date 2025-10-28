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

    [Header("Air Drag")]
    [SerializeField] private float _airLinearDrag = 1.2f;     // c: scales with speed
    [SerializeField] private float _airQuadraticDrag = 0.0f;  // k: scales with speed^2 (set >0 for stronger drag at high speed)
    [SerializeField] private float _horizontalDragMultiplier = 0f; // extra tuning for x
    [SerializeField] private float _verticalDragMultiplier = 1.0f;   // extra tuning for y
    [SerializeField] private float _terminalFallSpeed = -22f;        // clamp max fall speed (downwards)

    [Header("Apex Glide (Before Falling)")]
    [SerializeField] private float _apexReleaseBoost = 1.0f;   // extra upward kick on release
    [SerializeField] private float _apexMinUpSpeed = 0.5f;     // ensure some initial up-speed
                                                               // Tip: set _timeBeforeFalling to ~0.08–0.18 for a tight, responsive feel (3f is huge)

    [Header("Runtime")]
    [SerializeField, ReadOnly] private int _jumpsRemaining;
    [SerializeField, ReadOnly] private float _coyoteTimer;
    [SerializeField, ReadOnly] private float _timeBeforeFallingTimer;

    [SerializeField, ReadOnly] bool _isBeforeFalling = false;
    [SerializeField, ReadOnly] bool _isFalling = false;

    [SerializeField, ReadOnly] bool _isJumpHeld;
    [SerializeField ,ReadOnly] bool _isGrounded;
    [SerializeField ,ReadOnly] bool _isJumping;
    [SerializeField, ReadOnly] private float _apexVyStart;



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
            EnterBeforeFalling(rb, addReleaseBoost: true);
            Debug.Log("<color=blue>before falling</color>");
        }
        // --- Apply variable gravity for better feel ---
        else if (_isFalling) // falling
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * _fallGravityMultiplier * dt;
            ApplyAirDrag(rb, dt);
            Debug.Log("<color=red>falling</color>");
        }
        else if (rb.linearVelocityY > 0)//v= v0 +at
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * _jumpMultiplier * dt;
            ApplyAirDrag(rb, dt);
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
            EnterBeforeFalling(_playerLocomotionState.Rb, addReleaseBoost: false);
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
                EnterBeforeFalling(_playerLocomotionState.Rb, addReleaseBoost: false);
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
    private void ApplyAirDrag(Rigidbody2D rb, float dt)
    {
        // no drag if we're in the "before falling" freeze window
        if (_isBeforeFalling) return;

        Vector2 v = rb.linearVelocity;
        float speed = v.magnitude;
        if (speed < 0.0001f) return;

        // Drag model: a = -(c * v + k * |v| * v)  (acceleration-like; units/tuning are arbitrary)
        Vector2 vDir = v / speed;
        float dragMag = _airLinearDrag * speed + _airQuadraticDrag * speed * speed;
        Vector2 dragAccel = -vDir * dragMag;

        // Optional axis scaling (lets you keep horizontal control while damping vertical more)
        dragAccel = new Vector2(dragAccel.x * _horizontalDragMultiplier,
                                dragAccel.y * _verticalDragMultiplier);

        // Integrate
        rb.linearVelocity += dragAccel * dt;

        // Terminal velocity clamp (downwards only)
        if (rb.linearVelocityY < _terminalFallSpeed)
            rb.linearVelocityY = _terminalFallSpeed;
    }
    private void EnterBeforeFalling(Rigidbody2D rb, bool addReleaseBoost)
    {
        if (_isBeforeFalling) return;

        float vy = Mathf.Max(0f, rb.linearVelocityY);
        if (addReleaseBoost)
            vy = Mathf.Max(vy, _apexMinUpSpeed) + _apexReleaseBoost;

        _apexVyStart = vy;
        rb.linearVelocityY = _apexVyStart;

        _isBeforeFalling = true;
        _isFalling = false;
        _timeBeforeFallingTimer = _timeBeforeFalling;

        // pause gravity globally (kept to match your current pattern)
        Physics2D.gravity = new Vector2(Physics2D.gravity.x, 0f);

        _playerLocomotionState.Anim.SetTrigger("JumpToFall");
        // Debug.Log("<color=blue>enter BEFORE FALLING</color>");
    }
}
