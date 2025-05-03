using Common;
using Drone.Battle;
using Drone.Battle.Network;
using Network;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Battle.Network
{
    public class NetworkDroneWatcher : MonoBehaviour
    {
        [SerializeField, Tooltip("ドローンスポーン管理オブジェクト")]
        private NetworkDroneSpawnManager _droneSpawnManager = null;

        /// <summary>
        /// 観戦中のドローン
        /// </summary>
        private static List<NetworkBattleDrone> _watchDrones = new List<NetworkBattleDrone>();

        /// <summary>
        /// 現在カメラ参照中のドローンのインデックス
        /// </summary>
        private static int _watchingDrone = 0;

        private static bool _isRunning = false;

        public static void Run()
        {
            if (_isRunning) return;
            _isRunning = true;

            // 試合中のプレイヤー取得
            _watchDrones = GameObject.FindGameObjectsWithTag(TagNameConst.PLAYER)
                                     .Where(x => !Useful.IsNullOrDestroyed(x))
                                     .Select(x => x.GetComponent<NetworkBattleDrone>()).ToList();

            // 全てのドローンのカメラ参照初期化
            foreach (NetworkBattleDrone drone in _watchDrones)
            {
                drone.IsWatch = false;
            }

            // 参照先カメラ設定
            _watchingDrone = 0;
            _watchDrones[_watchingDrone].IsWatch = true;
        }

        private void Awake()
        {
            // イベント設定
            NetworkManager.OnTcpReceived += OnTcpReceived;
            _droneSpawnManager.OnDroneDestroy += OnDroneDestroy;
        }

        private void Update()
        {
            if (_watchDrones.Count <= 0) return;

            // スペースキーで次のプレイヤーへカメラ切り替え
            if (Input.GetKeyDown(KeyCode.Space))
            {
                WatchNextDrone();
            }
        }

        private void OnDestroy()
        {
            _isRunning = false;

            // イベント削除
            NetworkManager.OnTcpReceived -= OnTcpReceived;
            _droneSpawnManager.OnDroneDestroy -= OnDroneDestroy;
        }

        private void OnTcpReceived(string name, BasePacket packet)
        {
            Run();
        }

        /// <summary>
        /// ドローン破壊イベント
        /// </summary>
        /// <param name="destroyDrone">破壊されたドローン</param>
        /// <param name="respawnDrone">リスポーンしたドローン</param>
        private void OnDroneDestroy(IBattleDrone destroyDrone, IBattleDrone respawnDrone)
        {
            if (!_isRunning) return;

            // 破壊されたドローンをリストから削除
            int droneIndex = _watchDrones.IndexOf(destroyDrone as NetworkBattleDrone);
            if (droneIndex >= 0)
            {
                _watchDrones.RemoveAt(droneIndex);
            }

            // リスポーンドローン取得
            var drone = respawnDrone as NetworkBattleDrone;

            // リスポーンされた場合は再度観戦対象に追加
            if (respawnDrone != null)
            {
                if (droneIndex >= 0)
                {
                    _watchDrones.Insert(droneIndex, drone);
                }
                else
                {
                    _watchDrones.Add(drone);
                }
            }

            // 破壊されたドローンが現在観戦中のプレイヤーの場合
            if (droneIndex == _watchingDrone)
            {
                // 残機0の場合は次のプレイヤーへ切り替え
                if (respawnDrone == null)
                {
                    WatchNextDrone();
                }
                else
                {
                    // 残機が残っていてリスポーンした場合はリスポーンドローンへ切り替え
                    drone.IsWatch = true;
                }
            }
        }

        /// <summary>
        /// 次のドローンへカメラを切り替える
        /// </summary>
        private void WatchNextDrone()
        {
            if (_watchingDrone < _watchDrones.Count)
            {
                _watchDrones[_watchingDrone].IsWatch = false;
            }

            // 次のプレイヤー
            _watchingDrone++;
            if (_watchingDrone >= _watchDrones.Count)
            {
                _watchingDrone = 0;
            }

            // カメラ参照設定（対象が破壊されている場合は不整合が起きているため削除して次のカメラへ切り替える）
            if (_watchDrones[_watchingDrone] == null)
            {
                _watchDrones.RemoveAt(_watchingDrone);
                WatchNextDrone();
            }
            else
            {
                _watchDrones[_watchingDrone].IsWatch = true;
            }
        }
    }
}