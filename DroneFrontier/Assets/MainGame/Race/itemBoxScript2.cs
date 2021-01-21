using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class itemBoxScript2 : MonoBehaviour
{

    public float nowPosi;

    void Start()
    {
        nowPosi = this.transform.position.y;
    }

    void Update()
    {
        transform.position = new Vector3(transform.position.x, nowPosi + Mathf.PingPong(Time.time * 15, 9.5f), transform.position.z);
    }

}
