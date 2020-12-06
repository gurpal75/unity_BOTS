using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Destructible2D;

public class Entity : MonoBehaviour
{
    public static List<Entity> entities;

    public Collider2D[] colliders;
    public float health = 10f;
    public bool immortal = false;
    public bool destroyOnDeath = false;
    public int coinSpawnOnDeath = 0;

    public static int Spawned = 0;
    public static int Destroyed = 0;

    D2dDestructible destructible;
    Rigidbody2D rb;

    private void Awake()
    {
        if (entities == null) entities = new List<Entity>();

        entities.Add(this);
        Spawned++;
        rb = GetComponent<Rigidbody2D>();
        destructible = GetComponent<D2dDestructible>();
    }

    public float GetDestructionLevel()
    {
        if (destructible != null)
            return 1f - (destructible.AlphaCount / (float)destructible.OriginalAlphaCount);

        return 0f;
    }

    public void TakeDamage(float dmg, Vector2 knockback)
    {
        if (!immortal) health -= dmg;
        if (rb != null) rb.AddForce(knockback, ForceMode2D.Impulse);

        var pm = GetComponent<PlayerMovement>();
        if (pm != null)
        {
            pm.ShakeCamera(0.1f);
            pm.Grunt();
        }

        if (!Alive())
        {
            if (destroyOnDeath)
            {
                entities.Remove(this);
                Destroy(gameObject);
                Destroyed++;
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

            if (colliders.Length != 0)
            {
                for (int i = 0; i < coinSpawnOnDeath; ++i)
                {
                    Collider2D c = colliders[Random.Range(0, colliders.Length)];

                    var p = new Vector3(
                        Random.Range(c.bounds.min.x, c.bounds.max.x),
                        Random.Range(c.bounds.min.y, c.bounds.max.y),
                        0);

                    PlayerMovement.me.SpawnCoin(p);
                }
            }
        }
    }

    public bool Alive()
    {
        return health > 0;
    }
}
