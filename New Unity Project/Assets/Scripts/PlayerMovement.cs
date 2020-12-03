using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Parameters")]

    public float moveSpeed = 5f;

    [Header("Components")]

    public Rigidbody2D rb;
    public Animator animator;

    Vector2 delta;

    void Update()
    {
        delta.x = -Input.GetAxisRaw("Horizontal");
        delta.y = Input.GetAxisRaw("Vertical");
    }


    void FixedUpdate()
    {
        bool moving = delta != Vector2.zero;

        animator.SetBool("Moving", moving);
        animator.SetFloat("Horizontal", delta.x);
        animator.SetFloat("Vertical", delta.y);

        if (moving)
        {
            rb.MovePosition(rb.position + delta * moveSpeed * Time.fixedDeltaTime);
        }
    }
}
