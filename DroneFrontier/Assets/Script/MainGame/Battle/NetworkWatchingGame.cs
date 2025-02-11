using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Network
{
    public class NetworkWatchingGame : MonoBehaviour
    {
        [SerializeField, Tooltip("ドローンスポーン管理オブジェクト")]
        private NetworkDroneSpawnManager _droneSpawnManager = null;

        /// <summary>
        /// 観戦中のドローン
        /// </summary>
        private List<NetworkBattleDrone> _watchDrones = new List<NetworkBattleDrone>();

        /// <summary>
        /// 現在カメラ参照中のドローンのインデックス
        /// </summary>
        private int _watchingDrone = 0;

        private void Update()
        {
            if (_watchDrones.Count <= 0) return;

            // スペースキーで次のドローンへカメラ切り替え
            if (Input.GetKeyDown(KeyCode.Space))
            {
                // カメラ深度初期化
                _watchDrones[_watchingDrone].Camera.depth = 0;

                // 次のドローン
                _watchingDrone++;
                if (_watchingDrone >= _watchDrones.Count)
                {
                    _watchingDrone = 0;
                }

                // カメラ参照設定
                _watchDrones[_watchingDrone].Camera.depth = 5;
            }
        }

        private void OnEnable()
        {
            // 試合中のドローン取得
            _watchDrones = FindObjectsByType<NetworkBattleDrone>(FindObjectsSortMode.None).ToList();

            // 全てのドローンのカメラ深度初期化
            foreach (NetworkBattleDrone drone in _watchDrones)
            {
                drone.Camera.depth = 0;
            }

            // 参照先カメラ設定
            _watchingDrone = 0;
            _watchDrones[_watchingDrone].Camera.depth = 5;

            // ドローン破壊イベント設定
            _droneSpawnManager.DroneDestroyEvent += DroneDestroy;

            // AudioListener有効化
            GetComponent<AudioListener>().enabled = true;
        }


        private void OnDisable()
        {
            // 全てのドローンのカメラ深度初期化
            foreach (NetworkBattleDrone drone in _watchDrones)
            {
                drone.Camera.depth = 0;
            }

            // ドローン破壊イベント削除
            _droneSpawnManager.DroneDestroyEvent -= DroneDestroy;

            // AudioListener無効化
            GetComponent<AudioListener>().enabled = false;
        }

        /// <summary>
        /// ドローン破壊イベント
        /// </summary>
        /// <param name="destroyDrone">破壊されたドローン</param>
        /// <param name="respawnDrone">リスポーンしたドローン</param>
        private void DroneDestroy(NetworkBattleDrone destroyDrone, NetworkBattleDrone respawnDrone)
        {
            // 破壊されたドローンをリスポーンしたドローンに入れ替える
            int index = _watchDrones.IndexOf(destroyDrone);
            _watchDrones.RemoveAt(index);
            _watchDrones.Insert(index, respawnDrone);

            // 破壊されたドローンが現在観戦中のドローンの場合はカメラ深度調整
            if (index == _watchingDrone)
            {
                respawnDrone.Camera.depth = 5;
            }
            else
            {
                // 観戦中ドローンでない場合はカメラ深度初期化
                respawnDrone.Camera.depth = 0;
            }
        }
    }
}