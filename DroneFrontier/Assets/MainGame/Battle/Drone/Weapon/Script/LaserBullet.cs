using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Mirror;

public class LaserBullet : NetworkBehaviour
{
    //パラメータ
    [SerializeField, Tooltip("レーザのサイズ(Scaleの代わり")] float scale = 1f;
    public float ShotInterval { private get; set; } = 0;
    float shotCountTime = 0;
    [SerializeField, Tooltip("チャージ時間")] float chargeTime = 3.0f; 
    [SerializeField, Tooltip("レーザーの当たり判定の半径")] float lineRadius = 0.01f;
    [SerializeField, Tooltip("レーザーの射程")] float lineRange = 4.0f; 
    public bool IsShotBeam { get; private set; } = false;
    bool isStartCharge = false;
    AudioSource audioSource = null;
    public bool IsLocalPlayer = false;

    //親子付け用
    [SyncVar, HideInInspector] public uint parentNetId = 0;
    [SyncVar, HideInInspector] public Vector3 localPos = new Vector3();
    [SyncVar, HideInInspector] public Quaternion localRot = Quaternion.identity;


    //Charge用変数
    const int MAX_RATE_OVER_TIME = 128;   //チャージのパーティクルのrateOverTime最大値
    [SerializeField] ParticleSystem charge = null;
    ParticleSystem.EmissionModule chargeEmission;
    ParticleSystem.MinMaxCurve chargeMinmaxcurve;
    float rateovertimeAddAmout;    //割り算は重いので先に計算させる用
    bool isCharged;     //チャージし終わったらtrue

    //Start用変数
    [SerializeField] Transform startObjcectTransform = null;
    ParticleSystem[] startChilds;

    //Midway用変数
    [SerializeField] Transform midwayObjectTransform = null;
    [SerializeField] ParticleSystem lineParticle = null;
    [SerializeField] ParticleSystem thunderParticle = null;
    Transform lineTransform;
    Transform thunderTransform;
    float initThunderScaleZ;    //初期のthunderの長さ(敵にレーザーが当たった際に使う)
    float initThunderPosZ;      //初期のthunderのz座標

    //End用変数
    [SerializeField] Transform endObjectTransform = null;
    List<ParticleSystem> endChilds = new List<ParticleSystem>();


    public override void OnStartClient()
    {
        base.OnStartClient();
        GameObject parent = NetworkIdentity.spawned[parentNetId].gameObject;
        transform.SetParent(parent.transform);
        transform.localPosition = localPos;
        transform.localRotation = localRot;

        //長さをスケールに合わせる
        lineRange *= scale;

        Transform cacheTransform = transform;   //処理の軽量化用キャッシュ

        //Charge用処理//
        Vector3 cLocalScale = charge.transform.localScale;
        charge.transform.localScale = new Vector3(scale * cLocalScale.x, scale * cLocalScale.y, scale * cLocalScale.z);
        chargeEmission = charge.emission;
        chargeMinmaxcurve = chargeEmission.rateOverTime;
        rateovertimeAddAmout = MAX_RATE_OVER_TIME / chargeTime;  //1秒間で増加するRateOverTime量

        //Lineオブジェクト
        lineTransform = lineParticle.transform;

        //thunderオブジェクト
        thunderTransform = thunderParticle.transform;
        initThunderScaleZ = thunderTransform.localScale.z;   //初期の長さを保存
        initThunderPosZ = thunderTransform.localPosition.z;  //初期のz座標を保存


        //Start用処理//
        startChilds = new ParticleSystem[startObjcectTransform.childCount];
        for (int i = 0; i < startObjcectTransform.childCount; i++)
        {
            startChilds[i] = startObjcectTransform.GetChild(i).GetComponent<ParticleSystem>();
            Vector3 sLocalScale = startChilds[i].transform.localScale;
            startChilds[i].transform.localScale = new Vector3(scale * sLocalScale.x, scale * sLocalScale.y, scale * sLocalScale.z);
        }
        startObjcectTransform.localRotation = midwayObjectTransform.localRotation;  //Midwayと同じ向き


        //End用処理//
        for (int i = 0; i < endObjectTransform.childCount; i++)
        {
            ParticleSystem ps = endObjectTransform.GetChild(i).GetComponent<ParticleSystem>();
            Vector3 eLocalScale = ps.transform.localScale;
            ps.transform.localScale = new Vector3(scale * eLocalScale.x, scale * eLocalScale.y, scale * eLocalScale.z);
            endChilds.Add(ps);
        }
        //初期座標の保存
        endObjectTransform.localRotation = midwayObjectTransform.localRotation;   //Midwayと同じ向き

        audioSource = GetComponent<AudioSource>();
        ModifyLaserLength(lineRange);
        StopShot();
    }

