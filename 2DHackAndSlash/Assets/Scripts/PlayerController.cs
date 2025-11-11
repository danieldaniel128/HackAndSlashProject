using System;
using Stateless;
using UnityEngine;
using UnityEngine.InputSystem;

public enum Trigger { DashPressed, DashFinished, AttackPressed, AttackFinished, Interrupt, Update }
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerControllerStateless : MonoBehaviour
{
    // ==== Existing action components (drag in Inspector) ====
    [SerializeField] private PlayerLocomotion _locomotion;   // same struct/class you already use
    [SerializeField] private MoveAction _move;
    [SerializeField] private JumpAction _jump;
    [SerializeField] private DashAction _dash;
    [SerializeField] private AttackAction _attack;

    // ========= Finite State Machine (Stateless) =========
    private enum State { Locomotion, Dashing, Attacking }

    private StateMachine<State, Trigger> _fsm;

    // Cache
    private Rigidbody2D _rb;

    // ===== Unity =====
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        // Wire actions to the same locomotion context
        _move.InitAction(_locomotion);
        _jump.InitAction(_locomotion);
        _dash.InitAction(_locomotion);
        _attack.InitAction(_locomotion);

        BuildStateMachine();
    }

    private void Update()
    {
        _fsm.Fire(Trigger.Update);
   
        RotatePlayer();

        // Jump processing that needs per-frame updates
    }

    private void FixedUpdate()
    {
        if (_fsm.State == State.Locomotion)
        {
            _move.MovementFixed(Time.fixedDeltaTime);
        }
        else if (_fsm.State == State.Attacking)
        {
            _rb.linearVelocity = new Vector2(0, _rb.linearVelocityY);
        }

        // OLD:
        // _jump.HandleJump(Time.fixedDeltaTime);

        // NEW:
        _jump.Tick(Time.fixedDeltaTime);
    }


    // ===== Input (call from PlayerInput actions) =====
    public void OnMove(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            _locomotion.MoveInput = ctx.ReadValue<float>();
            if (Mathf.Abs(_locomotion.MoveInput) < 0.35f) _locomotion.MoveInput = 0f;
            if (_locomotion.MoveInput != 0)
                _locomotion.LastMoveInputNot0 = _locomotion.MoveInput;
        }
        else if (ctx.canceled) _locomotion.MoveInput = 0f;
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (_fsm.State != State.Locomotion) // no jumping during dash / attack
            return;

        if (ctx.started) _jump.SetJumpHeld(true);
        else if (ctx.performed) _jump.TryJump();
        else if (ctx.canceled) _jump.SetJumpHeld(false);
    }

    public void OnDash(InputAction.CallbackContext ctx) { if (ctx.performed) _fsm.Fire(Trigger.DashPressed); }
    public void OnAttack(InputAction.CallbackContext ctx) { if (ctx.performed) _fsm.Fire(Trigger.AttackPressed); }

    // ===== Stateless wiring =====
    private void BuildStateMachine()
    {
        _fsm = new StateMachine<State, Trigger>(State.Locomotion);

        _fsm.Configure(State.Locomotion)
            .OnEntry(() => _locomotion.InputLocked = false)
            .InternalTransition(Trigger.Update, () => _move.MovementFixed(Time.deltaTime))
            .OnExit(() => { /* keep free */ })
            .PermitIf(Trigger.DashPressed, State.Dashing, () => _dash.CanDash)
            .Permit(Trigger.AttackPressed, State.Attacking);

        _fsm.Configure(State.Dashing)
            .OnEntry(() =>
            {
                _dash.OnDashEnded += OnDashEndedEvent;
                _dash.TryDash();         // will lock input internally and zero gravity in your DashAction
            })
            .InternalTransition(Trigger.Update, () => _dash.HandleDash())
            .Permit(Trigger.DashFinished, State.Locomotion)
            .OnExit(() =>
            {
                // Ensure gravity restored in case dash interrupted (DashAction handles it too)
                Physics2D.gravity = new Vector2(Physics2D.gravity.x, -9.81f);
                _dash.OnDashEnded -= OnDashEndedEvent;
            });

        _fsm.Configure(State.Attacking)
            .OnEntry(() =>
            {
                _attack.OnAttackEnded += OnAttackEndedEvent;
                _attack.Attack();       // kicks off first hit
                _locomotion.InputLocked = true; // freeze locomotion while attack anim plays
            })
            .InternalTransition(Trigger.AttackPressed, _attack.Attack)
            .Permit(Trigger.AttackFinished, State.Locomotion)
            .Ignore(Trigger.Update)
            .OnExit(() => 
            {
                _attack.OnAttackEnded -= OnAttackEndedEvent;
                _locomotion.InputLocked = false;
            });

        //_fsm.OnUnhandledTrigger((s, trig) =>
        //Debug.Log($"FSM ignored {trig} in {s}"));

        //_fsm.OnTransitioned(t => {
        //    //if (t.Source != t.Destination) // skip reentry if you want
        //    //    Debug.Log($"[FSM] {t.Source} --{t.Trigger}--> {t.Destination} @ {Time.time:F3}s");
        //});
    }

    // Call this from an Animation Event at the end of your combo clips
    public void OnAttackAnimationEnd()
    {
        if (_fsm.State == State.Attacking)
            _fsm.Fire(Trigger.AttackFinished);
    }

    // ===== Utils =====
    private void RotatePlayer()
    {
        var scale = transform.localScale;
        bool rotateLeft = _locomotion.MoveInput < 0f;

        if (_locomotion.MoveInput == 0 && _locomotion.LastMoveInputNot0 != 0)
            rotateLeft = _locomotion.LastMoveInputNot0 < 0f;

        scale.x = rotateLeft ? -1f : 1f;
        transform.localScale = scale;
    }
    private void OnDashEndedEvent() => _fsm.Fire(Trigger.DashFinished);
    private void OnAttackEndedEvent() => _fsm.Fire(Trigger.AttackFinished);

}

// Keep using your existing PlayerLocomotion definition
[Serializable]
public class PlayerLocomotion
{
    [Header("References")] public Rigidbody2D Rb; public Animator Anim;
    [Header("Runtime")] public bool InputLocked; public float MoveInput; public float LastMoveInputNot0;
}
