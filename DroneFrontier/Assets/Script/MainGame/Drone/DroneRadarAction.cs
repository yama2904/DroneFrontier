using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class DroneRadarAction : MonoBehaviour
{
    [SerializeField, Tooltip("ドローンのカメラ")] 
    private Camera _camera = null;

    [SerializeField, Tooltip("レーダー中に使用するマスク")]
    private Image _radarMask = null;
 
    GameObject _enemyMarker = null;
    GameObject _itemMarker = null;

    struct SearchData
    {
        public Transform target;
        public RectTransform marker;
    }
    List<SearchData> searchDatas = new List<SearchData>();
    [SerializeField] float searchRadius = 300.0f; //照射する範囲
    [SerializeField, Tooltip("照射距離")] float maxDistance = 300f;

    //レーダーに照射しないオブジェクト
    List<GameObject> notRadarObjects = new List<GameObject>();
    
    private Transform _cameraTransform = null;

    const float ONE_SEARCH_TIME = 1f;
    const float TWO_SEARCH_TIME = 3;
    const float THREE_SEARCH_TIME = 7f;
    float deltaTime = 0;  //計測用


    void Awake()
    {
        _cameraTransform = _camera.transform;
        _radarMask.enabled = false;
        searchDatas.Clear();

        _enemyMarker = Resources.Load("EnemyMarker") as GameObject;
        _itemMarker = Resources.Load("ItemMarker") as GameObject;

        //自分を照射しない対象に入れる
        notRadarObjects.Add(gameObject);
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
        }).Where(h => h.CompareTag(TagNameConst.PLAYER) || h.CompareTag(TagNameConst.CPU) || h.CompareTag(TagNameConst.ITEM) || h.CompareTag(TagNameConst.JAMMING_BOT))  //照射対象を選択       
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


    public void UseRadar()
    {
        _radarMask.enabled = true;

        //レーダーを使用し続けた秒数に応じて照射距離が変動
        float searchLength = 0;
        deltaTime += Time.deltaTime;
        if (deltaTime < ONE_SEARCH_TIME) return;
        if (deltaTime < TWO_SEARCH_TIME)
        {
            searchLength = maxDistance / 3;
        }
        else if (deltaTime < THREE_SEARCH_TIME)
        {
            searchLength = (maxDistance / 3) * 2;
        }
        else if(deltaTime >= THREE_SEARCH_TIME)
        {
            deltaTime = THREE_SEARCH_TIME;
            searchLength = maxDistance;
        }


        //取得したRaycastHit配列から各RaycastHitクラスのgameObjectを抜き取ってリスト化する
        var hits = Physics.SphereCastAll(
            _cameraTransform.position,
            searchRadius,
            _cameraTransform.forward,
            searchLength).Select(h => h.transform.gameObject).ToList();

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
                if (hit.CompareTag(TagNameConst.PLAYER) || hit.CompareTag(TagNameConst.CPU) || hit.CompareTag(TagNameConst.JAMMING_BOT))
                {
                    sd.marker = Instantiate(_enemyMarker).transform.GetChild(0).GetComponent<RectTransform>();
                }
                else if (hit.CompareTag(TagNameConst.ITEM))
                {
                    sd.marker = Instantiate(_itemMarker).transform.GetChild(0).GetComponent<RectTransform>();
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
    }

    public void StopRadar()
    {
        _radarMask.enabled = false;
        deltaTime = 0;

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
