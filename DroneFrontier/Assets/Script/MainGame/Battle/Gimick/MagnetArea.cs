using Common;
using Cysharp.Threading.Tasks;
using Drone.Battle;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class MagnetArea : MonoBehaviour
{
    /// <summary>
    /// アクティブ可能な最大エリア数
    /// </summary>
    private const int MAX_MAGNET_AREA_NUM = 3;

    /// <summary>
    /// 現在のアクティブエリア数
    /// </summary>
    private static int _currentAreaNum = 0;

    #region プロパティ

    /// <summary>
    /// 速度低下率
    /// </summary>
    public float DownPercent
    {
        get => _downPercent;
        set => _downPercent = value;
    }

    /// <summary>
    /// スポーン確率
    /// </summary>
    public int SpawnPercent
    {
        get => _spawnPercent;
        set => _spawnPercent = value;
    }

    /// <summary>
    /// 発生間隔（秒）
    /// </summary>
    public float SpawnInterval
    {
        get => _spawnInterval;
        set => _spawnInterval = value;
    }

    /// <summary>
    /// 発生時間（秒）
    /// </summary>
    public float ActiveTime
    {
        get => _activeTime;
        set => _activeTime = value;
    }

    /// <summary>
    /// 最小エリアサイズ
    /// </summary>
    public float MinAreaSize
    {
        get => _minAreaSize;
        set => _minAreaSize = value;
    }

    /// <summary>
    /// 最大エリアサイズ
    /// </summary>
    public float MaxAreaSize
    {
        get => _maxAreaSize;
        set => _maxAreaSize = value;
    }

    /// <summary>
    /// 現在エリアサイズ
    /// </summary>
    public float CurrentAreaSize { get; private set; } = 0;

    #endregion

    /// <summary>
    /// 磁気エリア発生イベント
    /// </summary>
    public event EventHandler OnSpawn;

    /// <summary>
    /// 磁気エリア消滅イベント
    /// </summary>
    public event EventHandler OnDespawn;

    [SerializeField, Range(0, 1f), Tooltip("速度低下率")]
    private float _downPercent = 0.7f;

    [SerializeField, Range(0, 100), Tooltip("スポーン確率")]
    private int _spawnPercent = 50;

    [SerializeField, Tooltip("発生間隔（秒）")]
    private float _spawnInterval = 30f;

    [SerializeField, Tooltip("発生時間（秒）")]
    private float _activeTime = 20f;

    [SerializeField, Tooltip("最小エリアサイズ")]
    private float _minAreaSize = 1f;

    [SerializeField, Tooltip("最大エリアサイズ")]
    private float _maxAreaSize = 3f;

    [SerializeField]
    private ParticleSystem _particle1 = null;

    [SerializeField]
    private ParticleSystem _particle2 = null;

    /// <summary>
    /// 各オブジェクトに付与したスピードダウンステータス
    /// </summary>
    private Dictionary<GameObject, SpeedDownStatus> _speedDowns = new Dictionary<GameObject, SpeedDownStatus>();

    private CancellationTokenSource _cancel = new CancellationTokenSource();

    // コンポーネントキャッシュ
    private Transform _transform = null;

    private void Start()
    {
        _transform = transform;
        SetEnabledArea(false);

        UniTask.Void(async () =>
        {
            while (true)
            {
                // 発生タイマー
                await UniTask.Delay(TimeSpan.FromSeconds(_spawnInterval), cancellationToken: _cancel.Token);

                // 発生させるかランダムに決定
                if (UnityEngine.Random.Range(0, 100) >= _spawnPercent) continue;

                // 既に最大数発生していたらスキップ
                if (_currentAreaNum >= MAX_MAGNET_AREA_NUM) continue;

                // 発生開始
                _currentAreaNum++;
                CurrentAreaSize = UnityEngine.Random.Range(_minAreaSize, _maxAreaSize);
                ChangeAreaSize(CurrentAreaSize);
                SetEnabledArea(true);

                // 発生イベント発火
                OnSpawn?.Invoke(this, EventArgs.Empty);

                // 停止タイマー
                await UniTask.Delay(TimeSpan.FromSeconds(_activeTime), cancellationToken: _cancel.Token);

                // 停止
                SetEnabledArea(false);
                ClearStatus();
                _currentAreaNum--;

                // 消滅イベント発火
                OnDespawn?.Invoke(this, EventArgs.Empty);
            }
        });
    }

    private void OnDestroy()
    {
        ClearStatus();
        _cancel.Cancel();
    }

    private void OnTriggerEnter(Collider other)
    {
        // 既にスピードダウン付与済みの場合は処理しない
        if (_speedDowns.ContainsKey(other.gameObject)) return;

        // プレイヤーかCPUのみ処理
        if (!other.CompareTag(TagNameConst.PLAYER)
            && !other.CompareTag(TagNameConst.CPU))
        {
            return;
        }

        // スピードダウンステータス付与
        SpeedDownStatus status = new SpeedDownStatus();
        other.GetComponent<DroneStatusComponent>().AddStatus(status, 9999, _downPercent);
        _speedDowns.Add(other.gameObject, status);

        Debug.Log($"スピードダウン：{other.gameObject.name}");
    }

    private void OnTriggerExit(Collider other)
    {
        // ジャミング解除
        if (_speedDowns.ContainsKey(other.gameObject))
        {
            _speedDowns[other.gameObject].EndSpeedDown();
            _speedDowns.Remove(other.gameObject);
        }
    }

    /// <summary>
    /// 磁気エリアの発生/停止を設定
    /// </summary>
    /// <param name="enable">発生させる場合はtrue</param>
    private void SetEnabledArea(bool enable)
    {
        if (enable)
        {
            _particle1.Play();
            _particle2.Play();
            gameObject.SetActive(true);
        }
        else
        {
            _particle1.Stop();
            _particle2.Stop();
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// エリアサイズ変更
    /// </summary>
    /// <param name="size"></param>
    private void ChangeAreaSize(float size)
    {
        _transform.localScale = new Vector3(size, size, size);
        _particle1.transform.localScale = new Vector3(size * 5, size * 5, size * 5);
        _particle2.transform.localScale = new Vector3(size * 5, size * 5, size * 5);
    }

    /// <summary>
    /// 全てのスピードダウンを解除
    /// </summary>
    private void ClearStatus()
    {
        foreach (SpeedDownStatus status in _speedDowns.Values)
        {
            status.EndSpeedDown();
        }
        _speedDowns.Clear();
    }
}
