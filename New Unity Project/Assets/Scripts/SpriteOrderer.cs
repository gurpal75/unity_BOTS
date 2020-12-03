using Destructible2D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteOrderer : MonoBehaviour
{
    SpriteRenderer sr;
    D2dSorter sorter;
    MeshRenderer mr;

    [Range(0f, 1f)]
    public float bottomOffset;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        sorter = GetComponent<D2dSorter>();
        mr = GetComponent<MeshRenderer>();
    }

    void Update()
    {
        float off = bottomOffset * (sr == null ? mr.bounds.size.y : sr.bounds.size.y);

        if (sr != null) sr.sortingOrder = Mathf.RoundToInt((sr.bounds.min.y + off) * 100f) * -1;
        if (sorter != null) sorter.SortingOrder = Mathf.RoundToInt((mr.bounds.min.y + off) * 100f) * -1;
    }

    private void OnDrawGizmosSelected()
    {
        if (sr == null || sorter == null || mr == null)
            Awake();

        float off = bottomOffset * (sr == null ? mr.bounds.size.y : sr.bounds.size.y);
        Gizmos.DrawSphere(new Vector3(transform.position.x, (sr == null ? mr.bounds.min.y : sr.bounds.min.y) + off, transform.position.z), 0.1f);
    }
}
