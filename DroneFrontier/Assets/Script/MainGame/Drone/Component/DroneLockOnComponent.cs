using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class DroneLockOnComponent : MonoBehaviour
{
    /// <summary>
    /// ロックオン中のオブジェクト
    /// </summary>
    public GameObject Target => Useful.IsNullOrDestroyed(_target) ? null : _target;
    private GameObject _target = null;

    [SerializeField, Tooltip("ドローンのカメラ")]
    private Camera _camera = null;

    [SerializeField, Tooltip("レティクル画像")]
    private Image _reticleImage = null;

    [SerializeField, Tooltip("ロックオン中のレティクルの色")]
    Color _lockingOnColor = new Color(255, 0, 0, 200);

    [SerializeField, Tooltip("非ロックオン中のレティクルの色")]
    Color _noLockOnColor = new Color(255, 255, 255, 128);

    [SerializeField, Tooltip("ロックオンした際に敵に向く速度")] 
    private float _aimSpeed = 12f;

    [SerializeField, Tooltip("ロックオン範囲")] 
    private float _lockOnRadius = 450f;

    [SerializeField, Tooltip("ロックオン距離")] 
    private float _lockOnDistance = 450f;
    
    /// <summary>
    /// ロックオン中であるか
    /// </summary>
    private bool _startedLockOn = false;

    /// <summary>
    /// 一時的なロックオン無効の重複カウント
    /// </summary>
    private int _disabledCount = 0;

    Transform _droneTransform = null;
    Transform _cameraTransform = null;
    Transform _targetTransform = null;

    /// <summary>
    /// ロックオン開始
    /// </summary>
    public void StartLockOn()
    {
        if (!enabled) return;
        _startedLockOn = true;
    }

    /// <summary>
    /// ロックオン停止
    /// </summary>
    public void StopLockOn()
    {
        if (!enabled) return;
        _startedLockOn = false;
        ResetTarget();
    }

    /// <summary>
    /// 一時的にロックオン無効を設定する
    /// </summary>
    public void QueueDisabled()
    {
        if (_disabledCount == 0)
        {
            StopLockOn();
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
        _droneTransform = transform;
        _cameraTransform = _camera.transform;
    }

    private void Start()
    {
        // 非ロックオン状態でレティクル初期化
        if (_reticleImage != null)
        {
            _reticleImage.color = _noLockOnColor;
        }
    }

    private void LateUpdate()
    {
        // ロックオン中でない場合は処理しない
        if (!_startedLockOn) return;

        // カメラの前方にあるオブジェクトを取得
        List<GameObject> hits = Physics.SphereCastAll(
                                            _cameraTransform.position,
                                            _lockOnRadius,
                                            _cameraTransform.forward,
                                            _lockOnDistance)
                                            .Select(h => h.transform.gameObject)
                                            .ToList();

        // ロックオン対象取り出し
        List<GameObject> targets = FilterTargets(hits);

        // ロックオン対象がいない場合はロックオン解除
        if (targets.Count <= 0)
        {
            ResetTarget();
            return;   
        }

        // ロックオン中のターゲットが対象内に存在する場合は追従処理
        if (Target != null && targets.Contains(Target))
        {
            // ターゲットとの距離計算
            Vector3 diff = _targetTransform.position - _cameraTransform.position;
            
            // 追従方向
            Quaternion rotation = Quaternion.LookRotation(diff);

            // ターゲットの方へ向く
            _droneTransform.rotation = Quaternion.Slerp(_droneTransform.rotation, rotation, _aimSpeed * Time.deltaTime);
            return;
        }

        // 追従処理を行わない場合は新規ロックオン先を探す

        // 新規ロックオン先
        GameObject newTarget = null;

        // オブジェクトとの最小距離
        float minTargetDistance = float.MaxValue;

        // 画面の中央から最も近いオブジェクトをロックオンする
        foreach (var target in targets)
        {
            // ビューポートに変換
            Vector3 targetScreenPoint = _camera.WorldToViewportPoint(target.transform.position);

            // 画面の中央との距離を計算
            float targetDistance = (new Vector2(0.5f, 0.5f) - new Vector2(targetScreenPoint.x, targetScreenPoint.y)).sqrMagnitude;

            // 距離が最小だったら更新
            if (targetDistance < minTargetDistance)
            {
                minTargetDistance = targetDistance;
                newTarget = target;
            }
        }

        // ロックオン画像の色変更
        if (_reticleImage != null)
        {
            _reticleImage.color = _lockingOnColor;
        }

        // ターゲット用変数更新
        _target = newTarget;
        _targetTransform = newTarget.transform;
    }

    /// <summary>
    /// 指定されたオブジェクトのうちロックオン対象を取り出す
    /// </summary>
    /// <param name="objects"></param>
    /// <returns>ロックオン対象</returns>
    private List<GameObject> FilterTargets(List<GameObject> objects)
    {
        // 戻り値
        List<GameObject> targets = new List<GameObject>();

        foreach (GameObject o in objects)
        {
            // ILockableOnインターフェースを実装していない場合は除外
            ILockableOn lockableOn = o.GetComponent<ILockableOn>();
            if (lockableOn == null) continue;

            // ロックオン不可設定がされている場合は除外
            if (!lockableOn.IsLockableOn) continue;

            // 自分のドローンがロックオン不可指定されている場合は除外
            if (lockableOn.NotLockableOnList.Contains(gameObject)) continue;

            // 画面の一定範囲内に存在しない場合は除外
            Vector3 screenPoint = _camera.WorldToViewportPoint(o.transform.position);
            if (!(screenPoint.x > 0.25f && screenPoint.x < 0.75f && screenPoint.y > 0.15f && screenPoint.y < 0.85f && screenPoint.z > 0)) continue;

            // リストに追加
            targets.Add(o);
        }

        return targets;
    }

    /// <summary>
    /// ターゲット解除
    /// </summary>
    private void ResetTarget()
    {
        // ロックオン画像の色変更
        if (_reticleImage != null)
        {
            _reticleImage.color = _noLockOnColor;
        }

        // ターゲット用変数の更新
        _target = null;
        _targetTransform = null;
    }
}