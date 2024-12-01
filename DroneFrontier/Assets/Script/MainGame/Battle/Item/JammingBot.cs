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
        /// ジャミングボット破壊イベント
        /// </summary>
        public event EventHandler DestroyEvent;

        /// <summary>
        /// 各オブジェクトに付与したジャミングステータス
        /// </summary>
        private Dictionary<GameObject, JammingStatus> _addedJammingStatusMap = new Dictionary<GameObject, JammingStatus>();

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