    void Update()
    {
        //発射間隔の管理
        shotCountTime += Time.deltaTime;
        if (shotCountTime > ShotInterval)
        {
            shotCountTime = ShotInterval;
        }
    }

    //リストから必要な要素だけ抜き取る
    List<RaycastHit> FilterTargetRaycast(List<RaycastHit> hits, GameObject shooter)
    {
        //不要な要素を除外する
        return hits.Where(h => !h.transform.CompareTag(TagNameManager.ITEM))    //アイテム除外
                   .Where(h => !h.transform.CompareTag(TagNameManager.BULLET))  //弾丸除外
                   .Where(h => !h.transform.CompareTag(TagNameManager.GIMMICK)) //ギミック除外
                   .Where(h =>  //撃ったプレイヤーは当たり判定から除外
                   {
                       return !ReferenceEquals(h.transform.gameObject, shooter);
                   })
                   .Where(h =>  //ジャミングボットを生成したプレイヤーと打ったプレイヤーが同じなら除外
                   {
                       if (h.transform.CompareTag(TagNameManager.JAMMING_BOT))
                       {
                           return !ReferenceEquals(h.transform.GetComponent<JammingBot>().creater, shooter);
                       }
                       return true;
                   })
                   .ToList();  //リスト化 
    }

    //リスト内で最も距離が近いRaycastHitを返す
    void SearchNearestObject(out RaycastHit hit, List<RaycastHit> hits)
    {
        hit = hits[0];
        float minTargetDistance = float.MaxValue;   //初期化
        foreach (RaycastHit h in hits)
        {
            //距離が最小だったら更新
            if (h.distance < minTargetDistance)
            {
                minTargetDistance = h.distance;
                hit = h;
            }
        }
    }


    #region ModifyLaserLength

    void ModifyLaserLength(float length)
    {
        //Lineオブジェクト
        Vector3 lineScale = lineTransform.localScale;

        //そのままレーザーを太くすると他プレイヤーから見ると異常に太く見えるので
        //ローカルプレイヤーのみ太くする
        float localScaleY = length;
        if (IsLocalPlayer)
        {
            localScaleY *= 30;
        }
        lineTransform.localScale = new Vector3(length, localScaleY, 1.0f);

        //Thunderオブジェクト
        Vector3 thunderScale = thunderTransform.localScale;
        thunderTransform.localScale = new Vector3(thunderScale.x, thunderScale.y, initThunderScaleZ * length);
        Vector3 thunderPos = thunderTransform.localPosition;
        thunderTransform.localPosition = new Vector3(thunderPos.x, thunderPos.y, initThunderPosZ * length);
    }
    
    [Command(ignoreAuthority = true)]
    void CmdCallRpcModifyLaserLength(float length)
    {
        RpcModifyLaserLength(length);
    }

    [ClientRpc]
    void RpcModifyLaserLength(float length)
    {
        ModifyLaserLength(length);
    }

    #endregion

    #region StopShot

    //チャージとレーザーを止める
    public void StopShot()
    {
        CmdCallRpcStopAllParticle();

        //フラグの初期化
        isCharged = false;
        IsShotBeam = false;
        isStartCharge = false;
    }

    [Command(ignoreAuthority = true)]
    void CmdCallRpcStopAllParticle()
    {
        RpcStopAllParticle();
    }

    [ClientRpc]
    void RpcStopAllParticle()
    {
        charge.Stop();  //Chargeを止める

        //Chargeのパーティクルの発生量の初期化
        chargeMinmaxcurve.constant = 0;
        chargeEmission.rateOverTime = chargeMinmaxcurve;


        //Startを止める
        foreach (ParticleSystem p in startChilds)
        {
            p.Stop();
        }

        //Midwayを止める
        lineParticle.Stop();
        thunderParticle.Stop();

        //Endを止める
        foreach (ParticleSystem p in endChilds)
        {
            p.Stop();
        }

        //サウンドを止める
        audioSource.Stop();
        audioSource.loop = false;
    }

    #endregion


