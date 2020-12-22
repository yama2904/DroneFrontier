using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class Radar : MonoBehaviour
{
    static Image radarMask;

    static GameObject enemyMarker = null;
    static GameObject itemMarker = null;

    struct SearchData
    {
        public GameObject target;
        public GameObject marker;
    }
    static List<SearchData> searchDatas = new List<SearchData>();

    static float searchRadius = 100.0f; //ロックオンする範囲
    static bool useRadar = true;        //ロックオンを使うか

    void Start()
    {
        GameObject o = transform.Find("RadarMask/Image").gameObject;
        o.SetActive(true);
        radarMask = o.GetComponent<Image>();
        radarMask.enabled = false;
        searchDatas.Clear();

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

        int count = searchDatas.Count;  //hitsの要素を追加する前の要素数を保持
        bool[] isTargetings = new bool[count];  //前回はレーダーに照射されていたが今回は照射されていないオブジェクトがある場合は要素を保持しておく
        for (int i = 0; i < count; i++)
        {
            isTargetings[i] = false;
        }

        hits = FilterTargetObject(hits);
        if (hits.Count > 0)
        {

            //hitsの中で既に照射済のものはスルーして新しいものだけ処理を行う
            foreach (GameObject hit in hits)
            {
                //既に照射済か調べる
                int x = searchDatas.FindIndex(s => s.target.name == hit.name);
                if (x >= 0 && x < count)
                {
                    isTargetings[x] = true;
                    continue;
                }

                SearchData sd = new SearchData();
                sd.target = hit;
                //プレイヤーかCPUなら赤い表示
                if (hit.tag == Player.PLAYER_TAG || hit.tag == CPUController.CPU_TAG)
                {
                    sd.marker = Instantiate(enemyMarker);
                }
                if (hit.tag == Item.ITEM_TAG)
                {
                    sd.marker = Instantiate(itemMarker);
                }
                searchDatas.Add(sd);
            }
        }

        //前回はレーダーに照射されていたが今回は照射されていないオブジェクトを削除
        for (int i = count - 1; i >= 0; i--)
        {
            if (!isTargetings[i])
            {
                Destroy(searchDatas[i].marker);
                searchDatas.RemoveAt(i);
            }
        }
        radarMask.enabled = true;
    }

    public static void ReleaseRadar()
    {
        radarMask.enabled = false;

        //マーカーを全て削除する
        foreach (SearchData s in searchDatas)
        {
            Destroy(s.marker);
        }
        searchDatas.Clear();
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
            Vector3 screenPoint = Camera.main.WorldToViewportPoint(h.transform.position);
            return screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1 && screenPoint.z > 0;
        }).Where(h => h.name != Player.ObjectName)   //操作しているプレイヤーは除外
          .Where(h => h.tag == Player.PLAYER_TAG || h.tag == CPUController.CPU_TAG || h.tag == Item.ITEM_TAG)  //プレイヤーとCPUとアイテムが対象          
          .ToList();
    }
}
