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


        if (PlayerMovement.me.IsVandalising())
        {
            WalkTowards(PlayerMovement.me.transform.position, out Vector3 playerDir);

            if (playerDir.sqrMagnitude < 15f && fireTimer > fireRate)
            {
                Fire(playerDir);
                fireTimer = 0f;
            }

            fireTimer += Time.deltaTime;
        }
        else
        {
            WalkTowards(transform.position, out Vector3 dir);
        }
    }

    private void WalkTowards(Vector3 target, out Vector3 dir)
    {
        bool walking = false;

        dir = (target - transform.position);

        if (dir.sqrMagnitude > 5f)
        {
            rb.AddForce(dir.normalized * moveSpeed);
            walking = true;
        }

        animator.SetBool("walk", walking);
        sprite.flipX = dir.x < 0;
    }

    void Fire(Vector2 dir)
    {
        float angleForward = Vector2.SignedAngle(Vector2.up, dir);

        var b = Instantiate(bullet, transform.position, Quaternion.identity);
        b.transform.eulerAngles = new Vector3(0, 0, angleForward);
    }
}
