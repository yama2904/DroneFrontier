using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class BarrierWeakLaserController : NetworkBehaviour
{
    [SerializeField] BarrierWeakLaser barrierWeak = null;
    [SyncVar] GameObject createdLaser = null;

    //キャッシュ用
    Transform cacheTransform = null;
    Vector3 angle;

    Vector3 startPos = new Vector3(150f, 998f, -293f);
    [SerializeField, Tooltip("ギミックが発生する間隔")] float interval = 60f;
    [SerializeField, Tooltip("発生間隔に追加するランダムな時間の最大値")] int MaxAddInterval = 20;
    [SerializeField, Tooltip("ギミックの発生時間")] float time = 20f;


    //レーザーの角度
    const float MIN_ANGLE = 20f;
    const float MAX_ANGLE = 50f;

    //回転スピード
    const float MIN_SPEED = 72f;
    const float MAX_SPEED = 120f;
    float speed = 0;

    //レーザーを発生させるフラグ
    bool isBarrierWeak = false;


    [ServerCallback]
    public override void OnStartClient()
    {
        base.OnStartClient();

        //バリア弱体化レーザーの生成
        GameObject o = Instantiate(barrierWeak).gameObject;
        NetworkServer.Spawn(o, connectionToClient);
        createdLaser = o;
        cacheTransform = createdLaser.transform;

        Invoke(nameof(StartBarrierWeak), interval + Random.Range(0, MaxAddInterval + 1));
    }

    [ServerCallback]
    void Update()
    {
        if (isBarrierWeak)
        {
            angle.y += speed * Time.deltaTime;
            cacheTransform.localEulerAngles = angle;
            cacheTransform.position = startPos;
        }
    }

    void StartBarrierWeak()
    {
        //既にレーザー発生中なら処理しない
        if (isBarrierWeak) return;
        
        //1度発生する度に間隔を短くする
        interval -= 3;
        if (interval <= 1f)
        {
            interval = 1f;
        }
        MaxAddInterval--;
        if(MaxAddInterval <= 10)
        {
            MaxAddInterval = 10;
        }


        //レーザーを表示
        RpcSetLaserActive(true);
        
        //レーザーの始点を設定
        cacheTransform.position = startPos;

        //レーザーの角度の設定
        angle = cacheTransform.localEulerAngles;
        angle.y = 0;
        angle.x = Random.Range(MIN_ANGLE, MAX_ANGLE);
        cacheTransform.localEulerAngles = angle;

        //レーザーの回転スピードの設定
        speed = Random.Range(MIN_SPEED, MAX_SPEED);


        //フラグを立てる
        Invoke(nameof(SetIsBarrierWeakTrue), 1.5f);

        Invoke(nameof(StopBarrierWeak), time);
    }

    void SetIsBarrierWeakTrue()
    {
        isBarrierWeak = true;
    }

    void StopBarrierWeak()
    {
        RpcSetLaserActive(false);
        isBarrierWeak = false;

        Invoke(nameof(StartBarrierWeak), interval + Random.Range(0, MaxAddInterval + 1));
    }

    [ClientRpc]
    void RpcSetLaserActive(bool flag)
    {
        createdLaser.GetComponent<BarrierWeakLaser>().SetLaser(flag);
    }
}
