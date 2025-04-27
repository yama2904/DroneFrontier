using Battle.Weapon;
using Common;
using Drone;
using Drone.Battle;
using Drone.Battle.Network;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Battle.Network
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
        /// 各ドローンの初期情報
        /// </summary>
        private Dictionary<string, (WeaponType weapon, Transform pos)> _initDatas = new Dictionary<string, (WeaponType weapon, Transform pos)>();

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
            NetworkBattleDrone drone = CreateDrone(spawnPos.position, spawnPos.rotation);
            IWeapon main = WeaponCreater.CreateWeapon(WeaponType.GATLING);
            IWeapon sub = WeaponCreater.CreateWeapon(weapon);
            drone.Initialize(name, main, sub, drone.StockNum);
            drone.enabled = false;

            // スポーン時点情報を保存
            _initDatas.Add(drone.Name, (weapon, spawnPos));

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
        /// <param name="pos">スポーン位置</param>
        /// <param name="rotate">スポーン向き</param>
        /// <returns>生成したドローン</returns>
        private NetworkBattleDrone CreateDrone(Vector3 pos, Quaternion rotate)
        {
            NetworkBattleDrone createdDrone = Instantiate(_playerDrone, pos, rotate);
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

            // 破壊されたドローンの初期情報取得
            var initData = _initDatas[drone.Name];

            // リスポーンさせたドローン
            NetworkBattleDrone respawnDrone = null;

            if (drone.StockNum > 0)
            {
                // リスポーン
                respawnDrone = CreateDrone(initData.pos.position, initData.pos.rotation);
                respawnDrone.enabled = true;
                IWeapon main = WeaponCreater.CreateWeapon(WeaponType.GATLING);
                IWeapon sub = WeaponCreater.CreateWeapon(initData.weapon);
                respawnDrone.Initialize(drone.Name, main, sub, drone.StockNum - 1);

                // 復活SE再生
                respawnDrone.GetComponent<DroneSoundComponent>().Play(SoundManager.SE.Respawn);
            }

            // イベント発火
            DroneDestroyEvent?.Invoke(drone, respawnDrone);

            // 破壊されたドローンからイベントの削除
            drone.DroneDestroyEvent -= DroneDestroy;
        }
    }
}