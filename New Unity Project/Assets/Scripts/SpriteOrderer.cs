using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteOrderer : MonoBehaviour
{
    SpriteRenderer sr;

    [Range(0f, 1f)]
    public float bottomOffset;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        float off = bottomOffset * sr.bounds.size.y;
        sr.sortingOrder = Mathf.RoundToInt((sr.bounds.min.y + off) * 100f) * -1;
    }

    private void OnDrawGizmosSelected()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();

        float off = bottomOffset * sr.bounds.size.y;
        Gizmos.DrawSphere(new Vector3(transform.position.x, sr.bounds.min.y + off, transform.position.z), 0.1f);
    }
}
