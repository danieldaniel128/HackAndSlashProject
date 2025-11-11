// EnemyHealth.cs
using System;
using UnityEngine;
using UnityEngine.UI; // for optional slider
// using TMPro; // uncomment if you want TMP labels

[RequireComponent(typeof(Collider2D))] // optional
public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] int _maxHealth = 100;
    [SerializeField] bool _useInvulnerability = true;
    [SerializeField] float _invulSeconds = 0;
    [SerializeField] bool _destroyOnDeath = true;
    [SerializeField] GameObject _deathVfxPrefab;
    [Header("Optional UI")]
    [SerializeField] Canvas _uiCanvas;
    [SerializeField] Image _healthFillImage; // assign Image type filled (or slider)

    float _currentHealth;
    bool _isInvulnerable;
    bool _isDead;

    public event Action<float, float> OnHealthChanged; // args: current, max
    public event Action OnDied;

    void Awake()
    {
        _currentHealth = _maxHealth;
        UpdateUI();
    }

    public bool TakeDamage(float amount, GameObject source = null)
    {
        if (_isDead) return false;
        if (_useInvulnerability && _isInvulnerable) return false;
        if (amount <= 0) return false;

        _currentHealth -= amount;
        if (_currentHealth < 0) _currentHealth = 0;

        var anim = GetComponent<Animator>();
        // Optional invul frames
        if (_useInvulnerability)
            StartCoroutine(InvulCoroutine());

        // Broadcast
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        UpdateUI();

        if (_currentHealth <= 0)
        {
            Die();
            return true;
        }
        else
        {
            if (anim) anim.SetTrigger("Hurt");
            return true;
        }

    }

    System.Collections.IEnumerator InvulCoroutine()
    {
        _isInvulnerable = true;
        yield return new WaitForSeconds(_invulSeconds);
        _isInvulnerable = false;
    }

    void UpdateUI()
    {
        if (_healthFillImage)
        {
            float t = _maxHealth > 0 ? (float)_currentHealth / _maxHealth : 0f;
            _healthFillImage.fillAmount = t;
        }

        if (_uiCanvas)
        {
            _uiCanvas.gameObject.SetActive(!_isDead);
        }
    }

    void Die()
    {
        if (_isDead) return;
        _isDead = true;
        OnDied?.Invoke();

        if (_deathVfxPrefab)
            Instantiate(_deathVfxPrefab, transform.position, Quaternion.identity);

        // TODO: more logic (drop loot, notify manager, play animation)
        var anim = GetComponent<Animator>();
        if (anim) anim.SetTrigger("Die");
        GetComponent<Collider2D>().enabled = false;
        if (_destroyOnDeath)
            Destroy(gameObject,1); // short delay for vfx/anim
        else
            gameObject.SetActive(false);
    }

    // Optional: public helper
    public void Heal(int amount)
    {
        if (_isDead || amount <= 0) return;
        _currentHealth = Mathf.Min(_currentHealth + amount, _maxHealth);
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        UpdateUI();
    }
}
public interface IDamageable
{
    /// <summary>Apply damage to this object. Returns true if damage was actually applied (not invulnerable / dead).</summary>
    bool TakeDamage(float amount, GameObject source = null);
}