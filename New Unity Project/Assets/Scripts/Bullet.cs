using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public Rigidbody2D rb;
    public float speed = 1f;

    void Start()
    {
        Vector3 playerDir = (PlayerMovement.me.transform.position - transform.position).normalized;
        rb.AddForce(playerDir * speed, ForceMode2D.Impulse);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        PlayerMovement.me.AttackCollider(collision.collider, 1f);
        Destroy(gameObject);
    }
}