    public void Shot(GameObject shooter, float power)
    {
        #region Charge

        //チャージ処理
        if (!isCharged)
        {
            //攻撃開始時
            if (!isStartCharge)
            {
                CmdCallRpcChargePlay(true);
                isStartCharge = true;
            }

            //徐々にチャージのエフェクトを増す
            CmdCallRpcAddChargeParticle(rateovertimeAddAmout * Time.deltaTime);

            //MAX_RATE_OVER_TIME経ったら発射
            if (chargeEmission.rateOverTime.constant > MAX_RATE_OVER_TIME)
            {
                //チャージを止める
                CmdCallRpcChargePlay(false);

                //レーザーの発射
                CmdCallRpcLaserPlay();

                isCharged = true;
            }
        }

        #endregion

        #region Laser

        else
        {
            IsShotBeam = true;

            //前回ヒットして発射間隔分の時間が経過していなかったら当たり判定を行わない
            if (shotCountTime < ShotInterval) return;

            //レーザーの射線上にヒットした全てのオブジェクトを調べる
            var hits = Physics.SphereCastAll(
                lineTransform.position,    //レーザーの発射座標
                lineRadius,                //レーザーの半径
                lineTransform.forward,     //レーザーの正面
                lineRange)                 //射程
                .ToList();  //リスト化  

            hits = FilterTargetRaycast(hits, shooter);
            float lineLength = lineRange;   //レーザーの長さ

            //ヒット処理
            if (hits.Count > 0)
            {
                SearchNearestObject(out RaycastHit hit, hits);
                GameObject o = hit.transform.gameObject;    //名前省略

                if (o.CompareTag(TagNameManager.PLAYER))
                {
                    o.GetComponent<BattleDrone>().CmdDamage(power);
                }
                else if (o.CompareTag(TagNameManager.JAMMING_BOT))
                {
                    o.GetComponent<JammingBot>().CmdDamage(power);
                }

                //ヒットしたオブジェクトの距離とレーザーの長さを合わせる
                lineLength = hit.distance;

                //ヒットした場所にEndオブジェクトを移動させる
                CmdCallRpcSetEndObjectTransform(hit.point);


                shotCountTime = 0;  //発射間隔のカウントをリセット
            }
            else
            {
                //レーザーの末端にEndオブジェクトを移動
                CmdCallRpcSetEndObjectTransform(lineTransform.position + (lineTransform.forward * lineRange));
            }
            //レーザーの長さに応じてオブジェクトの座標やサイズを変える
            CmdCallRpcModifyLaserLength(lineLength);
        }

        #endregion
    }

    #region ChargePlay

    [Command(ignoreAuthority = true)]
    void CmdCallRpcChargePlay(bool flag)
    {
        RpcChargePlay(flag);
    }

    [ClientRpc]
    void RpcChargePlay(bool flag)
    {
        if (flag)
        {
            charge.Play();
            audioSource.clip = SoundManager.GetAudioClip(SoundManager.SE.BEAM_CAHRGE);
            audioSource.time = 0.2f;
            audioSource.volume = SoundManager.BaseSEVolume * 0.15f;
            audioSource.Play();
        }
        else
        {
            charge.Stop();
            audioSource.Stop();
        }
    }

    #endregion

    #region AddChargeParticle

    [Command(ignoreAuthority = true)]
    void CmdCallRpcAddChargeParticle(float add)
    {
        RpcAddChargeParticle(add);
    }

    [ClientRpc]
    void RpcAddChargeParticle(float add)
    {
        chargeMinmaxcurve.constant += add;
        chargeEmission.rateOverTime = chargeMinmaxcurve;
    }

    #endregion

    #region LaserPlay

    [Command(ignoreAuthority = true)]
    void CmdCallRpcLaserPlay()
    {
        RpcLaserPlay();
    }

    [ClientRpc]
    void RpcLaserPlay()
    {
        //Startの再生
        foreach (ParticleSystem p in startChilds)
        {
            p.Play();
        }

        //Midwayの再生
        lineParticle.Play();
        thunderParticle.Play();

        //Endの再生
        foreach (ParticleSystem p in endChilds)
        {
            p.Play();
        }

        //レーザー音の再生
        audioSource.clip = SoundManager.GetAudioClip(SoundManager.SE.BEAM);
        audioSource.volume = SoundManager.BaseSEVolume * 0.05f;
        audioSource.loop = true;
        audioSource.Play();
    }

    #endregion

    #region SetEndObjectTransform

    [Command(ignoreAuthority = true)]
    void CmdCallRpcSetEndObjectTransform(Vector3 pos)
    {
        RpcSetEndObjectTransform(pos);
    }

    [ClientRpc]
    void RpcSetEndObjectTransform(Vector3 pos)
    {
        endObjectTransform.position = pos;
    }

    #endregion
}
