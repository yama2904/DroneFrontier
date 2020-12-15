using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public string OwnerName { private get; set; } = "";
    public float SpeedPerSecond { get; set; } = 0;   //1秒間に進む量
    public float DestroyTime { get; set; } = 0;      //発射してから消えるまでの時間(射程)
    public float TrackingPower { get; set; } = 0;    //追従力

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
        if(totalTime > DestroyTime)
        {
            Destroy(gameObject);
        }
    }

    protected virtual void FixedUpdate()
    {
        if (target != null)
        {
            Vector3 diff = target.transform.position - transform.position;
            //視野内に敵がいる場合
            if (Vector3.Dot(transform.forward, diff) > 0)
            {
                float angle = Vector3.Angle(transform.forward, diff);
                if (angle > TrackingPower)
                {
                    angle = TrackingPower;
                }

                Vector3 axis = Vector3.Cross(transform.forward, diff);
                //左右の回転
                float x = angle * (axis.y < 0 ? -1 : 1);
                transform.RotateAround(transform.position, Vector3.up, x);

                //上下の回転
                float y = angle * (axis.x < 0 ? -1 : 1);
                transform.RotateAround(transform.position, Vector3.right, y);
            }
        }
        transform.position += transform.forward * SpeedPerSecond * Time.deltaTime;
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == Player.PLAYER_TAG)
        {
            if(other.name == OwnerName)
            {
                return;
            }
            Destroy(gameObject);
        }
    }
}
