using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Skeleton : MonoBehaviour, IEnemy
{
    public event EventHandler OnDie;
    public event EventHandler OnAttack;
    public event EventHandler OnTakeDamage;

    public event EventHandler<OnHealthChangedEventArgs> OnHealthChanged;
    public class OnHealthChangedEventArgs : EventArgs
    {
        public float health;
        public float maxHealth;
    }

    [Header("Attributes")]
    [SerializeField] private float maxHealth;
    [SerializeField] private float damagePower;

    private float health;

    private void Start()
    {
        Board.Instance.OnPotionParticlesSpwan += (sender, args) =>
        {
            TakeDamage(args.damage);
        };

        health = maxHealth;
        OnHealthChanged?.Invoke(this, new OnHealthChangedEventArgs { health = this.health, maxHealth = this.maxHealth });
    }
    private void Attack()
    {
        OnAttack?.Invoke(this, EventArgs.Empty);
    }
    private void TakeDamage(float damage)
    {
        health -= damage;
        OnHealthChanged?.Invoke(this, new OnHealthChangedEventArgs { health = this.health, maxHealth = this.maxHealth });
        if (health <= 0)
        {
            Die();
        }
        else
        {
            OnTakeDamage?.Invoke(this, EventArgs.Empty);
        }
    }

    private void Die()
    {
        OnDie?.Invoke(this, EventArgs.Empty);
    }
}
