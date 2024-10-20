using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Online;

public class BarrierWeakLaser : MonoBehaviour
{
    [SerializeField, Tooltip("レーザーの当たり判定の半径")] float lineRadius = 0.01f; 
    float lineRange = 3000f;  //最大射程
    [SerializeField, Tooltip("バリアの弱体化時間")] float barrierWeakTime = 15.0f;

    //キャッシュ用
    Transform cacheTransform = null;
    LineRenderer lineRenderer = null;

    class HitPlayerData
    {
        public DroneStatusAction player;
        public float deltaTime; //計測用
    }

    //ヒットしているプレイヤーを格納
    List<HitPlayerData> hitPlayerDatas = new List<HitPlayerData>();


    //レーザーの太さ
    const float MAX_WIDTH = 100f;
    float laserWidth = 0;
    bool setingWidth = false;  //太さを変えるならtrue

    //レーザーを発生させるか
    bool laserFlag = false;


    void Start()
    {
        //キャッシュ用
        cacheTransform = transform;
        lineRenderer = GetComponent<LineRenderer>();
        ModifyLaserLength(0);
    }

    void Update()
    {
        for (int i = hitPlayerDatas.Count - 1; i >= 0; i--)
        {
            HitPlayerData h = hitPlayerDatas[i];  //名前省略
            if (h.deltaTime >= barrierWeakTime)
            {
                if (h.player != null)
                {
                    //バリアの弱体化をやめる
                    h.player.UnSetBarrierWeak();
                }

                //リストから削除
                hitPlayerDatas.RemoveAt(i);
            }
            else
            {
                h.deltaTime += Time.deltaTime;
            }
        }
    }

    void FixedUpdate()
    {
        if (laserFlag)
        {
            //徐々に太くする
            if (setingWidth)
            {
                laserWidth += MAX_WIDTH * Time.deltaTime;
                if (laserWidth >= MAX_WIDTH)
                {
                    laserWidth = MAX_WIDTH;
                    setingWidth = false;
                }
                SetWidth(laserWidth);
            }
        }
        else
        {
            //徐々に細くする
            if (setingWidth)
            {
                laserWidth -= MAX_WIDTH * Time.deltaTime;
                if(laserWidth <= 0)
                {
                    laserWidth = 0;
                    setingWidth = false;
                    ModifyLaserLength(0);
                }
                SetWidth(laserWidth);
            }
            return;
        }

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

            if (o.CompareTag(TagNameConst.PLAYER))
            {
                DroneStatusAction player = o.GetComponent<DroneStatusAction>();
                if (player.isLocalPlayer)
                {
                    //既にリスト内に存在しているか調べる
                    int index = hitPlayerDatas.FindIndex(p => p.player.netId == player.netId);
                    if (index == -1)
                    {
                        //存在していなかったらバリアを弱体化
                        player.SetBarrierWeak();

                        //リストに追加
                        HitPlayerData h = new HitPlayerData();
                        h.player = player;
                        h.deltaTime = 0;
                        hitPlayerDatas.Add(h);
                    }
                    else
                    {
                        //存在していたらカウントをリセット
                        hitPlayerDatas[index].deltaTime = 0;
                    }
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
        Vector3 lineScale = cacheTransform.localScale;
        cacheTransform.localScale = new Vector3(lineScale.x, lineScale.y, length);
    }

    //リストから必要な要素だけ抜き取る
    List<RaycastHit> FilterTargetRaycast(List<RaycastHit> hits)
    {
        //不要な要素を除外する
        return hits.Where(h => !h.transform.CompareTag(TagNameConst.ITEM))    //アイテム除外
                   .Where(h => !h.transform.CompareTag(TagNameConst.BULLET))  //弾丸除外
                   .Where(h => !h.transform.CompareTag(TagNameConst.GIMMICK)) //ギミックエリア除外
                   .Where(h => !h.transform.CompareTag(TagNameConst.JAMMING)) //ジャミングエリア除外
                   .Where(h => !h.transform.CompareTag(TagNameConst.TOWER))   //タワー除外
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

    public void SetLaser(bool flag)
    {
        laserFlag = flag;
        setingWidth = true;
    }

    void SetWidth(float width)
    {
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
    }
}
