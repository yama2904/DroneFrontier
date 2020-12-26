using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class Radar : MonoBehaviour
{
    [SerializeField] GameObject playerInspector = null;
    static GameObject player = null;
    static Camera mainCamera = null;
    static Transform mainCameraTransform = null;

    static Image radarMask = null;
    static GameObject enemyMarker = null;
    static GameObject itemMarker = null;

    struct SearchData
    {
        public Transform target;
        public RectTransform marker;
    }
    static List<SearchData> searchDatas = new List<SearchData>();

    static float searchRadius = 100.0f; //ロックオンする範囲
    static bool useRadar = true;        //ロックオンを使うか

    void Awake()
    {
        player = playerInspector;
    }

    void Start()
    {
        mainCamera = Camera.main;
        mainCameraTransform = mainCamera.transform;
        GameObject o = transform.Find("RadarMask/Image").gameObject;
        radarMask = o.GetComponent<Image>();
        radarMask.enabled = false;
        searchDatas.Clear();

        enemyMarker = Resources.Load("EnemyMarker") as GameObject;
        itemMarker = Resources.Load("ItemMarker") as GameObject;
    }

    void Update()
    {
        foreach (SearchData s in searchDatas)
        {
            Vector3 screenPoint = mainCamera.WorldToViewportPoint(s.target.position);
            s.marker.position = new Vector3(Screen.width * screenPoint.x, Screen.height * screenPoint.y, 0);
        }
    }

    public static void StartRadar()
    {
        if (!useRadar)
        {
            return;
        }

        //取得したRaycastHit配列から各RaycastHitクラスのgameObjectを抜き取ってリスト化する
        var hits = Physics.SphereCastAll(
            mainCameraTransform.position,
            searchRadius,
            mainCameraTransform.forward,
            0.01f).Select(h => h.transform.gameObject).ToList();

        hits = FilterTargetObject(hits);
        if (hits.Count > 0)
        {
            int count = searchDatas.Count;  //hitsの要素を追加する前の要素数を保持
            bool[] isTargetings = new bool[count];  //前回はレーダーに照射されていたが今回は照射されていないオブジェクトがある場合は要素を保持しておく
            for (int i = 0; i < count; i++)
            {
                isTargetings[i] = false;
            }

            //hitsの中で既に照射済のものはスルーして新しいものだけ処理を行う
            foreach (GameObject hit in hits)
            {
                //既に照射済か調べる
                int index = searchDatas.FindIndex(s => ReferenceEquals(hit, s.target.gameObject));
                if (index >= 0 && index < count)
                {
                    isTargetings[index] = true;
                    continue;
                }

                SearchData sd = new SearchData();
                sd.target = hit.transform;
                //プレイヤーかCPUなら赤い表示
                if (hit.CompareTag(Player.PLAYER_TAG) || hit.CompareTag(CPUController.CPU_TAG))
                {
                    sd.marker = Instantiate(enemyMarker).transform.GetChild(0).GetComponent<RectTransform>();
                }
                else if (hit.CompareTag(Item.ITEM_TAG))
                {
                    sd.marker = Instantiate(itemMarker).transform.GetChild(0).GetComponent<RectTransform>();
                }

                //マーカーを移動させる
                Vector3 screenPoint = mainCamera.WorldToViewportPoint(sd.target.position);
                sd.marker.position = new Vector3(Screen.width * screenPoint.x, Screen.height * screenPoint.y, 0);

                searchDatas.Add(sd);
            }

            //前回はレーダーに照射されていたが今回は照射されていないオブジェクトを削除
            for (int i = count - 1; i >= 0; i--)
            {
                if (!isTargetings[i])
                {
                    Destroy(searchDatas[i].marker.parent.gameObject);
                    searchDatas.RemoveAt(i);
                }
            }
        }
        else
        {
            //何もレーダーに照射されていない場合はリストとマーカーを削除
            if (searchDatas.Count > 0)
            {
                foreach(SearchData sd in searchDatas)
                {
                    Destroy(sd.marker.parent.gameObject);
                }
                searchDatas.Clear();
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
            Destroy(s.marker.parent.gameObject);
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
            Vector3 screenPoint = mainCamera.WorldToViewportPoint(h.transform.position);
            return screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1 && screenPoint.z > 0;
        }).Where(h => !ReferenceEquals(h, player))   //操作しているプレイヤーは除外
          .Where(h => h.CompareTag(Player.PLAYER_TAG) || h.CompareTag(CPUController.CPU_TAG) || h.CompareTag(Item.ITEM_TAG))  //プレイヤーとCPUとアイテムが対象          
          .ToList();
    }
}
