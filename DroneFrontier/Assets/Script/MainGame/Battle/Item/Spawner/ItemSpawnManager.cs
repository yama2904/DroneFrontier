using Common;
using Drone.Battle;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Battle.Spawner
{
    public class ItemSpawnManager : MonoBehaviour
    {
        [SerializeField, Tooltip("フィールド上に出現させるアイテムの上限")]
        private int _maxSpawnNum = 10;

        [SerializeField, Tooltip("アイテムが出現する間隔")]
        private float _spawnInterval = 10f;

        [SerializeField, Tooltip("定期的にスポーンするアイテムの数")]
        private int _spawnNum = 1;

        /// <summary>
        /// アイテムスポナーリスト
        /// </summary>
        private List<IItemSpawner> _spawnerList = new List<IItemSpawner>();

        /// <summary>
        /// スポーンしたアイテムと対応するスポナー
        /// </summary>
        private Dictionary<ISpawnItem, IItemSpawner> _spawnedMap = new Dictionary<ISpawnItem, IItemSpawner>();

        /// <summary>
        /// 定期スポーン計測
        /// </summary>
        private float _spawnTimer = 0;

        /// <summary>
        /// アイテムスポーンを有効にするか指定して初期化
        /// </summary>
        /// <param name="enableSpawn">アイテムスポーンを有効にする場合はtrue</param>
        public void Initialize(bool enableSpawn)
        {
            if (enableSpawn)
            {
                // 各スポナーを検索して取得
                MonoBehaviour[] objects = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
                foreach (MonoBehaviour obj in objects)
                {
                    if (obj is IItemSpawner spawner)
                    {
                        _spawnerList.Add(spawner);
                    }
                }

                // アイテムのランダムスポーン
                ItemSpawn(_spawnerList, _maxSpawnNum);
            }
            else
            {
                GameObject[] items = GameObject.FindGameObjectsWithTag(TagNameConst.ITEM_SPAWN);
                foreach (GameObject item in items)
                {
                    Destroy(item);
                }
                enabled = false;
            }
        }

        private void Update()
        {
            _spawnTimer += Time.deltaTime;
            if (_spawnTimer < _spawnInterval) return;

            // 経過時間リセット
            _spawnTimer = 0;

            // 最大数スポーンしている場合は新規にスポーンしない
            if (_spawnedMap.Count >= _maxSpawnNum) return;

            // 未スポーンのスポナーを集計
            List<IItemSpawner> notSpawned = new List<IItemSpawner>();
            lock (_spawnedMap)
            {
                foreach (IItemSpawner spawner in _spawnerList)
                {
                    if (!_spawnedMap.ContainsValue(spawner))
                    {
                        notSpawned.Add(spawner);
                    }
                }
            }

            // スポーン実行
            ItemSpawn(notSpawned, _spawnNum);
        }

        /// <summary>
        /// 指定された数のアイテムスポーン
        /// </summary>
        /// <param name="spawnerList">アイテムスポーンさせるスポナー</param>
        /// <param name="spawnNum">スポーン数</param>
        private void ItemSpawn(List<IItemSpawner> spawnerList, int spawnNum)
        {
            // スポナーの数がスポーン数以下の場合は全てのスポナーからアイテムスポーン
            if (spawnerList.Count <= spawnNum)
            {
                foreach (IItemSpawner spawner in spawnerList)
                {
                    // スポーン実行
                    ISpawnItem item = spawner.Spawn();

                    // アイテム消滅イベント設定
                    item.OnSpawnItemDestroy += OnSpawnItemDestroy;

                    // スポーン済みアイテムに追加
                    lock (_spawnedMap) _spawnedMap.Add(item, spawner);
                }

                return;
            }

            // 各スポナーからランダムにアイテムスポーン
            int num = 0;
            while (true)
            {
                foreach (IItemSpawner spawner in spawnerList)
                {
                    // 既にスポーン済の場合はスポーンを行わない
                    if (_spawnedMap.ContainsValue(spawner)) continue;

                    // スポーン実行
                    ISpawnItem item = spawner.SpawnRandom();

                    // スポーン成功可否
                    if (item == null) continue;

                    // アイテム消滅イベント設定
                    item.OnSpawnItemDestroy += OnSpawnItemDestroy;

                    // スポーン済みアイテムに追加
                    lock (_spawnedMap) _spawnedMap.Add(item, spawner);

                    // 指定されたスポーン数に達した場合は終了
                    num++;
                    if (num >= spawnNum) return;
                }
            }
        }

        /// <summary>
        /// スポーンアイテム消滅イベント
        /// </summary>
        /// <param name="item">消滅したアイテムのスポナー</param>
        private void OnSpawnItemDestroy(object sender, EventArgs e)
        {
            // アイテム取得
            ISpawnItem item = sender as ISpawnItem;

            // 消滅したアイテムのスポナー取得
            IItemSpawner spawner = _spawnedMap[item];

            // 消滅したアイテムからイベント削除
            item.OnSpawnItemDestroy -= OnSpawnItemDestroy;

            // スポーン済みアイテムから削除
            lock (_spawnedMap) _spawnedMap.Remove(item);
        }
    }
}