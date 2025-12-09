using System;
using UnityEngine;
using Stateless;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyStateMachine : MonoBehaviour
{
    private enum State
    {
        Patrol,
        Chase,
        Deagro,
        Attack
    }

    private enum Trigger
    {
        PlayerInChaseRange,
        PlayerLost,
        PlayerInAttackRange,
        PlayerOutOfAttackRange,
        DeagroFinished
    }

    // Ron's Code 
    [SerializeField] private TMPro.TextMeshProUGUI _stateText;

    [Header("References")]
    [SerializeField] private Transform _player;
    [SerializeField] private Transform _patrolPointA;
    [SerializeField] private Transform _patrolPointB;

    [Header("Patrol")]
    [SerializeField] private float _patrolSpeed = 2f;
    [SerializeField] private float _patrolWaitTime = 2f;

    [Header("Chase")]
    [SerializeField] private float _chaseSpeed = 3.5f;
    [SerializeField] private float _chaseRange = 6f;

    [Header("Attack")]
    [SerializeField] private float _attackRange = 1.5f;
    [SerializeField] private float _attackCooldown = 1f;

    [Header("Deagro")]
    [SerializeField] private float _deagroDuration = 2f;


    [Header("Platform / Vertical")]
    [SerializeField] private float _verticalTolerance = 0.5f; // how far in Y still counts as "same platform"

    [Header("Debug")]
    [SerializeField] private bool _drawRanges = true;

    private Rigidbody2D _rb;

    private State _currentState;
    private StateMachine<State, Trigger> _fsm;

    // Patrol logic
    private int _currentPatrolIndex = 0; // 0 = A, 1 = B
    private float _patrolWaitTimer = 0f;

    // Deagro logic
    private float _deagroTimer = 0f;
    private Vector2 _lastKnownPlayerPosition;
    private Vector2 _deagroDirection;

    // Attack logic
    private float _attackTimer = 0f;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        _currentState = State.Patrol;
        _fsm = new StateMachine<State, Trigger>(() => _currentState, s => _currentState = s);

        ConfigureStateMachine();
    }

    private void ConfigureStateMachine()
    {
        // PATROL
        _fsm.Configure(State.Patrol)
            .OnEntry(StartPatrol)
            .Permit(Trigger.PlayerInChaseRange, State.Chase)
            .Permit(Trigger.PlayerInAttackRange, State.Attack);

        // CHASE
        _fsm.Configure(State.Chase)
            .OnEntry(StartChase)
            .Permit(Trigger.PlayerLost, State.Deagro)
            .Permit(Trigger.PlayerInAttackRange, State.Attack);

        // DEAGRO
        _fsm.Configure(State.Deagro)
            .OnEntry(StartDeagro)
            .Permit(Trigger.DeagroFinished, State.Patrol)
            .Permit(Trigger.PlayerInChaseRange, State.Chase);

        // ATTACK
        _fsm.Configure(State.Attack)
            .OnEntry(StartAttack)
            .Permit(Trigger.PlayerOutOfAttackRange, State.Chase)
            .Permit(Trigger.PlayerLost, State.Deagro);
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        UpdatePerception(); // distances & triggers

        switch (_currentState)
        {
            case State.Patrol:
                UpdatePatrol(dt);
                break;
            case State.Chase:
                UpdateChase(dt);
                break;
            case State.Deagro:
                UpdateDeagro(dt);
                break;
            case State.Attack:
                UpdateAttack(dt);
                break;
        }
        
        // ron's code
        _stateText.text = _currentState.ToString();
    }

    // =========================
    // PERCEPTION / TRIGGERS
    // =========================
    private void UpdatePerception()
    {
        if (_player == null)
            return;

        Vector2 me = transform.position;
        Vector2 p = _player.position;

        float dx = Mathf.Abs(p.x - me.x);
        float dy = Mathf.Abs(p.y - me.y);

        bool samePlatform = dy <= _verticalTolerance;
        bool inChaseRange = samePlatform && dx <= _chaseRange;
        bool inAttackRange = samePlatform && dx <= _attackRange;

        // Highest priority: attack
        if (inAttackRange)
        {
            TryFire(Trigger.PlayerInAttackRange);
        }
        else
        {
            if (_currentState == State.Attack && !inAttackRange && inChaseRange)
            {
                TryFire(Trigger.PlayerOutOfAttackRange);
            }
        }

        if (inChaseRange)
        {
            if (_currentState == State.Patrol || _currentState == State.Deagro)
            {
                TryFire(Trigger.PlayerInChaseRange);
            }

            // Update last known position only if same platform
            _lastKnownPlayerPosition = p;
        }
        else
        {
            // Only deagro if we were chasing/attacking AND lost horizontal+vertical contact
            if ((_currentState == State.Chase || _currentState == State.Attack))
            {
                TryFire(Trigger.PlayerLost);
            }
        }
    }


    private void TryFire(Trigger trigger)
    {
        if (_fsm.CanFire(trigger))
        {
            _fsm.Fire(trigger);
        }
    }

    // =========================
    // PATROL
    // =========================
    private void StartPatrol()
    {
        _patrolWaitTimer = 0f;
        SetHorizontalVelocity(0f);
    }

    private void UpdatePatrol(float dt)
    {
        Transform target = _currentPatrolIndex == 0 ? _patrolPointA : _patrolPointB;
        if (target == null)
            return;

        Vector2 dir = (target.position - transform.position);
        float dist = dir.magnitude;

        if (dist <= 0.05f)
        {
            // Reached patrol point -> idle
            SetHorizontalVelocity(0f);
            _patrolWaitTimer += dt;

            if (_patrolWaitTimer >= _patrolWaitTime)
            {
                _patrolWaitTimer = 0f;
                _currentPatrolIndex = 1 - _currentPatrolIndex; // switch A <-> B
            }
        }
        else
        {
            dir.Normalize();
            SetHorizontalVelocity(dir.x * _patrolSpeed);
            //FaceDirection(dir.x);
        }
    }

    // =========================
    // CHASE
    // =========================
    private void StartChase()
    {
        // Could play alert animation / sound here
    }

    private void UpdateChase(float dt)
    {
        if (_player == null)
        {
            SetHorizontalVelocity(0f);
            return;
        }

        Vector2 dir = _player.position - transform.position;
        dir.y = 0f; // only horizontal movement in platformer
        dir.Normalize();

        SetHorizontalVelocity(dir.x * _chaseSpeed);
        //FaceDirection(dir.x);
    }

    // =========================
    // DEAGRO (SEARCH)
    // =========================
    private void StartDeagro()
    {
        _deagroTimer = 0f;

        Vector2 from = transform.position;
        Vector2 to = _lastKnownPlayerPosition;
        _deagroDirection = (to - from).normalized;

        // If for some reason we don't have a direction, keep current facing
        if (_deagroDirection == Vector2.zero)
        {
            _deagroDirection = transform.localScale.x >= 0 ? Vector2.right : Vector2.left;
        }
    }

    private void UpdateDeagro(float dt)
    {
        _deagroTimer += dt;
        if (_deagroTimer >= _deagroDuration)
        {
            SetHorizontalVelocity(0f);
            TryFire(Trigger.DeagroFinished);
            return;
        }

        SetHorizontalVelocity(_deagroDirection.x * (_chaseSpeed * 0.5f)); // slower "search" walk
        //FaceDirection(_deagroDirection.x);
    }

    // =========================
    // ATTACK
    // =========================
    private void StartAttack()
    {
        _attackTimer = 0f;
        SetHorizontalVelocity(0f);

        // TODO: trigger your actual attack animation here
        // _anim.SetTrigger("Attack");
    }

    private void UpdateAttack(float dt)
    {
        _attackTimer -= dt;

        if (_attackTimer <= 0f)
        {
            PerformAttackHit();
            _attackTimer = _attackCooldown;
        }

        // Keep facing player while attacking
        if (_player != null)
        {
            float dirX = Mathf.Sign(_player.position.x - transform.position.x);
            //FaceDirection(dirX);
        }
    }

    private void PerformAttackHit()
    {
        // TODO: implement your hit logic here (OverlapCircle, etc.)
        // Example:
        // Collider2D hit = Physics2D.OverlapCircle(transform.position, _attackRange, _playerLayer);
        // if (hit != null) { hit.GetComponent<IDamageable>()?.TakeDamage(damage); }

        // Debug.Log("Enemy performed an attack.");
    }

    // =========================
    // MOVEMENT HELPERS
    // =========================
    private void SetHorizontalVelocity(float x)
    {
        if (_rb == null)
            return;

        _rb.linearVelocity = new Vector2(x, _rb.linearVelocity.y);
    }

    //private void FaceDirection(float dirX)
    //{
    //    if (Mathf.Approximately(dirX, 0f))
    //        return;

    //    Vector3 scale = transform.localScale;
    //    scale.x = Mathf.Abs(scale.x) * Mathf.Sign(dirX);
    //    transform.localScale = scale;
    //}

    private void OnDrawGizmosSelected()
    {
        if (!_drawRanges)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _chaseRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackRange);
    }
}
