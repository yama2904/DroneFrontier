using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneGuard : MonoBehaviour
{
    enum Pattern
    {
        ONE,
        TWO,
        THREE,

        NONE
    }

    [SerializeField] Pattern pattern = Pattern.NONE;

    float nowPosi;

    void Start()
    {
        nowPosi = this.transform.position.y;
    }

    void Update()
    {
        if (pattern == Pattern.ONE)
        {
            transform.position = new Vector3(transform.position.x, nowPosi + Mathf.PingPong(Time.time * 10, 7.5f), transform.position.z);
        }
        if(pattern == Pattern.TWO)
        {
            transform.position = new Vector3(transform.position.x, nowPosi + Mathf.PingPong(Time.time * 15, 9.5f), transform.position.z);
        }
        if(pattern == Pattern.THREE)
        {
            transform.position = new Vector3(transform.position.x, nowPosi + Mathf.PingPong(Time.time * 5, 5.5f), transform.position.z);
        }
    }
}
