using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BarrierWeakArea : MonoBehaviour
{
    [SerializeField] ParticleSystem lineParticle = null;
    [SerializeField] ParticleSystem thunderParticle = null;
    [SerializeField] float lineRadius = 0.01f;  //レーザーの半径
    [SerializeField] float lineRange = 1000;    //射程
    [SerializeField] float barrierWeakTime = 15.0f;  //バリアの弱体化時間

    //キャッシュ用のtransform
    Transform cacheTransform = null;    
    Transform lineTransform = null;     
    Transform thunderTransform = null;

    float initThunderScaleZ;    //初期のthunderの長さ(敵にレーザーが当たった際に使う)
    float initThunderPosZ;      //初期のthunderのz座標

    class HitPlayerData
    {
        public BasePlayer player;
        public float deltaTime; //計測用
    }

    //ヒットしているプレイヤーを格納
    List<HitPlayerData> hitPlayerDatas = new List<HitPlayerData>();

    void Start()
    {
        //キャッシュ用
        cacheTransform = transform;
        lineTransform = lineParticle.transform;
        thunderTransform = thunderParticle.transform;

        //初期値の保存
        initThunderPosZ = thunderTransform.localPosition.z;
        initThunderScaleZ = thunderTransform.localScale.z;

        //リスト初期化
        hitPlayerDatas.Clear();

        ModifyLaserLength(lineRange);
    }

    void Update()
    {
        foreach(HitPlayerData h in hitPlayerDatas)
        {
            if(h.deltaTime >= barrierWeakTime)
            {
                //バリアの弱体化をやめる

            }

            h.deltaTime += Time.deltaTime;
        }
    }

    void FixedUpdate()
    {
        var hits = Physics.SphereCastAll(
            cacheTransform.position,    //発射座標
            lineRadius,                 //レーザーの半径
            cacheTransform.forward,     //正面
            lineRange)                  //射程
            .ToList();  //リスト化

        hits = FilterTargetRaycast(hits);
        float lineLength = lineRange;   //レーザーの長さ

        //ヒット処理
        if (hits.Count > 0)
        {
            SearchNearestObject(out RaycastHit hit, hits);
            GameObject o = hit.transform.gameObject;    //名前省略

            if (o.CompareTag(Player.PLAYER_TAG) || o.CompareTag(CPUController.CPU_TAG))
            {
                BasePlayer bp = o.GetComponent<BasePlayer>();
                int index = -1;

                //既にリスト内に存在しているか調べる
                index = hitPlayerDatas.FindIndex(p => ReferenceEquals(p.player, bp));
                if (index == -1)
                {
                    //存在していなかったらバリアを弱体化させてリストに追加
                    //bp._Barrier.HP *= -0.5f;


                    HitPlayerData h = new HitPlayerData();
                    h.player = bp;
                    h.deltaTime = 0;
                    hitPlayerDatas.Add(h);
                }
                else
                {
                    //存在していたらカウントをリセット
                    hitPlayerDatas[index].deltaTime = 0;
                }
            }
            //ヒットしたオブジェクトの距離とレーザーの長さを合わせる
            lineLength = hit.distance;
        }
        //レーザーの長さを変える
        ModifyLaserLength(lineLength);
    }

    //レーザーの長さを変える
    void ModifyLaserLength(float length)
    {
        //Lineオブジェクト
        Vector3 lineScale = lineTransform.localScale;
        lineTransform.localScale = new Vector3(length, length, lineScale.z);

        //Thunderオブジェクト
        Vector3 thunderScale = thunderTransform.localScale;
        thunderTransform.localScale = new Vector3(thunderScale.x, thunderScale.y, initThunderScaleZ * length);
        Vector3 thunderPos = thunderTransform.localPosition;
        thunderTransform.localPosition = new Vector3(thunderPos.x, thunderPos.y, initThunderPosZ * length);
    }

    //リストから必要な要素だけ抜き取る
    List<RaycastHit> FilterTargetRaycast(List<RaycastHit> hits)
    {
        //不要な要素を除外する
        return hits.Where(h => !h.transform.CompareTag(Item.ITEM_TAG))      //アイテム除外
                   .Where(h => !h.transform.CompareTag(Bullet.BULLET_TAG))  //弾丸除外
                   .ToList();
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
}
