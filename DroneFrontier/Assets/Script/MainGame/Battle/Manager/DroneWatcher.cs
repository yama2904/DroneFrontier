using Battle.Drone;
using Drone.Battle;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Battle
{
    public class DroneWatcher : MonoBehaviour
    {
        [SerializeField, Tooltip("ドローンスポーン管理オブジェクト")]
        private DroneSpawnManager _droneSpawnManager = null;

        /// <summary>
        /// 観戦中のドローン
        /// </summary>
        private static List<CpuBattleDrone> _watchDrones = new List<CpuBattleDrone>();

        /// <summary>
        /// 現在カメラ参照中のドローンのインデックス
        /// </summary>
        private static int _watchingDrone = 0;

        private static bool _isRunning = false;

        public static void Run()
        {
            if (_isRunning) return;
            _isRunning = true;

            // 試合中のCPU取得
            _watchDrones = FindObjectsByType<CpuBattleDrone>(FindObjectsSortMode.None).ToList();

            // 全てのドローンのカメラ参照初期化
            foreach (CpuBattleDrone drone in _watchDrones)
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
            _droneSpawnManager.OnDroneDestroy += OnDroneDestroy;
        }

        private void Update()
        {
            if (_watchDrones.Count <= 0) return;

            // スペースキーで次のCPUへカメラ切り替え
            if (Input.GetKeyDown(KeyCode.Space))
            {
                WatchNextDrone();
            }
        }

        private void OnDestroy()
        {
            _isRunning = false;

            // イベント削除
            _droneSpawnManager.OnDroneDestroy -= OnDroneDestroy;
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
            int droneIndex = _watchDrones.IndexOf(destroyDrone as CpuBattleDrone);
            _watchDrones.RemoveAt(droneIndex);

            // リスポーンドローン取得
            var drone = respawnDrone as CpuBattleDrone;

            // リスポーンされた場合は再度観戦対象に追加
            if (respawnDrone != null)
            {
                _watchDrones.Insert(droneIndex, drone);
            }

            // 破壊されたドローンが現在観戦中のCPUの場合
            if (droneIndex == _watchingDrone)
            {
                // 残機0の場合は次のCPUへ切り替え
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
            _watchDrones[_watchingDrone].IsWatch = false;

            // 次のCPU
            _watchingDrone++;
            if (_watchingDrone >= _watchDrones.Count)
            {
                _watchingDrone = 0;
            }

            // カメラ参照設定
            _watchDrones[_watchingDrone].IsWatch = true;
        }
    }
}