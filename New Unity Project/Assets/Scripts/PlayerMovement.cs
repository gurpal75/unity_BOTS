using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public Rigidbody2D rb;

    Vector2 delta;

    void Update()
    {
        delta.x = -Input.GetAxisRaw("Horizontal");
        delta.y = Input.GetAxisRaw("Vertical");
    }


    void FixedUpdate()
    {
        rb.MovePosition(rb.position + delta * moveSpeed * Time.fixedDeltaTime);
    }
}
