using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Destructible2D;

public class Entity : MonoBehaviour
{
    Rigidbody2D rb;
    public Collider2D[] colliders;

    public float health = 10f;
    public bool immortal = false;
    public bool destroyOnDeath = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void TakeDamage(float dmg, Vector2 knockback)
    {
        if (!immortal) health -= dmg;
        if (rb != null) rb.AddForce(knockback, ForceMode2D.Impulse);

        if (!Alive())
        {
            if (destroyOnDeath)
            {
                Destroy(gameObject);
            }
            else
            {
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.velocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                }
                foreach (var c in colliders)
                    c.enabled = false;
            }
        }
    }

    public bool Alive()
    {
        return health > 0;
    }
}
