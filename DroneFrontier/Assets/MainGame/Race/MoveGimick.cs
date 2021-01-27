using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveGimick : MonoBehaviour
{
    enum Dir
    {
        DIR_X,
        DIR_Y,
        DIR_Z,

        NONE
    }
    [SerializeField] Dir type = Dir.NONE;
    [SerializeField] float speed = 1f;
    [SerializeField, Tooltip("移動距離")] float range = 7.5f;
    Transform cacheTransform = null;
    Vector3 initPos;

    void Start()
    {
        cacheTransform = transform;
        initPos = cacheTransform.position;
    }
    
    void Update()
    {
        if(type == Dir.DIR_X)
        {
            cacheTransform.position = new Vector3(initPos.x + Mathf.PingPong(Time.time * speed, range), initPos.y, initPos.z);
        }
        if(type == Dir.DIR_Y)
        {
            cacheTransform.position = new Vector3(initPos.x, initPos.y + Mathf.PingPong(Time.time * speed, range), initPos.z);
        }
        if(type == Dir.DIR_Z)
        {
            cacheTransform.position = new Vector3(initPos.x, initPos.y, initPos.z + Mathf.PingPong(Time.time * speed, range));
        }
    }
}
