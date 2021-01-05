using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissileBullet : Bullet
{
    [SerializeField] Explosion explosion = null;
    float totalTime;    //発射させてから経過した時間

    protected override void Start()
    {
        cacheTransform = transform;
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
        cacheTransform.Rotate(new Vector3(-90, 0, 0)); 
        base.FixedUpdate();
        cacheTransform.Rotate(new Vector3(90, 0, 0));
    }

    protected override void OnTriggerEnter(Collider other)
    {
        //当たり判定を行わないオブジェクトだったら処理をしない
        if (ReferenceEquals(other.gameObject, Shooter))
        {
            return;
        }

        if (other.CompareTag(Player.PLAYER_TAG) || other.CompareTag(CPUController.CPU_TAG))
        {
            BasePlayer bp = other.GetComponent<BasePlayer>();
            
            bp.Damage(Power);
            createExplosion();
        }
        else if (other.CompareTag(JammingBot.JAMMING_BOT_TAG))
        {
            JammingBot jb = other.GetComponent<JammingBot>();
            if(jb.Creater == Shooter)
            {
                return;
            }
            jb.Damage(Power);
            createExplosion();
        }
    }

    void createExplosion()
    {
        Explosion e = Instantiate(explosion, cacheTransform.position, Quaternion.Euler(0, 0, 0));
        e.Shooter = Shooter;
        Destroy(gameObject);
    }

    public void InitRotate()
    {
        transform.localRotation = Quaternion.Euler(90, 0, 0);    //オブジェクトを90度傾ける
    }
}