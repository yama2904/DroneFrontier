using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class DroneGuard : NetworkBehaviour
{
    enum Pattern
    {
        ONE,
        TWO,
        THREE,

        NONE
    }

    [SerializeField] Pattern pattern = Pattern.NONE;
    [SerializeField] float power = 200;

    Transform cacheTransform = null;
    float initPos;    

    void Start()
    {
        cacheTransform = transform;
        initPos = cacheTransform.position.y;
    }

    void Update()
    {
        if (pattern == Pattern.ONE)
        {
            cacheTransform.position = new Vector3(cacheTransform.position.x, initPos + Mathf.PingPong(Time.time * 10, 7.5f), cacheTransform.position.z);
        }
        if(pattern == Pattern.TWO)
        {
            cacheTransform.position = new Vector3(cacheTransform.position.x, initPos + Mathf.PingPong(Time.time * 15, 9.5f), cacheTransform.position.z);
        }
        if(pattern == Pattern.THREE)
        {
            cacheTransform.position = new Vector3(cacheTransform.position.x, initPos + Mathf.PingPong(Time.time * 5, 5.5f), cacheTransform.position.z);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(TagNameManager.PLAYER))
        {
            RaceDrone p = collision.gameObject.GetComponent<RaceDrone>();
            if (!p.isLocalPlayer) return;
            p.GetComponent<Rigidbody>().AddForce(p.transform.forward * power * -1, ForceMode.Impulse);
        }
    }
}
