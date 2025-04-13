using System;
using System.Collections.Generic;
using UnityEngine;

namespace Network
{
    public class NetworkDroneSpawnManager : MonoBehaviour
    {
        /// <summary>
        /// ドローン破壊イベント
        /// </summary>
        /// <param name="destroyDrone">破壊されたドローン</param>
        /// <param name="respawnDrone">リスポーンしたドローン（残機が無くなった場合はnull）</param>
        public delegate void DroneDestroyHandler(NetworkBattleDrone destroyDrone, NetworkBattleDrone respawnDrone);

        /// <summary>
        /// ドローン破壊イベント
        /// </summary>
        public event DroneDestroyHandler DroneDestroyEvent;

        [SerializeField, Tooltip("プレイヤードローン")]
        private NetworkBattleDrone _playerDrone = null;

        [SerializeField, Tooltip("ドローンスポーン位置")]
        private Transform[] _droneSpawnPositions = null;

        /// <summary>
        /// 各ドローンの初期位置
        /// </summary>
        private Dictionary<string, Transform> _initPositions = new Dictionary<string, Transform>();

        /// <summary>
        /// 次のスポーン時に使用する配列インデックス
        /// </summary>
        private int _nextSpawnIndex = -1;

        /// <summary>
        /// ドローンをスポーンさせる
        /// </summary>
        /// <param name="name">スポーンさせるドローンの名前</param>
        /// <param name="weapon">スポーンさせるドローンのサブ武器</param>
        /// <returns>スポーンさせたドローン</returns>
        public NetworkBattleDrone SpawnDrone(string name, WeaponType weapon)
        {
            // スポーン位置取得
            Transform spawnPos = _droneSpawnPositions[_nextSpawnIndex];

            // ドローン生成
            NetworkBattleDrone drone = CreateDrone(name, weapon, spawnPos);

            // スポーン位置を保存
            _initPositions.Add(drone.Name, spawnPos);

            // 次のスポーン位置
            _nextSpawnIndex++;
            if (_nextSpawnIndex >= _droneSpawnPositions.Length)
            {
                _nextSpawnIndex = 0;
            }

            return drone;
        }

        private void Awake()
        {
            // 初期スポーン位置をランダムに選択
            _nextSpawnIndex = UnityEngine.Random.Range(0, _droneSpawnPositions.Length);
        }

        /// <summary>
        /// ドローン生成
        /// </summary>
        /// <param name="weapon">ドローンに設定する名前</param>
        /// <param name="weapon">設定する武器</param>
        /// <param name="spawnPosition">スポーン位置</param>
        /// <returns>生成したドローン</returns>
        private NetworkBattleDrone CreateDrone(string name, WeaponType weapon, Transform spawnPosition)
        {
            NetworkBattleDrone createdDrone = Instantiate(_playerDrone, spawnPosition.position, spawnPosition.rotation);
            createdDrone.Name = name;
            createdDrone.SubWeapon = weapon;
            createdDrone.DroneDestroyEvent += DroneDestroy;

            return createdDrone;
        }

        /// <summary>
        /// ドローン破壊イベント
        /// </summary>
        /// <param name="sender">イベントオブジェクト</param>
        /// <param name="e">イベント引数</param>
        private void DroneDestroy(object sender, EventArgs e)
        {
            NetworkBattleDrone drone = sender as NetworkBattleDrone;

            // 破壊されたドローンの初期位置取得
            Transform initPos = _initPositions[drone.Name];

            // リスポーンさせたドローン
            NetworkBattleDrone respawnDrone = null;

            if (drone.StockNum > 0)
            {
                // リスポーン
                respawnDrone = CreateDrone(drone.Name, drone.SubWeapon, initPos);

                // 復活SE再生
                respawnDrone.GetComponent<DroneSoundComponent>().Play(SoundManager.SE.Respawn);

                // ストック数更新
                respawnDrone.StockNum = drone.StockNum - 1;
            }

            // イベント発火
            DroneDestroyEvent?.Invoke(drone, respawnDrone);

            // 破壊されたドローンからイベントの削除
            drone.DroneDestroyEvent -= DroneDestroy;
        }
    }
}