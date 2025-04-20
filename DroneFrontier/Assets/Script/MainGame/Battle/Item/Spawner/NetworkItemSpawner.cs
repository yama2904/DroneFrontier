using Drone.Battle;
using UnityEngine;

namespace Network
{
    public class NetworkItemSpawner : MyNetworkBehaviour, IItemSpawner
    {
        /// <summary>
        /// スポーン確率（0～1）
        /// </summary>
        public float SpawnPercent
        {
            get { return _spawnPercent; }
        }

        [SerializeField, Tooltip("スポーンさせるアイテム一覧")]
        private MyNetworkBehaviour[] _spawnItems = null;

        [SerializeField, Tooltip("スポーン確率(0～1)")]
        private float _spawnPercent = 0.5f;

        /// <summary>
        /// キャッシュ用Transform
        /// </summary>
        private Transform _transform = null;

        /// <summary>
        /// ランダムなアイテムをスポーンさせる
        /// </summary>
        /// <returns>スポーンしたアイテム</returns>
        public ISpawnItem Spawn()
        {
            // ランダムなアイテムをスポーン
            int index = Random.Range(0, _spawnItems.Length);
            MyNetworkBehaviour item = Instantiate(_spawnItems[index], _transform);
            item.transform.SetParent(_transform);

            // 全クライアントにスポーンさせる
            NetworkObjectSpawner.Spawn(item);

            // スポーンしたアイテムを返す
            return item.GetComponent<ISpawnItem>();
        }

        /// <summary>
        /// スポーン確率を基に成功可否を決定し、成功した場合はランダムなアイテムをスポーンさせる
        /// </summary>
        /// <returns>スポーンしたアイテム。失敗した場合はnull</returns>
        public ISpawnItem SpawnRandom()
        {
            // スポーン確率を基に成功可否を決定
            if (Random.Range(0, 101) > _spawnPercent * 100) return null;

            // アイテム
            return Spawn();
        }

        protected override void Awake()
        {
            // Transformをキャッシュ保存しておく
            _transform = transform;
        }
    }
}