using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class Radar : MonoBehaviour, IRadar
{
    [SerializeField] Player player = null;
    [SerializeField] Camera _camera = null;
    Transform cameraTransform = null;

    Image radarMask = null;
    GameObject enemyMarker = null;
    GameObject itemMarker = null;

    struct SearchData
    {
        public Transform target;
        public RectTransform marker;
    }
    List<SearchData> searchDatas = new List<SearchData>();
    [SerializeField, Tooltip("照射距離")] float searchRadius = 100.0f; //照射する範囲

    //レーダーに照射しないオブジェクト
    List<GameObject> notRadarObjects = new List<GameObject>();


    void Awake()
    {
        cameraTransform = _camera.transform;
        GameObject o = transform.Find("RadarMask/Image").gameObject;
        radarMask = o.GetComponent<Image>();
        radarMask.enabled = false;
        searchDatas.Clear();

        enemyMarker = Resources.Load("EnemyMarker") as GameObject;
        itemMarker = Resources.Load("ItemMarker") as GameObject;

        //自分を照射しない対象に入れる
        notRadarObjects.Add(player.gameObject);
    }

    void Update()
    {
        foreach (SearchData s in searchDatas)
        {
            Vector3 screenPoint = _camera.WorldToViewportPoint(s.target.position);
            s.marker.position = new Vector3(Screen.width * screenPoint.x, Screen.height * screenPoint.y, 0);
        }
    }

    //リストから必要な要素だけ抜き取る
    List<GameObject> FilterTargetObject(List<GameObject> hits)
    {
        return hits.Where(h =>
        {
            //各要素の座標をビューポートに変換(画面左下が0:0、右上が1:1)して条件に合うものだけリストに詰め込む
            Vector3 screenPoint = _camera.WorldToViewportPoint(h.transform.position);
            return screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1 && screenPoint.z > 0;
        }).Where(h => h.CompareTag(TagNameManager.PLAYER) || h.CompareTag(TagNameManager.CPU) || h.CompareTag(TagNameManager.ITEM) || h.CompareTag(TagNameManager.JAMMING_BOT))  //照射対象を選択       
          .Where(h =>   //notRadarObjects内のオブジェクトがある場合は除外
          {
              if (notRadarObjects.FindIndex(o => ReferenceEquals(o, h.gameObject)) == -1)
              {
                  return true;
              }
              return false;
          })
        .ToList();
    }


    public void StartRadar()
    {
        //取得したRaycastHit配列から各RaycastHitクラスのgameObjectを抜き取ってリスト化する
        var hits = Physics.SphereCastAll(
            cameraTransform.position,
            searchRadius,
            cameraTransform.forward,
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
                if (hit.CompareTag(TagNameManager.PLAYER) || hit.CompareTag(TagNameManager.CPU) || hit.CompareTag(TagNameManager.JAMMING_BOT))
                {
                    sd.marker = Instantiate(enemyMarker).transform.GetChild(0).GetComponent<RectTransform>();
                }
                else if (hit.CompareTag(TagNameManager.ITEM))
                {
                    sd.marker = Instantiate(itemMarker).transform.GetChild(0).GetComponent<RectTransform>();
                }

                //マーカーを移動させる
                Vector3 screenPoint = _camera.WorldToViewportPoint(sd.target.position);
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
                foreach (SearchData sd in searchDatas)
                {
                    Destroy(sd.marker.parent.gameObject);
                }
                searchDatas.Clear();
            }
        }

        radarMask.enabled = true;
    }

    public void ReleaseRadar()
    {
        radarMask.enabled = false;

        //マーカーを全て削除する
        foreach (SearchData s in searchDatas)
        {
            Destroy(s.marker.parent.gameObject);
        }
        searchDatas.Clear();
    }

    //ロックオンしないオブジェクトを設定
    public void SetNotRadarObject(GameObject o)
    {
        //既にオブジェクトが含まれている場合はスルー
        if (notRadarObjects.FindIndex(listObject => ReferenceEquals(listObject, o)) == -1)
        {
            notRadarObjects.Add(o);
        }
    }

    //SetNotLockOnObjectで設定したオブジェクトをロックオンするように設定
    public void UnSetNotRadarObject(GameObject o)
    {
        int index = notRadarObjects.FindIndex(listObject => ReferenceEquals(listObject, o));
        if (index >= 0)
        {
            notRadarObjects.RemoveAt(index);
        }
    }
}
