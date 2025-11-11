using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class AttackAction : PlayerAction
{
    [Header("Attack Settings")]
    [SerializeField] private float[] _attackDamages = { 1, 1.2f, 1.6f };   // damage per stage
    [SerializeField] private float[] _attackCooldowns = { 0.4f, 0.5f, 0.7f }; // cooldown per stage
    [SerializeField] private float _comboResetTime = 1.0f; // reset combo if idle this long
    [SerializeField] private float _delayAttackTime = 0.3f;
    [Header("References")]
    [SerializeField] private AttackHitNotifier _hitNotifier;

    [Header("Runtime (Debug)")]
    [SerializeField, ReadOnly] private int _currentAttackIndex = 0;
    [SerializeField, ReadOnly] private float _attackTimer = 0f;
    [SerializeField, ReadOnly] private bool _isAttacking = false;
    [SerializeField, ReadOnly] private bool _inputQueued = false;
    [SerializeField, ReadOnly] private float _comboTimer = 0f;
    public Action OnAttackEnded;

    [SerializeField] float _attackRange = 1.2f;
    [SerializeField] LayerMask _enemyLayer;
    [SerializeField] Transform _attackOrigin; // usually player transform or a child (null -> this.transform)
    [SerializeField] bool _useOverlapSphere = true; // set to false to use Physics.Raycast for directional attacks
    private void Awake()
    {
        _hitNotifier.OnHitTarget += HandleHitTarget;
    }

    private void OnDestroy()
    {
        _hitNotifier.OnHitTarget -= HandleHitTarget;
    }
    private void Update()
    {
        HandleAttack();
    }

    public void HandleAttack()
    {
        if (_isAttacking)
        {
            _attackTimer -= Time.deltaTime;

            // attack finished
            if (_attackTimer <= 0f)
            {
                if (_inputQueued && _currentAttackIndex < _attackDamages.Length - 1)
                {
                    StartAttack(_currentAttackIndex + 1); // continue combo
                }
                else
                {
                    ResetCombo();
                }
                OnAttackEnded?.Invoke();
                _hitNotifier.TurnOffHitBox();
            }
        }
        else
        {
            // track idle time to reset combo chain
            if (_currentAttackIndex > 0)
            {
                _comboTimer += Time.deltaTime;
                if (_comboTimer >= _comboResetTime)
                    ResetCombo();
            }
        }
    }

    public void Attack()
    {
        if (!_isAttacking)
        {
            StartAttack(0); // first hit
        }
        else
        {
            // queue next attack if still inside animation/cooldown
            _inputQueued = true;
        }
    }

    private void StartAttack(int index)
    {
        _isAttacking = true;
        _inputQueued = false;
        _currentAttackIndex = index;
        _attackTimer = _attackCooldowns[index];
        _comboTimer = 0f; // reset idle timer

        // trigger animation
        _playerLocomotionState.Anim.SetTrigger($"Attack");
        string color;
        switch (index)
        {
            case 0:
                color = "green";
                break;
            case 1:
                color = "yellow";
                break;
            case 2:
                color = "red";
                break;
            default:
                color = "green";
                break;
        }
        Debug.Log($"<color={color}>Attack {index + 1}!</color> Damage: {_attackDamages[index]}");
        _hitNotifier.TurnOnHitBox();
        // TODO: spawn hitbox / apply damage here
    }
    private void HandleHitTarget(Collider2D hit)
    {
        float damage = _attackDamages[_currentAttackIndex];
        var dmg = hit.GetComponentInParent<IDamageable>();
        if (dmg != null)
        {
            StartCoroutine(AttackDelay(() =>
            {
                dmg.TakeDamage(damage, gameObject);
                Debug.Log($"Dealt {damage} damage to {hit.name}");
            }));
        }
    }
    private System.Collections.IEnumerator AttackDelay(System.Action action)
    {
        yield return new WaitForSeconds(_delayAttackTime);
        action?.Invoke();
    }
    private void ResetCombo()
    {
        _isAttacking = false;
        _inputQueued = false;
        _currentAttackIndex = 0;
        _comboTimer = 0f;
    }
    // Debug: draw range
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        var origin = _attackOrigin ? _attackOrigin.position : transform.position;
        Gizmos.DrawWireSphere(origin, _attackRange);
    }
}
