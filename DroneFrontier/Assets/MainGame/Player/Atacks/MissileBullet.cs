using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissileBullet : Bullet
{
    protected override void Start()
    {
        transform.Rotate(new Vector3(90, 0, 0));    //オブジェクトを90度傾ける
        totalTime = 0;
    }

    protected override void FixedUpdate()
    {
        //Quaternion rotation = Quaternion.LookRotation(diff);    //ターゲットへの向き
        //transform.rotation = Quaternion.Slerp(transform.rotation, rotation, trackingPower); 
        //Quaternion diffRotation = rotation * Quaternion.Inverse(transform.rotation);

        //90度傾けたままだと誘導がバグるので一旦直す
        transform.Rotate(new Vector3(-90, 0, 0)); 
        base.FixedUpdate();
        transform.Rotate(new Vector3(90, 0, 0));
    }
}