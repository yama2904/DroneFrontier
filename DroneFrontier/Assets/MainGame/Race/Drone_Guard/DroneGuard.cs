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

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(TagNameManager.PLAYER))
        {
            Player p = collision.gameObject.GetComponent<Player>();
            if (!p.IsLocalPlayer) return;
            p.GetComponent<Rigidbody>().AddForce(p.transform.forward * power * -1, ForceMode.Impulse);
        }
    }
}
