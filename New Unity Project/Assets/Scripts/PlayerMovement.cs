using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Parameters")]

    public float moveSpeed = 5f;
    public float footstepsSoundRate = 0.2f;
    public float slashAngleOffset = 0f;
    public float attackRate = 0.1f;

    [Header("Sounds")]

    public AudioClip[] footsteps;
    public AudioClip[] slash;
    public AudioClip[] slash_hit;

    [Header("Components")]

    public Rigidbody2D rb;
    public Animator animator;
    public AudioSource audioSource;
    public SpriteRenderer slashFX;

    Vector2 delta;
    Vector2 lastDirection;
    float footstep_timer = 0f;
    float attack_timer = 0f;

    void Update()
    {
        delta.x = -Input.GetAxisRaw("Horizontal");
        delta.y = Input.GetAxisRaw("Vertical");

        if (delta != Vector2.zero)
            lastDirection = delta;

        if (Input.GetMouseButtonDown(0) && Time.time - attack_timer > attackRate)
        {
            Attack();
        }
    }

    void Attack()
    {
        attack_timer = Time.time;

        audioSource.pitch = Random.Range(0.9f, 1.1f);
        audioSource.clip = slash_hit[Random.Range(0, slash_hit.Length)];
        audioSource.Play();

        animator.SetTrigger("Attack");

        var angle = Vector2.SignedAngle(Vector2.up, lastDirection.normalized);

        slashFX.transform.eulerAngles = new Vector3(0, 0, angle + slashAngleOffset);
    }


    void FixedUpdate()
    {
        bool moving = delta != Vector2.zero;

        animator.SetBool("Moving", moving);
        animator.SetFloat("Horizontal", lastDirection.x);
        animator.SetFloat("Vertical", lastDirection.y);

        if (moving)
        {
            rb.MovePosition(rb.position + delta * moveSpeed * Time.fixedDeltaTime);

            footstep_timer += Time.fixedDeltaTime;

            if (footstep_timer > footstepsSoundRate)
            {
                audioSource.PlayOneShot(footsteps[Random.Range(0, footsteps.Length)]);
                footstep_timer = 0f;
            }
        }
    }
}
