using Drone.Race.Network;
using UnityEngine;

namespace Race.Network
{
    public class NetworkSpawnManager : MonoBehaviour
    {
        [SerializeField, Tooltip("プレイヤードローン")]
        private NetworkRaceDrone _playerDrone = null;

        [SerializeField, Tooltip("ドローンスポーン位置")]
        private Transform[] _droneSpawnPositions = null;

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
        public NetworkRaceDrone SpawnDrone(string name)
        {
            // スポーン位置取得
            Transform spawnPos = _droneSpawnPositions[_nextSpawnIndex];

            // ドローン生成
            NetworkRaceDrone drone = Instantiate(_playerDrone, spawnPos.position, spawnPos.rotation);
            drone.Initialize(name);
            drone.enabled = false;

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
    }
}