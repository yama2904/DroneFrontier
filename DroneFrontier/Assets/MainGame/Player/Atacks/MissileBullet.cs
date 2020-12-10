using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissileBullet : Bullet
{
    //[SerializeField] float speedPerSecond = 5.0f;
    //[SerializeField] float destroyTime = 2.0f;      //発射してから消えるまでの時間(射程)
    //[SerializeField] float trackingPower = 2.3f;    //追従力

   //GameObject target;
   //float totalTime;    //発射されてから経過した時間

    protected override void Start()
    {
        target = LockOn.Target;
        transform.Rotate(new Vector3(90, 0, 0));
        totalTime = 0;
    }

    //protected override void Update()
    //{
    //    totalTime += Time.deltaTime;
    //    if (totalTime > destroyTime)
    //    {
    //        Destroy(gameObject);
    //    }
    //}

    protected override void FixedUpdate()
    {
        //Quaternion rotation = Quaternion.LookRotation(diff);    //ターゲットへの向き
        //transform.rotation = Quaternion.Slerp(transform.rotation, rotation, trackingPower); 
        //Quaternion diffRotation = rotation * Quaternion.Inverse(transform.rotation);

        transform.Rotate(new Vector3(-90, 0, 0));

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
        //弾丸の後ろにターゲットがいた場合
        //if (Vector3.Dot(transform.forward, diff) < 0)
        //{
        //    transform.RotateAround(transform.position, Vector3.up, angleX);
        //    transform.RotateAround(transform.position, transform.right, angleY);
        //}
        transform.position += transform.forward * speedPerSecond * Time.deltaTime;
        transform.Rotate(new Vector3(90, 0, 0));
    }

    //private void OnTriggerEnter(Collider other)
    //{
    //    if (other.gameObject.tag == Player.PLAYER_TAG)
    //    {
    //        if (other.name == Player.ObjectName)
    //        {
    //            return;
    //        }
    //        Destroy(gameObject);
    //    }
    //}
}