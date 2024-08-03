using System;
using UnityEngine;
using UnityEngine.Events;


public class Skeleton : MonoBehaviour, IEnemy
{
    public static Skeleton Instance { get; private set; }

    [HideInInspector] public UnityEvent OnDie;
    [HideInInspector] public UnityEvent OnAttack;
    [HideInInspector] public UnityEvent OnTakeDamage;
    [HideInInspector] public UnityEvent<OnHealthChangedEventArgs> OnHealthChanged;
    public class OnHealthChangedEventArgs : EventArgs
    {
        public float health;
        public float maxHealth;
    }

    [HideInInspector] public UnityEvent<OnStateChangedEventArgs> OnStateChanged;
    public class OnStateChangedEventArgs : EventArgs
    {
        public State state;
    }
    public enum State
    {
        Idle,
        Attack,
        Die
    }

    [Header("Refrences")]
    [SerializeField] private SkeletonVisual skeletonVisual;

    [Header("Attributes")]
    [SerializeField] private float maxHealth;
    [SerializeField] private float damagePower;
    [SerializeField] private float waitingToAttackTimerMax = 5f;

    private float health;
    private float waitingToAttackTimer;

    private bool onAttackState = false;

    private State state;

    private void Awake()
    {
        Instance = this;
    }
    private void Start()
    {
        Board.Instance.OnPotionParticlesSpwan += (sender, args) =>
        {
            TakeDamage(args.damage);
        };

        skeletonVisual.OnAttackAnimationEnd.AddListener(() =>
        {
            if (state == State.Attack)
            {
                state = State.Idle;
                OnStateChanged?.Invoke(new OnStateChangedEventArgs { state = state });

                onAttackState = false;
            }
        });

        health = maxHealth;
        OnHealthChanged?.Invoke(new OnHealthChangedEventArgs { health = this.health, maxHealth = this.maxHealth });

        waitingToAttackTimer = waitingToAttackTimerMax;
    }

    private void Update()
    {
        if (state == State.Die) return;

        switch (state)
        {
            case State.Idle:
                waitingToAttackTimer -= Time.deltaTime;
                if (waitingToAttackTimer < 0f)
                {
                    state = State.Attack;
                    OnStateChanged?.Invoke(new OnStateChangedEventArgs { state = state });

                    waitingToAttackTimer = UnityEngine.Random.Range(waitingToAttackTimerMax / 2, waitingToAttackTimerMax);
                }
                break;
            case State.Attack:
                if (!onAttackState)
                {
                    Attack();
                    onAttackState = true;
                }
                break;
            case State.Die:
                break;
            default:
                break;
        }
    }
    private void Attack()
    {
        if (state == State.Die) return;
        OnAttack?.Invoke();
    }
    private void TakeDamage(float damage)
    {
        health -= damage;
        OnHealthChanged?.Invoke(new OnHealthChangedEventArgs { health = this.health, maxHealth = this.maxHealth });
        if (health <= 0)
        {
            Die();
        }
        else
        {
            OnTakeDamage?.Invoke();
        }
    }

    private void Die()
    {
        state = State.Die;
        OnStateChanged?.Invoke(new OnStateChangedEventArgs { state = state });

        OnDie?.Invoke();
    }
}
