using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] protected float speedPerSecond = 10.0f;
    [SerializeField] protected float destroyTime = 1.0f;      //発射してから消えるまでの時間(射程)
    [SerializeField] protected float trackingPower = 1.2f;    //追従力

    protected GameObject target;
    protected float totalTime;    //発射されてから経過した時間
    
    protected virtual void Start()
    {
        target = LockOn.Target;
        totalTime = 0;
    }

    protected virtual void Update()
    {
        totalTime += Time.deltaTime;
        if(totalTime > destroyTime)
        {
            Destroy(gameObject);
        }
    }

    protected virtual void FixedUpdate()
    {
        if (target != null)
        {
            Vector3 diff = target.transform.position - transform.position;
            //ミサイルの視野内に敵がいる場合
            if (Vector3.Dot(transform.forward, diff) > 0)
            {
                Vector3 axis = Vector3.Cross(transform.forward, diff);
                float angle = Vector3.Angle(transform.forward, diff);
                if (angle > trackingPower)
                {
                    angle = trackingPower;
                }

                //左右の回転
                float x = angle * (axis.y < 0 ? -1 : 1);
                transform.RotateAround(transform.position, Vector3.up, x);

                //上下の回転
                float y = angle * (axis.x < 0 ? -1 : 1);
                transform.RotateAround(transform.position, transform.right, y);
            }
        }
        transform.position += transform.forward * speedPerSecond * Time.deltaTime;
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == Player.PLAYER_TAG)
        {
            if(other.name == Player.ObjectName)
            {
                return;
            }
            Destroy(gameObject);
        }
    }
}
