using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Destructible2D;

public class Entity : MonoBehaviour
{
    Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void TakeDamage(float dmg, Vector2 knockback)
    {
        if (rb != null) rb.AddForce(knockback * dmg, ForceMode2D.Impulse);
    }
}
