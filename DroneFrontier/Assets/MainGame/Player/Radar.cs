using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class Radar : MonoBehaviour
{
    static Image radarMask;

    static GameObject markers = null;
    static GameObject enemyMarker = null;
    static GameObject itemMarker = null;

    static List<GameObject> targetObjects;
    static float searchRadius = 100.0f; //ロックオンする範囲
    static bool useRadar = true;        //ロックオンを使うか

    void Start()
    {
        radarMask = transform.Find("RadarMask/Image").GetComponent<Image>();
        radarMask.enabled = false;
        targetObjects = new List<GameObject>();

        markers = transform.Find("Markers").gameObject;
        enemyMarker = Resources.Load("EnemyMarker") as GameObject;
        itemMarker = Resources.Load("ItemMarker") as GameObject;
    }

    void Update()
    {

    }

    public static void StartRadar()
    {
        if (!useRadar)
        {
            return;
        }

        //名前省略
        GameObject camera = Camera.main.gameObject;

        //取得したRaycastHit配列から各RaycastHitクラスのgameObjectを抜き取ってリスト化する
        var hits = Physics.SphereCastAll(
            camera.transform.position,
            searchRadius,
            camera.transform.forward,
            0.01f).Select(h => h.transform.gameObject).ToList();

        hits = FilterTargetObject(hits);
        if (hits.Count() > 0)
        {
            foreach (GameObject hit in hits)
            {
                //既にレーダに表示中か調べる
                bool isTargeting = false;
                foreach (GameObject o in targetObjects)
                {
                    if (o.name == hit.name)
                    {
                        isTargeting = true;
                        break;
                    }
                }
                //既に表示中ならスキップ
                if (isTargeting)
                {
                    continue;
                }

                targetObjects.Add(hit);

                //プレイヤーかCPUなら赤い表示
                if (hit.tag == Player.PLAYER_TAG || hit.tag == CPUController.CPU_TAG)
                {
                    GameObject o = Instantiate(enemyMarker);
                    o.transform.parent = markers.transform;
                }
                if (hit.tag == Item.ITEM_TAG)
                {
                    GameObject o = Instantiate(itemMarker);
                    o.transform.parent = markers.transform;
                }
            }
        }
        radarMask.enabled = true;
    }

    public static void ReleaseRadar()
    {
        targetObjects.Clear();
        radarMask.enabled = false;

        //マーカーを全て削除する
        for (int i = markers.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(markers.transform.GetChild(i));
        }
    }

    //レーダーを使用するならtrue
    //禁止するならfalse
    public static void UseRadar(bool use)
    {
        if (!use)
        {
            ReleaseRadar();
        }
        useRadar = use;
    }

    //リストから必要な要素だけ抜き取る
    static List<GameObject> FilterTargetObject(List<GameObject> hits)
    {
        return hits.Where(h =>
        {
            //各要素の座標をビューポートに変換(画面左下が0:0、右上が1:1)して条件に合うものだけリストに詰め込む
            //タグがプレイヤーのオブジェクトに絞る
            //操作しているプレイヤーのオブジェクト名はロックオン対象外
            Vector3 screenPoint = Camera.main.WorldToViewportPoint(h.transform.position);
            return screenPoint.x > 0 && screenPoint.x < 0 && screenPoint.y > 0 && screenPoint.y < 0;
        }).Where(h => h.tag == Player.PLAYER_TAG)       //プレイヤーが対象
          .Where(h => h.tag == CPUController.CPU_TAG)   //CPUが対象
          .Where(h => h.tag == Item.ITEM_TAG)           //アイテムが対象
          .Where(h => h.name != Player.ObjectName)      //操作しているプレイヤーは除外
          .ToList();
    }
}
