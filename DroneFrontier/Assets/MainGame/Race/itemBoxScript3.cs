using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class itemBoxScript3 : MonoBehaviour
{

    public float nowPosi;

    void Start()
    {
        nowPosi = this.transform.position.y;
    }

    void Update()
    {
        transform.position = new Vector3(transform.position.x, nowPosi + Mathf.PingPong(Time.time * 5, 5.5f), transform.position.z);
    }

}