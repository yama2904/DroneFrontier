using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class DroneRadarComponent : MonoBehaviour
{
    /// <summary>
    /// レーダー照射中のアイテムを返す
    /// </summary>
    public List<GameObject> RadaringItems
    {
        get
        {
            List<GameObject> items = new List<GameObject>();
            foreach (GameObject key in _radaringMap.Keys)
            {
                if (_radaringMap[key].type == IRadarable.ObjectType.Item)
                {
                    items.Add(key);
                }
            }

            return items;
        }
    }

    /// <summary>
    /// レーダー照射中の敵を返す
    /// </summary>
    public List<GameObject> RadaringEnemys
    {
        get
        {
            List<GameObject> enemys = new List<GameObject>();
            foreach (GameObject key in _radaringMap.Keys)
            {
                if (_radaringMap[key].type == IRadarable.ObjectType.Enemy)
                {
                    enemys.Add(key);
                }
            }

            return enemys;
        }
    }

    [SerializeField, Tooltip("ドローンのカメラ")]
    private Camera _camera = null;

    [SerializeField, Tooltip("レーダー中に使用するマスク")]
    private Image _radarMask = null;

    [SerializeField, Tooltip("マーカーのCanvas")]
    private Transform _canvas = null;

    [SerializeField, Tooltip("エネミーマーカー")]
    private Image _enemyMarker = null;

    [SerializeField, Tooltip("アイテムマーカー")]
    private Image _itemMarker = null;

    [SerializeField, Tooltip("レーダー照射半径")]
    private float _radarRadius = 300.0f;

    [SerializeField, Tooltip("各レベルごとの時間")]
    private float[] _secPerLevel = null;

    [SerializeField, Tooltip("各レベルごとの照射距離")]
    private float[] _distancePerLevel = null;

    /// <summary>
    /// レーダー照射中オブジェクトのマーカー座標
    /// </summary>
    private Dictionary<GameObject, (IRadarable.ObjectType type, RectTransform marker)> _radaringMap = new Dictionary<GameObject, (IRadarable.ObjectType type, RectTransform marker)>();

    /// <summary>
    /// レーダー照射時間計測
    /// </summary>
    private float _radarTimer = 0;

    /// <summary>
    /// レーダー中であるか
    /// </summary>
    private bool _startedRadar = false;

    /// <summary>
    /// 現在のレベル
    /// </summary>
    private int _nowLevel = 0;

    /// <summary>
    /// 一時的なロックオン無効の重複カウント
    /// </summary>
    private int _disabledCount = 0;

    private Transform _cameraTransform = null;

    /// <summary>
    /// レーダー照射開始
    /// </summary>
    public void StartRadar()
    {
        if (!enabled) return;
        _startedRadar = true;

        if (_radarMask != null)
        {
            _radarMask.enabled = true;
        }
    }

    /// <summary>
    /// レーダー照射停止
    /// </summary>
    public void StopRadar()
    {
        if (!enabled) return;
        _nowLevel = 0;
        _startedRadar = false;
        _radarTimer = 0;
        DestroyAllMarkers();
        
        if (_radarMask != null)
        {
            _radarMask.enabled = false;
        }
    }

    /// <summary>
    /// 一時的にロックオン無効を設定する
    /// </summary>
    public void QueueDisabled()
    {
        if (_disabledCount == 0)
        {
            StopRadar();
        }

        _disabledCount++;
        enabled = false;
    }

    /// <summary>
    /// 一時的なロックオン無効を解除する。ロックオン無効が重複してる場合は無効のままとなる。
    /// </summary>
    public void DequeueDisabled()
    {
        _disabledCount--;
        if (_disabledCount <= 0)
        {
            _disabledCount = 0;
            enabled = true;
        }
    }

    private void Awake()
    {
        _cameraTransform = _camera.transform;
        if (_radarMask != null)
        {
            _radarMask.enabled = false;
        }
    }

    private void Update()
    {
        // レーダー照射時間計測
        if (!_startedRadar) return;
        if (_nowLevel < _distancePerLevel.Length - 1)
        {
            _radarTimer += Time.deltaTime;
            if (_radarTimer > _secPerLevel[_nowLevel])
            {
                _nowLevel++;
                _radarTimer = 0;
            }
        }
    }

    private void LateUpdate()
    {
        // レーダー中でない場合は処理しない
        if (!_startedRadar) return;

        // レーダーを使用し続けた時間に応じて照射距離が変動
        float distance = _distancePerLevel[_nowLevel];

        // カメラの前方にあるオブジェクトを取得
        List<GameObject> hits = Physics.SphereCastAll(
                                            _cameraTransform.position,
                                            _radarRadius,
                                            _cameraTransform.forward,
                                            distance)
                                            .Select(h => h.transform.gameObject)
                                            .ToList();

        // レーダー対象取り出し
        List<GameObject> targets = FilterTargets(hits);

        // 対象が存在しない場合はマーカーを全て削除して終了
        if (targets.Count <= 0)
        {
            DestroyAllMarkers();
            return;
        }

        foreach (GameObject target in targets)
        {
            // 既に照射中の場合はスキップ
            if (_radaringMap.ContainsKey(target)) continue;

            // IRadarableインターフェース取得
            IRadarable radarable = target.GetComponent<IRadarable>();

            // マーカー生成
            RectTransform markerTransform = null;
            if (_canvas != null)
            {
                Image marker = null;
                if (radarable.Type == IRadarable.ObjectType.Enemy)
                {
                    marker = Instantiate(_enemyMarker);
                }
                else
                {
                    marker = Instantiate(_itemMarker);
                }

                // マーカーの親Canvas紐づけ
                markerTransform = marker.rectTransform;
                markerTransform.SetParent(_canvas);
            }

            // 照射中マップに追加
            _radaringMap.Add(target, (radarable.Type, markerTransform));
        }

        // 照射中の全てのマーカー座標を更新
        List<GameObject> radaringList = _radaringMap.Keys.ToList();  // foreach中のremove時の例外対策
        foreach (GameObject radaring in radaringList)
        {
            // レーダー対象にない場合は画面外に出て照射から外れたため削除
            if (!targets.Contains(radaring))
            {
                if (_radaringMap[radaring].marker != null)
                {
                    Destroy(_radaringMap[radaring].marker.gameObject);
                }
                _radaringMap.Remove(radaring);
                continue;
            }

            // マーカー座標を更新
            if (_radaringMap[radaring].marker != null)
            {
                _radaringMap[radaring].marker.position = ConvertToScreenPosition(radaring.transform.position);
            }
        }
    }

    /// <summary>
    /// 指定されたオブジェクトのうちレーダー照射対象を取り出す
    /// </summary>
    /// <param name="objects"></param>
    /// <returns>レーダー照射対象</returns>
    private List<GameObject> FilterTargets(List<GameObject> objects)
    {
        // 戻り値
        List<GameObject> targets = new List<GameObject>();

        foreach (GameObject o in objects)
        {
            // IRadarableインターフェースを実装していない場合は除外
            IRadarable radarable = o.GetComponent<IRadarable>();
            if (radarable == null) continue;

            // レーダー照射不可設定がされている場合は除外
            if (!radarable.IsRadarable) continue;

            // 自分のドローンがレーダー照射不可指定されている場合は除外
            if (radarable.NotRadarableList.Contains(gameObject)) continue;

            // 画面内に存在しない場合は除外
            Vector3 screenPoint = _camera.WorldToViewportPoint(o.transform.position);
            if (!(screenPoint.x > 0 && screenPoint.x < 1f && screenPoint.y > 0 && screenPoint.y < 1f && screenPoint.z > 0)) continue;

            // リストに追加
            targets.Add(o);
        }

        return targets;
    }

    /// <summary>
    /// 指定された3Dオブジェクトの座標をカメラから見た画面上の座標へ変換する
    /// </summary>
    /// <param name="position">3Dオブジェクトの座標</param>
    /// <returns>変換後の画面上の座標</returns>
    private Vector3 ConvertToScreenPosition(Vector3 position)
    {
        // 座標をビューポートに変換(画面左下が0:0、右上が1:1)
        Vector3 screenPoint = _camera.WorldToViewportPoint(position);

        // 画面サイズに合わせた座標へ変換して返す
        return new Vector3(Screen.width * screenPoint.x, Screen.height * screenPoint.y, 0);
    }

    /// <summary>
    /// マーカーを全て削除
    /// </summary>
    private void DestroyAllMarkers()
    {
        if (_canvas != null)
        {
            foreach (var target in _radaringMap.Values)
            {
                Destroy(target.marker.gameObject);
            }
        }
        _radaringMap.Clear();
    }
}
