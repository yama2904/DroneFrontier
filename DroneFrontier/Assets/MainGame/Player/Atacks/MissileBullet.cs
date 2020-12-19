using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissileBullet : Bullet
{
    [SerializeField] GameObject explosion = null;

    protected override void Start()
    {
        transform.Rotate(new Vector3(90, 0, 0));    //オブジェクトを90度傾ける
        totalTime = 0;
    }

    protected override void Update()
    {
        //発射されて一定時間経過したら爆破
        totalTime += Time.deltaTime;
        if (totalTime > DestroyTime)
        {
            createExplosion();
        }
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

    protected override void OnTriggerEnter(Collider other)
    {
        //当たり判定を行わないオブジェクトだったら処理をしない
        if (other.name == OwnerName)
        {
            return;
        }

        if (other.gameObject.tag == Player.PLAYER_TAG)
        {
            other.GetComponent<Player>().Damage(Power);
            createExplosion();
        }

        if (other.gameObject.tag == CPUController.CPU_TAG)
        {
            other.GetComponent<CPUController>().Damage(Power);
            createExplosion();
        }
    }

    void createExplosion()
    {
        GameObject o = Instantiate(explosion, transform.position, Quaternion.Euler(0, 0, 0)) as GameObject;
        o.GetComponent<Explosion>().OwnerName = OwnerName;
        Destroy(gameObject);
    }
}