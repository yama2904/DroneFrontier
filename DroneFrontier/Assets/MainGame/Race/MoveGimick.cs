using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveGimick : MonoBehaviour
{
    enum Type
    {
        ONE,
        TWO,
        THREE,

        NONE
    }
    [SerializeField] Type type = Type.NONE;
    Transform cacheTransform = null;
    float nowPosi = 0;

    void Start()
    {
        cacheTransform = transform;
        nowPosi = cacheTransform.position.y;
    }
    
    void Update()
    {
        if(type == Type.ONE)
        {
            cacheTransform.position = new Vector3(nowPosi + Mathf.PingPong(Time.time * 7, 7.5f), cacheTransform.position.y, cacheTransform.position.z);
        }
        if(type == Type.TWO)
        {
            cacheTransform.position = new Vector3(nowPosi + Mathf.PingPong(Time.time * 7, 30f), cacheTransform.position.y, cacheTransform.position.z);
        }
        if(type == Type.THREE)
        {
            cacheTransform.position = new Vector3(cacheTransform.position.x, nowPosi + Mathf.PingPong(Time.time * 10, 7.5f), cacheTransform.position.z);
        }
    }
}
