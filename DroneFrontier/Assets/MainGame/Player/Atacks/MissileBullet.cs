using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissileBullet : Bullet
{
    protected override void Start()
    {
        target = LockOn.Target;
        transform.Rotate(new Vector3(90, 0, 0));
        totalTime = 0;
    }

    protected override void FixedUpdate()
    {
        //Quaternion rotation = Quaternion.LookRotation(diff);    //ターゲットへの向き
        //transform.rotation = Quaternion.Slerp(transform.rotation, rotation, trackingPower); 
        //Quaternion diffRotation = rotation * Quaternion.Inverse(transform.rotation);

        transform.Rotate(new Vector3(-90, 0, 0));
        base.FixedUpdate();
        transform.Rotate(new Vector3(90, 0, 0));
    }
}