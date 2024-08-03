using System;
using UnityEngine;
using UnityEngine.Events;
public class Player : MonoBehaviour
{
    [HideInInspector] public UnityEvent<OnHealthChangedEventArgs> OnHealthChanged;
    public class OnHealthChangedEventArgs : EventArgs
    {
        public float health;
        public float maxHealth;
    }

    [Header("Refrences")]
    [SerializeField] private PlayerVisual playerVisual;

    [Header("Attributes")]
    [SerializeField] private float maxHealth;

    private float health;


    private void Start()
    {
        health = maxHealth;
        OnHealthChanged?.Invoke(new OnHealthChangedEventArgs { health = this.health, maxHealth = this.maxHealth });

        Skeleton.Instance.OnAttack.AddListener((args) =>
        {
            TakeDamage(args.damage);
        });
    }


    private void TakeDamage(float damage)
    {
        health -= damage;
        if (health <= 0)
        {
            health = 0;
            Die();
        }
        OnHealthChanged?.Invoke(new OnHealthChangedEventArgs { health = this.health, maxHealth = this.maxHealth });
    }
    private void Die()
    {
        Debug.Log("Die");
    }
}
