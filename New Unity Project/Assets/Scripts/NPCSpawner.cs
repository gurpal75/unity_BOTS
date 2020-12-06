using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCSpawner : MonoBehaviour
{
    public GameObject prefab;
    public int count = 50;
    public AudioSource crowd;

    // Start is called before the first frame update
    void Start()
    {
        Respawn();
    }

    public void Respawn()
    {
        for (int i = 0; i < count; ++i)
        {
            Instantiate(prefab, transform.position + new Vector3(Random.Range(-8f, 8f), Random.Range(-1f, 2f), 0), Quaternion.identity);
        }
    }

    private void Update()
    {
        crowd.pitch = Time.timeScale;
    }
}
