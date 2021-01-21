using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class kabe : MonoBehaviour
{

    public float nowPosi;

    void Start()
    {
        nowPosi = this.transform.position.x;
    }

    void Update()
    {
        transform.position = new Vector3( nowPosi + Mathf.PingPong(Time.time * 7, 7.5f), transform.position.y, transform.position.z);
    }

}
