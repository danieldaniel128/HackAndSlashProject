using UnityEngine;

public class AttackAction : PlayerAction
{
    [Header("Attack Settings")]
    [SerializeField] float _damage;
    [SerializeField] float _cooldown;
    [Header("Runtime")]
    [SerializeField, ReadOnly] float _attackTimer;
    [SerializeField, ReadOnly] bool _hasAttacked;
    private void Start()
    {
        _attackTimer = _cooldown;
    }
    public void HandleAttack()
    {
        if (_hasAttacked)
            if (_attackTimer > 0f)
                _attackTimer -= Time.deltaTime;
            else
            {
                _attackTimer = _cooldown;
                _hasAttacked = false; // Reset dash cooldown
            }
    }
    public void Attack()
    {
        if (_hasAttacked)
            return;
        _playerLocomotionState.Anim.SetTrigger("Attack");
        _hasAttacked = true;
        Debug.Log("<color=green>attacked</color>");
    }


}
