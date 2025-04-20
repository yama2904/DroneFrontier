using Common;
using Drone.Battle;
using System;
using System.Collections.Generic;
using UnityEngine;

public class JammingBot : MonoBehaviour, ILockableOn, IRadarable, IDamageable
{
    public GameObject Owner => Creater;

    /// <summary>
    /// ジャミングボット生成直後の移動量
    /// </summary>
    private const int BOT_MOVE_VALUE = 60;

    /// <summary>
    /// ジャミングボットの残りHP
    /// </summary>
    public float HP
    {
        get { return _hp; }
    }

    /// <summary>
    /// ロックオン可能であるか
    /// </summary>
    public bool IsLockableOn { get; } = true;

    /// <summary>
    /// ロックオン不可にするオブジェクト
    /// </summary>
    public List<GameObject> NotLockableOnList { get; } = new List<GameObject>();

    public IRadarable.ObjectType Type => IRadarable.ObjectType.Enemy;

    public bool IsRadarable => true;

    public List<GameObject> NotRadarableList { get; } = new List<GameObject>();

    /// <summary>
    /// ジャミングボット生成オブジェクト
    /// </summary>
    public GameObject Creater
    {
        get { return _creater; }
        set
        {
            NotLockableOnList.Clear();
            NotLockableOnList.Add(value);
            NotRadarableList.Clear();
            NotRadarableList.Add(value);

            // 更新前のオブジェクトとの当たり判定を復活
            if (!Useful.IsNullOrDestroyed(_creater) && _creater.TryGetComponent(out Collider oldCollider))
            {
                Physics.IgnoreCollision(oldCollider, GetComponent<Collider>(), false);
            }

            // 生成者とは当たり判定を行わない
            if (!Useful.IsNullOrDestroyed(value) && value.TryGetComponent(out Collider collider))
            {
                Physics.IgnoreCollision(collider, GetComponent<Collider>());
            }

            _creater = value;
        }
    }
    private GameObject _creater = null;

    /// <summary>
    /// ジャミングボットの生存時間（秒）
    /// </summary>
    public float DestroySec { get; set; } = 60.0f;

    /// <summary>
    /// ボット生成時の移動時間（秒）
    /// </summary>
    public float InitMoveSec { get; set; } = 1;

    /// <summary>
    /// ジャミングボット破壊イベント
    /// </summary>
    public event EventHandler DestroyEvent;

    [SerializeField]
    private float _hp = 30.0f;

    [SerializeField]
    private JammingArea _jammingArea = null;

    private JammingArea _createdArea = null;

    /// <summary>
    /// ジャミングボット生成直後の移動時間計測
    /// </summary>
    private float _initMoveTimer = 0;

    /// <summary>
    /// ジャミングボット破壊タイマー
    /// </summary>
    private float _destroyTimer = 0;

    /// <summary>
    /// ジャミングボットのRigidBody
    /// </summary>
    private Rigidbody _rigidBody = null;

    public bool Damage(GameObject source, float value)
    {
        // 自オブジェクト生成者からはダメージを受けない
        if (source == Creater) return false;

        // 小数点第2以下切り捨て
        value = Useful.Floor(value, 1);
        _hp -= value;
        if (_hp < 0)
        {
            // オブジェクト削除
            Destroy(gameObject);
        }

        return true;
    }

    private void Awake()
    {
        _rigidBody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (_destroyTimer > DestroySec) return;

        _destroyTimer += Time.deltaTime;
        if (_destroyTimer > DestroySec)
        {
            Destroy(gameObject);
        }
    }

    private void FixedUpdate()
    {
        // ジャミングボットの移動が終わった場合は処理しない
        if (_initMoveTimer > InitMoveSec) return;

        _rigidBody.AddForce(Vector3.up * BOT_MOVE_VALUE, ForceMode.Acceleration);

        // 移動時間計測
        _initMoveTimer += Time.deltaTime;

        // 移動終了
        if (_initMoveTimer > InitMoveSec)
        {
            _rigidBody.isKinematic = true;
            _createdArea = Instantiate(_jammingArea, transform.position, Quaternion.identity);
            _createdArea.Creater = Creater;
        }
    }

    private void OnDestroy()
    {
        // ジャミングエリア削除
        if (_createdArea != null)
        {
            Destroy(_createdArea.gameObject);
        }

        // 破壊イベント発火
        DestroyEvent?.Invoke(this, EventArgs.Empty);

        //デバッグ用
        Debug.Log("ジャミングボット破壊");
    }
}