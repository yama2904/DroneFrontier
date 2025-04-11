using Offline.Player;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Offline
{
    public class JammingBot : MonoBehaviour, ILockableOn, IRadarable
    {
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

        /// <summary>
        /// ジャミングボット生成直後の移動量
        /// </summary>
        private const int BOT_MOVE_VALUE = 60;

        /// <summary>
        /// ジャミングボット生成直後の移動時間計測
        /// </summary>
        private float _initMoveTimer = 0;

        /// <summary>
        /// ジャミングボット破壊タイマー
        /// </summary>
        private float _destroyTimer = 0;

        /// <summary>
        /// 各オブジェクトに付与したジャミングステータス
        /// </summary>
        private Dictionary<GameObject, JammingStatus> _addedJammingStatusMap = new Dictionary<GameObject, JammingStatus>();

        /// <summary>
        /// ジャミングボットのRigidBody
        /// </summary>
        private Rigidbody _rigidBody = null;

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
            if (_initMoveTimer > InitMoveSec)
            {
                _rigidBody.isKinematic = true;
            }
        }

        private void OnDestroy()
        {
            foreach (JammingStatus status in _addedJammingStatusMap.Values)
            {
                status.EndJamming();
            }

            // 破壊イベント発火
            DestroyEvent?.Invoke(this, EventArgs.Empty);

            //デバッグ用
            Debug.Log("ジャミングボット破壊");
        }

        private void OnTriggerEnter(Collider other)
        {
            // ジャミングボットを生成したオブジェクト自身なら処理しない
            if (other.gameObject == _creater) return;

            // 既にジャミング付与済みの場合は処理しない
            if (_addedJammingStatusMap.ContainsKey(other.gameObject)) return;

            // プレイヤーかCPUのみ処理
            string tag = other.tag;
            if (tag != TagNameConst.PLAYER && tag != TagNameConst.CPU) return;

            // ジャミングステータス付与
            JammingStatus status = new JammingStatus();
            other.GetComponent<DroneStatusComponent>().AddStatus(status, 9999);
            _addedJammingStatusMap.Add(other.gameObject, status);
        }

        private void OnTriggerExit(Collider other)
        {
            // ジャミング解除
            if (_addedJammingStatusMap.ContainsKey(other.gameObject))
            {
                _addedJammingStatusMap[other.gameObject].EndJamming();
                _addedJammingStatusMap.Remove(other.gameObject);
            }
        }
    }
}