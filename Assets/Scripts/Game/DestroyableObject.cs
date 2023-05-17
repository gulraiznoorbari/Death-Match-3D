using UnityEngine;
using Mirror;

public class DestroyableObject : NetworkBehaviour, IDamageable
{
    [SerializeField] int HealthMax = 100;
    int Health;

    private void Start()
    {
        SetMaxHP();
    }

    [Server]
    private void SetMaxHP()
    {
        Health = HealthMax;
    }

    [Server]
    public void Damage(int amount, uint shooterID)
    {
        Health -= amount;
        if (Health < 10)
        {
            Die();
        }
    }

    [Server]
    public void Die()
    {
        Destroy(gameObject);
    }
}
