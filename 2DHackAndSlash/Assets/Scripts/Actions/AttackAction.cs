using System;
using System.Collections;
using UnityEngine;

public class AttackAction : PlayerAction
{
    [Header("Attack Stages")]
    [SerializeField] private AttackStageData[] _stages;

    [Header("Combo Settings")]
    [SerializeField] private float _comboResetTime = 1.0f;   // (optional) time after combo ends to forget it
    [SerializeField] private float _delayAttackTime = 0.3f;  // delay before damage is actually applied

    [Header("References")]
    [SerializeField] private AttackHitNotifier _hitNotifier;

    [Header("Runtime (Debug)")]
    [SerializeField, ReadOnly] private int _currentStageIndex = -1;
    [SerializeField, ReadOnly] private float _stageTimer = 0f;
    [SerializeField, ReadOnly] private bool _isAttacking = false;
    [SerializeField, ReadOnly] private bool _inputQueued = false;
    [SerializeField, ReadOnly] private float _comboIdleTimer = 0f;

    public event Action OnAttackEnded;

    [Header("Debug Range")]
    [SerializeField] private float _attackRange = 1.2f;
    [SerializeField] private LayerMask _enemyLayer;
    [SerializeField] private Transform _attackOrigin;
    [SerializeField] private bool _useOverlapSphere = true;

    private Animator _anim;

    private void Awake()
    {
        _anim = _playerLocomotionState.Anim;

        if (_hitNotifier != null)
            _hitNotifier.OnHitTarget += HandleHitTarget;
    }

    private void OnDestroy()
    {
        if (_hitNotifier != null)
            _hitNotifier.OnHitTarget -= HandleHitTarget;
    }

    // ========== PUBLIC API ==========

    /// <summary>
    /// Called from the FSM or input when the player presses attack.
    /// </summary>
    public void Attack()
    {
        if (_stages == null || _stages.Length == 0)
        {
            Debug.LogWarning("[AttackAction] No stages configured.");
            return;
        }

        if (!_isAttacking)
        {
            // start from first stage
            StartStage(0);
        }
        else
        {
            // we are mid-attack: queue next stage
            _inputQueued = true;
        }
    }

    /// <summary>
    /// Call this from PlayerControllerStateless.Update while in Attacking state.
    /// </summary>
    public void Tick(float dt)
    {
        if (_isAttacking)
        {
            _stageTimer -= dt;

            if (_stageTimer <= 0f)
            {
                if (_inputQueued && _currentStageIndex < _stages.Length - 1)
                {
                    // continue combo
                    StartStage(_currentStageIndex + 1);
                }
                else
                {
                    // combo finished
                    FinishCombo();
                }
                OnAttackEnded?.Invoke();
            }
        }
        else
        {
            // optional: forget last combo index after some idle time
            if (_currentStageIndex >= 0)
            {
                _comboIdleTimer += dt;
                if (_comboIdleTimer >= _comboResetTime)
                {
                    _currentStageIndex = -1;
                    _comboIdleTimer = 0f;
                }
            }
        }
    }

    // ========== INTERNAL LOGIC ==========

    private void StartStage(int index)
    {
        index = Mathf.Clamp(index, 0, _stages.Length - 1);
        var stage = _stages[index];

        _isAttacking = true;
        _inputQueued = false;
        _currentStageIndex = index;
        _stageTimer = Mathf.Max(0.01f, stage.duration);
        _comboIdleTimer = 0f;

        if (_anim != null && !string.IsNullOrEmpty(stage.animTrigger))
            _anim.SetTrigger(stage.animTrigger);

        string color = index switch
        {
            0 => "green",
            1 => "yellow",
            2 => "red",
            _ => "white"
        };

        Debug.Log($"<color={color}>Attack {index + 1}!</color> Damage: {stage.damage}");

        if (_hitNotifier != null)
            _hitNotifier.TurnOnHitBox();
    }

    private void FinishCombo()
    {
        if (_hitNotifier != null)
            _hitNotifier.TurnOffHitBox();

        _isAttacking = false;
        _inputQueued = false;
        _stageTimer = 0f;
        _comboIdleTimer = 0f;
        _currentStageIndex = -1;

    }

    private float GetCurrentStageDamage()
    {
        if (_stages == null || _stages.Length == 0)
            return 0f;
        if (_currentStageIndex < 0 || _currentStageIndex >= _stages.Length)
            return 0f;

        return _stages[_currentStageIndex].damage;
    }

    private void HandleHitTarget(Collider2D hit)
    {
        float damage = GetCurrentStageDamage();
        if (damage <= 0f)
            return;

        var dmg = hit.GetComponentInParent<IDamageable>();
        if (dmg != null)
        {
            StartCoroutine(AttackDelay(() =>
            {
                dmg.TakeDamage(damage, gameObject);
                _hitNotifier.TurnOffHitBox();
                Debug.Log($"Dealt {damage} damage to {hit.name}");
            }));
        }
    }

    private IEnumerator AttackDelay(Action action)
    {
        if (_delayAttackTime > 0f)
            yield return new WaitForSeconds(_delayAttackTime);

        action?.Invoke();
    }

    // Debug: draw range
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        var origin = _attackOrigin ? _attackOrigin.position : transform.position;
        Gizmos.DrawWireSphere(origin, _attackRange);
    }
}
[Serializable]
public class AttackStageData
{
    public string name;                  // for debugging / inspector
    public float damage = 1f;            // damage dealt by this stage
    public float duration = 0.5f;        // how long this stage lasts (animation+cooldown)
    public string animTrigger = "Attack"; // e.g. "Attack_1", "Attack_2", "Attack_3"
}
