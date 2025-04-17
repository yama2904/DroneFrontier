using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class BarrierWeakLaser : MonoBehaviour
{
    /// <summary>
    /// レーザーが発生する度に短くする発生間隔（秒）
    /// </summary>
    private const float INTERVAL_SHORT_SEC = 3;

    /// <summary>
    /// レーザーの最大半径
    /// </summary>
    private const float MAX_LASER_WIDTH = 100f;

    [SerializeField, Tooltip("バリア弱体化時間（秒）")]
    private float _weakTime = 15f;

    [SerializeField, Tooltip("レーザー射程")]
    private float _lazerRange = 3000f;

    [SerializeField, Tooltip("レーザーの当たり判定の半径")]
    private float _lazerRadius = 10f;

    [SerializeField, Tooltip("レーザーのランダム発生間隔の最小値（秒）")]
    private float _minInterval = 30f;

    [SerializeField, Tooltip("レーザーのランダム発生間隔の最大値（秒）")]
    private float _maxInterval = 60f;

    [SerializeField, Tooltip("レーザーの発生時間（秒）")]
    private float _laserTime = 20f;

    [SerializeField, Tooltip("レーザーのY軸ランダム回転速度(/s)の最小値")]
    private float _minRotateSpeed = 70f;

    [SerializeField, Tooltip("レーザーのY軸ランダム回転速度(/s)の最大値")]
    private float _maxRotateSpeed = 120f;

    [SerializeField, Tooltip("レーザー発生時のX軸ランダム角度の最小値")]
    private float _minAngle = 20f;

    [SerializeField, Tooltip("レーザー発生時のX軸ランダム角度の最大値")]
    private float _maxAngle = 50f;

    /// <summary>
    /// レーザーのY軸回転速度
    /// </summary>
    private float _rotateSpeed = 0;

    /// <summary>
    /// 現在のレーザー半径
    /// </summary>
    private float _lazerWidth = 0;

    private CancellationTokenSource _cancel = new CancellationTokenSource();

    private bool _enabledLaser = false;

    // コンポーネントキャッシュ
    private Transform _transform = null;
    private LineRenderer _renderer = null;

    private void Start()
    {
        // コンポーネントキャッシュ
        _transform = transform;
        _renderer = GetComponent<LineRenderer>();

        // レーザーの始点初期化
        _transform.position = _renderer.GetPosition(0);

        // レーザー非表示
        _renderer.startWidth = 0;
        _renderer.endWidth = 0;

        // レーザー発生/停止タイマー
        UniTask.Void(async () =>
        {
            while (true)
            {
                // レーザー発生タイマー
                TimeSpan interval = TimeSpan.FromSeconds(UnityEngine.Random.Range(_minInterval, _maxInterval));
                await UniTask.Delay(interval, cancellationToken: _cancel.Token);

                // 既にレーザー発生中の場合はスキップ
                if (_enabledLaser) continue;
                Debug.Log("バリア弱体化レーザー発生");

                // 発生するたびに間隔を短くする
                _minInterval -= INTERVAL_SHORT_SEC;
                _minInterval = _minInterval < 0 ? 0 : _minInterval;
                _maxInterval -= INTERVAL_SHORT_SEC;
                _maxInterval = _maxInterval < 0 ? 0 : _maxInterval;

                // レーザーの角度をランダムに設定
                Vector3 angle = _transform.localEulerAngles;
                angle.x = UnityEngine.Random.Range(_minAngle, _maxAngle);
                _transform.localEulerAngles = angle;

                // 回転速度をランダムに設定
                _rotateSpeed = UnityEngine.Random.Range(_minRotateSpeed, _maxRotateSpeed);

                // レーザー発生
                _enabledLaser = true;

                // レーザー停止タイマー
                UniTask.Void(async () =>
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(_laserTime), cancellationToken: _cancel.Token);
                    _enabledLaser = false;
                    Debug.Log("バリア弱体化レーザー停止");
                });
            }
        });
    }

    private void Update()
    {
        // レーザーサイズ更新
        UpdateLazerWidth(_enabledLaser);
    }

    private void FixedUpdate()
    {
        if (!_enabledLaser) return;

        // レーザーの発生中は常に回転
        Vector3 angle = _transform.localEulerAngles;
        angle.y += _rotateSpeed * Time.deltaTime;
        _transform.localEulerAngles = angle;

        // レーザーのヒットオブジェクトをすべて取得
        RaycastHit[] hits = Physics.SphereCastAll(
                                        _transform.position,
                                        _lazerRadius,
                                        _transform.forward,
                                        _lazerRange);
        // ヒットオブジェクトを絞り込む
        bool exists = FilterTarget(hits, out RaycastHit target);

        // ヒット無しの場合はオブジェクトの表示を最大射程にして終了
        if (!exists)
        {
            ApplyLaserLength(_lazerRange);
            return;
        }

        // ドローンにヒットした場合はバリア弱体化付与
        if (target.transform.CompareTag(TagNameConst.PLAYER))
        {
            // バリア弱体化
            target.transform.GetComponent<DroneStatusComponent>().AddStatus(new BarrierWeakStatus(), _weakTime);
        }

        // ヒットしたオブジェクトでレーザーを止める
        ApplyLaserLength(target.distance);
    }

    private void OnDestroy()
    {
        _cancel.Cancel();
    }

    /// <summary>
    /// 指定されたオブジェクトのうち最も距離が近いヒット可能オブジェクトを返す
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
            // 当たり判定を行わないオブジェクトはスキップ
            Transform t = hit.transform;
            if (t.CompareTag(TagNameConst.ITEM)) continue;
            if (t.CompareTag(TagNameConst.BULLET)) continue;
            if (t.CompareTag(TagNameConst.GIMMICK)) continue;
            if (t.CompareTag(TagNameConst.JAMMING_AREA)) continue;
            if (t.CompareTag(TagNameConst.TOWER)) continue;
            if (t.CompareTag(TagNameConst.NOT_COLLISION)) continue;

            // 距離が最小だったら更新
            if (hit.distance < minDistance)
            {
                minDistance = hit.distance;
                target = hit;
            }
            exists = true;
        }

        return exists;
    }

    /// <summary>
    /// レーザーオブジェクトの長さを反映させる
    /// </summary>
    /// <param name="length">レーザーの長さ</param>
    private void ApplyLaserLength(float length)
    {
        // transformに適用
        Vector3 scale = _transform.localScale;
        _transform.localScale = new Vector3(scale.x, scale.y, length);

        // レーザーの見た目に適用
        _renderer.SetPosition(1, _transform.position + (_transform.forward * length));
    }

    /// <summary>
    /// レーザーの半径を更新
    /// </summary>
    /// <param name="enableLazer">レーザー発生中であるか</param>
    private void UpdateLazerWidth(bool enableLazer)
    {
        // レーザーが発生中の場合は徐々に太くする
        if (enableLazer)
        {
            if (_lazerWidth >= MAX_LASER_WIDTH) return;

            _lazerWidth += MAX_LASER_WIDTH * Time.deltaTime;
            if (_lazerWidth > MAX_LASER_WIDTH)
            {
                _lazerWidth = MAX_LASER_WIDTH;
            }
            _renderer.startWidth = _lazerWidth;
            _renderer.endWidth = _lazerWidth;
        }
        else
        {
            // レーザーが発生中以外の場合は徐々に細くする

            if (_lazerWidth <= 0) return;

            _lazerWidth -= MAX_LASER_WIDTH * Time.deltaTime;
            if (_lazerWidth < 0)
            {
                _lazerWidth = 0;
            }
            _renderer.startWidth = _lazerWidth;
            _renderer.endWidth = _lazerWidth;
        }
    }
}
