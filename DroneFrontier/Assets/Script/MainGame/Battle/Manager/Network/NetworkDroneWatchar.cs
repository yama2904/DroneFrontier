using Drone.Battle.Network;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Battle.Network
{
    public class NetworkDroneWatchar : MonoBehaviour
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
                _watchDrones[_watchingDrone].IsWatch = false;

                // 次のドローン
                _watchingDrone++;
                if (_watchingDrone >= _watchDrones.Count)
                {
                    _watchingDrone = 0;
                }

                // カメラ参照設定
                _watchDrones[_watchingDrone].IsWatch = true;
            }
        }

        private void OnEnable()
        {
            // 試合中のドローン取得
            _watchDrones = FindObjectsByType<NetworkBattleDrone>(FindObjectsSortMode.None).ToList();

            // 全てのドローンのカメラ参照初期化
            foreach (NetworkBattleDrone drone in _watchDrones)
            {
                drone.IsWatch = false;
            }

            // 参照先カメラ設定
            _watchingDrone = 0;
            _watchDrones[_watchingDrone].IsWatch = true;

            // ドローン破壊イベント設定
            _droneSpawnManager.OnDroneDestroy += OnDroneDestroy;

            // AudioListener有効化
            GetComponent<AudioListener>().enabled = true;
        }


        private void OnDisable()
        {
            // 全てのドローンのカメラ参照初期化
            foreach (NetworkBattleDrone drone in _watchDrones)
            {
                drone.IsWatch = false;
            }

            // ドローン破壊イベント削除
            _droneSpawnManager.OnDroneDestroy -= OnDroneDestroy;

            // AudioListener無効化
            GetComponent<AudioListener>().enabled = false;
        }

        /// <summary>
        /// ドローン破壊イベント
        /// </summary>
        /// <param name="destroyDrone">破壊されたドローン</param>
        /// <param name="respawnDrone">リスポーンしたドローン</param>
        private void OnDroneDestroy(NetworkBattleDrone destroyDrone, NetworkBattleDrone respawnDrone)
        {
            // 破壊されたドローンをリスポーンしたドローンに入れ替える
            int index = _watchDrones.IndexOf(destroyDrone);
            _watchDrones.RemoveAt(index);
            _watchDrones.Insert(index, respawnDrone);

            // 破壊されたドローンが現在観戦中のドローンの場合はリスポーンしたドローンを見る
            if (index == _watchingDrone)
            {
                respawnDrone.IsWatch = true;
            }
        }
    }
}