using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProtestorBrain : MonoBehaviour
{
    public Skin[] skins;
    public SpriteRenderer player;
    public SpriteRenderer sign;
    public Rigidbody2D rb;
    public float moveSpeed = 100f;

    private bool walking = false;
    private int skinId = 0;
    private float timeOff = 0;

    private void Awake()
    {
        timeOff = Random.Range(0, 20);
        skinId = Random.Range(0, skins.Length);
        player.sprite = skins[skinId].sprites[0];
    }

    // Update is called once per frame
    void Update()
    {
        sign.enabled = true;
        sign.transform.localPosition = new Vector3(0, (1f + Mathf.Sin(Time.time * 5f + timeOff)) * 0.1f, 0);
    }

    private void WalkTowards(Vector3 target, out Vector3 dir)
    {
        walking = false;

        dir = (target - transform.position);

        if (dir.sqrMagnitude > 5f)
        {
            rb.AddForce(dir.normalized * moveSpeed);
            walking = true;
        }

        player.flipX = dir.x < 0;
    }
}

[System.Serializable]
public struct Skin
{
    public Sprite[] sprites;
}