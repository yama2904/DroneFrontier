using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class itemBoxScript : MonoBehaviour
{
    float nowPosi;

    void Start()
    {
        nowPosi = this.transform.position.y;
    }

    void Update()
    {
        transform.position = new Vector3(transform.position.x, nowPosi + Mathf.PingPong(Time.time * 10, 7.5f), transform.position.z);
    }
}