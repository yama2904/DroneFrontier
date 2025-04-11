using System;
using UnityEngine;
using UnityEngine.UI;

public class DroneLockOnComponent : MonoBehaviour, IDroneComponent
{
    /// <summary>
    /// ロックオン中のオブジェクト
    /// </summary>
    public GameObject Target => Useful.IsNullOrDestroyed(_target) ? null : _target;
    private GameObject _target = null;

    /// <summary>
    /// レティクルを非表示にするか
    /// </summary>
    public bool HideReticle
    {
        get { return _hideReticle; }
        set
        {
            if (_reticleImage != null)
            {
                _reticleImage.enabled = !value;
            }
            _hideReticle = value;
        }
    }
    private bool _hideReticle = false;

    /// <summary>
    /// 新規ターゲットロックオンイベント
    /// </summary>
    public event EventHandler OnTargetLockOn;

    /// <summary>
    /// ターゲットロックオン解除イベント
    /// </summary>
    public event EventHandler OnTargetUnLockOn;

    [SerializeField, Tooltip("ドローンのカメラ")]
    private Camera _camera = null;

    [SerializeField, Tooltip("レティクル画像")]
    private Image _reticleImage = null;

    [SerializeField, Tooltip("ロックオン中のレティクルの色")]
    Color _lockOnColor = new Color(255, 0, 0, 200);

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

    public void Initialize() 
    {
        // 非ロックオン状態でレティクル初期化
        if (_reticleImage != null)
        {
            _reticleImage.color = _noLockOnColor;
        }
    }

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
    /// ロックオンの有効・無効を設定<br/>
    /// ロックオン無効が重複している状態で有効にした場合は無効のままとなる
    /// </summary>
    public void SetEnableLockOn(bool enable)
    {
        if (enable)
        {
            _disabledCount--;
            if (_disabledCount <= 0)
            {
                _disabledCount = 0;
                enabled = true;
            }
        }
        else
        {
            if (_disabledCount == 0)
            {
                StopLockOn();
            }

            _disabledCount++;
            enabled = false;
        }
    }

    private void Awake()
    {
        _droneTransform = transform;
        _cameraTransform = _camera.transform;
    }

    private void FixedUpdate()
    {
        // ロックオン中でない場合は処理しない
        if (!_startedLockOn) return;

        // カメラの前方にあるオブジェクトを取得
        RaycastHit[] hits = Physics.SphereCastAll(
                                            _cameraTransform.position,
                                            _lockOnRadius,
                                            _cameraTransform.forward,
                                            _lockOnDistance);

        // ロックオン中のオブジェクトが存在するかチェック
        foreach (RaycastHit hit in hits)
        {
            // 存在する場合はターゲットの方へ追従して終了
            if (Target == hit.transform.gameObject)
            {
                // ターゲットとの距離計算
                Vector3 diff = _targetTransform.position - _cameraTransform.position;

                // 追従方向
                Quaternion rotation = Quaternion.LookRotation(diff);

                // ターゲットの方へ向く
                _droneTransform.rotation = Quaternion.Slerp(_droneTransform.rotation, rotation, _aimSpeed * Time.deltaTime);
                return;
            }
        }

        // ロックオン中のオブジェクトが存在しない場合は新規ロックオン先を探す
        bool exists = FilterTarget(hits, out RaycastHit target);

        // 新規ロックオン先がいない場合はロックオン解除
        if (!exists)
        {
            ResetTarget();
            return;   
        }

        // ターゲットを新規ロックオン先で更新
        _target = target.transform.gameObject;
        _targetTransform = target.transform.transform;

        // ロックオン画像の色変更
        if (_reticleImage != null)
        {
            _reticleImage.color = _lockOnColor;
        }

        // 新規ターゲットロックオンイベント発火
        OnTargetLockOn?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 指定されたオブジェクトのうちロックオン可能、かつカメラの中央に最も近いオブジェクトを返す
    /// </summary>
    /// <param name="hits"></param>
    /// <param name="target"></param>
    /// <returns>ヒット可能オブジェクトが存在しない場合はfalse</returns>
    private bool FilterTarget(RaycastHit[] hits, out RaycastHit target)
    {
        // outパラメータ初期化
        target = new RaycastHit();

        // オブジェクトとの最小距離
        float minDistance = float.MaxValue;

        bool exists = false;
        foreach (RaycastHit hit in hits)
        {
            // ILockableOnインターフェースを実装していない場合は除外
            ILockableOn lockableOn = hit.transform.GetComponent<ILockableOn>();
            if (lockableOn == null) continue;

            // ロックオン不可設定がされている場合は除外
            if (!lockableOn.IsLockableOn) continue;

            // 自分のドローンがロックオン不可指定されている場合は除外
            if (lockableOn.NotLockableOnList.Contains(gameObject)) continue;

            // 画面の一定範囲内に存在しない場合は除外
            Vector3 screenPoint = _camera.WorldToViewportPoint(hit.transform.position);
            if (!(screenPoint.x > 0.25f && screenPoint.x < 0.75f && screenPoint.y > 0.15f && screenPoint.y < 0.85f && screenPoint.z > 0)) continue;

            // 画面の中央との距離を計算
            float hitDistance = (new Vector2(0.5f, 0.5f) - new Vector2(screenPoint.x, screenPoint.y)).sqrMagnitude;

            // カメラの中央に最も近い場合ターゲット更新
            if (hitDistance < minDistance)
            {
                minDistance = hitDistance;
                target = hit;
            }

            exists = true;
        }

        return exists;
    }

    /// <summary>
    /// ターゲット解除
    /// </summary>
    private void ResetTarget()
    {
        // ターゲットロックオン解除イベント発火
        if (Target != null)
        {
            OnTargetUnLockOn?.Invoke(this, EventArgs.Empty);
        }

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