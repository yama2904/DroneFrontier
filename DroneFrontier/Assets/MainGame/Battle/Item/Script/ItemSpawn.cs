using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ItemSpawn : NetworkBehaviour
{
    [Header("スポーン確率(0～1)")]
    [SerializeField] float spawnPercent = 0.5f;
    public float SpawnPercent { get { return spawnPercent; } }


    public override void OnStartClient()
    {
        base.OnStartClient();
        GetComponent<Renderer>().enabled = false;
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
