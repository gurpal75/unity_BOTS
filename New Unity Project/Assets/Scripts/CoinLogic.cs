using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinLogic : MonoBehaviour
{
    public Rigidbody2D rb;

    const float timerMax = 3f;
    float timer = timerMax;
    float timealive = 0f;

    // Start is called before the first frame update
    void Start()
    {
        rb.AddForce(Random.insideUnitCircle * 100f, ForceMode2D.Impulse);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float distancePlayer = Vector3.Distance(transform.position, PlayerMovement.me.transform.position);

        if (distancePlayer < .5f && timealive > 0.5f)
        {
            PlayerMovement.me.PickedUpCoin();
            Destroy(gameObject);
        }

        WalkTowards(PlayerMovement.me.transform.position, Mathf.Lerp(500, 50, timer / timerMax));

        timer -= Time.deltaTime;
        timealive += Time.deltaTime;
    }

    private void WalkTowards(Vector3 target, float moveSpeed)
    {
        var dir = (target - transform.position);
        rb.AddForce(dir.normalized * moveSpeed);
    }
}
