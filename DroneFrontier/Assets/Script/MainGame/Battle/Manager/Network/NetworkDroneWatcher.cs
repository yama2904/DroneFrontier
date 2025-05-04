using Battle.Packet;
using Common;
using Cysharp.Threading.Tasks;
using Drone.Battle;
using Drone.Battle.Network;
using Network;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        private static List<(string name, NetworkBattleDrone drone)> _watchDrones = new List<(string name, NetworkBattleDrone drone)>();

        /// <summary>
        /// 現在カメラ参照中のドローンのインデックス
        /// </summary>
        private static int _watchingDrone = 0;

        private static CancellationTokenSource _cancel = new CancellationTokenSource();
        private static bool _isRunning = false;

        public static void Run()
        {
            if (_isRunning) return;
            _isRunning = true;
            _cancel = new CancellationTokenSource();

            // 試合中のプレイヤー取得
            _watchDrones = GameObject.FindGameObjectsWithTag(TagNameConst.PLAYER)
                                     .Where(x => !Useful.IsNullOrDestroyed(x))
                                     .Select(x =>
                                     {
                                         var drone = x.GetComponent<NetworkBattleDrone>();
                                         return (drone.Name, drone);
                                     })
                                     .ToList();

            // 全てのドローンのカメラ参照初期化
            foreach (var drone in _watchDrones)
            {
                drone.drone.IsWatch = false;
            }

            // 参照先カメラ設定
            _watchingDrone = 0;
            _watchDrones[_watchingDrone].drone.IsWatch = true;
        }

        private void Awake()
        {
            // イベント設定
            NetworkManager.OnTcpReceived += OnTcpReceived;
            NetworkManager.OnUdpReceivedOnMainThread += OnUdpReceived;
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
            _cancel.Cancel();

            // イベント削除
            NetworkManager.OnTcpReceived -= OnTcpReceived;
            NetworkManager.OnUdpReceivedOnMainThread -= OnUdpReceived;
            _droneSpawnManager.OnDroneDestroy -= OnDroneDestroy;
        }

        private void OnTcpReceived(string name, BasePacket packet)
        {
            if (packet is DroneWatchPacket)
            {
                Run();
            }
        }

        /// <summary>
        /// ドローン破壊イベント
        /// </summary>
        /// <param name="destroyDrone">破壊されたドローン</param>
        /// <param name="respawnDrone">リスポーンしたドローン</param>
        private void OnDroneDestroy(IBattleDrone destroyDrone, IBattleDrone respawnDrone)
        {
            UpdateWatchDrones(destroyDrone.Name, respawnDrone);
        }

        /// <summary>
        /// UDP受信イベント
        /// </summary>
        /// <param name="player">送信元プレイヤー</param>
        /// <param name="packet">受信したUDPパケット</param>
        protected virtual async void OnUdpReceived(string player, BasePacket packet)
        {
            if (packet is DroneDestroyPacket destroy)
            {
                string newId = destroy.RespawnDroneId;

                IBattleDrone respawnDrone = null;
                if (!string.IsNullOrEmpty(newId))
                {
                    while (true)
                    {
                        if (NetworkObjectSpawner.SpawnedObjects.ContainsKey(newId))
                        {
                            respawnDrone = NetworkObjectSpawner.SpawnedObjects[newId] as NetworkBattleDrone;
                            break;
                        }
                        await UniTask.Delay(1, cancellationToken: _cancel.Token);
                    }
                }

                UpdateWatchDrones(destroy.Name, respawnDrone);
            }
        }

        /// <summary>
        /// 観戦中ドローンリスト更新
        /// </summary>
        /// <param name="player">更新するドローンのプレイヤー名</param>
        /// <param name="respawnDrone">リスポーンドローン</param>
        private void UpdateWatchDrones(string player, IBattleDrone respawnDrone)
        {
            if (!_isRunning) return;

            // 破壊されたドローンをリストから削除
            int droneIndex = _watchDrones.FindIndex(x => x.name == player);
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
                    _watchDrones.Insert(droneIndex, (drone.Name, drone));
                }
                else
                {
                    _watchDrones.Add((drone.Name, drone));
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
            if (_watchingDrone < _watchDrones.Count
                && _watchDrones[_watchingDrone].drone != null)
            {
                _watchDrones[_watchingDrone].drone.IsWatch = false;
            }

            // 次のプレイヤー
            _watchingDrone++;
            if (_watchingDrone >= _watchDrones.Count)
            {
                _watchingDrone = 0;
            }

            // カメラ参照設定（対象が破壊されている場合は不整合が起きているため削除して次のカメラへ切り替える）
            if (_watchDrones[_watchingDrone].drone == null)
            {
                _watchDrones.RemoveAt(_watchingDrone);
                WatchNextDrone();
            }
            else
            {
                _watchDrones[_watchingDrone].drone.IsWatch = true;
            }
        }
    }
}