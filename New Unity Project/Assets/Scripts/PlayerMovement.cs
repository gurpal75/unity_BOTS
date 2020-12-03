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
    public float attackRange = 2f;
    public LayerMask attackLayer;

    public float playerDmg = 1f;

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
    Vector2 attackDir;
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
            animator.SetTrigger("Attack");
            attack_timer = Time.time;
            attackDir = lastDirection;
        }
    }

    // This method is called by the animation
    void Attack()
    {
        // Make the slash face the player direction
        var angle = Vector2.SignedAngle(Vector2.up, attackDir.normalized);
        slashFX.transform.eulerAngles = new Vector3(0, 0, angle + slashAngleOffset);

        // Get all the entities around me
        var collisions = Physics2D.OverlapCircleAll(transform.position, attackRange, attackLayer);

        // Keep track if we hit something
        bool hitSomething = false;

        for (int i = 0; i < collisions.Length; ++i)
        {
            var entity = collisions[i].GetComponent<Entity>();
            Vector2 direction = (entity.transform.position - transform.position).normalized;

            // Make sure we are facing it before we attack it
            if (Vector2.Dot(direction, attackDir.normalized) > 0.5f)
            {
                entity.TakeDamage(playerDmg, direction * 50f);
                hitSomething = true;
            }
        }

        // Play the slash sound

        audioSource.pitch = Random.Range(0.9f, 1.1f);
        audioSource.clip = hitSomething ? 
            slash_hit[Random.Range(0, slash_hit.Length)] :
            slash[Random.Range(0, slash.Length)];
        audioSource.Play();
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
