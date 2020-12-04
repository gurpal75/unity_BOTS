using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Entity), typeof(Animator), typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class NPCBrain : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float fireRate = 1f;
    public GameObject bullet;

    Animator animator;
    Entity entity;
    Rigidbody2D rb;
    SpriteRenderer sprite;

    public static int NPCCount = 0;

    float fireTimer;
    bool alive = true;

    private void Awake()
    {
        NPCCount++;

        entity = GetComponent<Entity>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
    }

    void FixedUpdate()
    {
        animator.SetBool("dead", !entity.Alive());

        if (!entity.Alive())
        {
            if (alive)
            {
                alive = false;
                NPCCount--;
            }
            return;
        }

        Vector3 playerDir = (PlayerMovement.me.transform.position - transform.position);
        bool walking = false;

        if (playerDir.sqrMagnitude > 5f)
        {
            rb.AddForce(playerDir.normalized * moveSpeed);
            walking = true;
        }

        sprite.flipX = playerDir.x < 0;

        animator.SetBool("walk", walking);

        if (playerDir.sqrMagnitude < 15f && fireTimer > fireRate)
        {
            Fire(playerDir);
            fireTimer = 0f;
        }

        fireTimer += Time.deltaTime;
    }

    void Fire(Vector2 dir)
    {
        float angleForward = Vector2.SignedAngle(Vector2.up, dir);

        var b = Instantiate(bullet, transform.position, Quaternion.identity);
        b.transform.eulerAngles = new Vector3(0, 0, angleForward);
    }
}
