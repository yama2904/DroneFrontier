using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

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
    private List<ItemSpawner> _spawnerList = new List<ItemSpawner>();

    /// <summary>
    /// スポーンしたアイテムと対応するスポナー
    /// </summary>
    private Dictionary<SpawnItem, ItemSpawner> _spawnedMap = new Dictionary<SpawnItem, ItemSpawner>();

    /// <summary>
    /// 定期スポーン計測
    /// </summary>
    private float _spawnTimer = 0;

    private void Start()
    {
        // 各スポナーを検索して取得
        _spawnerList = FindObjectsByType<ItemSpawner>(FindObjectsSortMode.None).ToList();
        
        // アイテムのランダムスポーン
        ItemSpawn(_spawnerList, _maxSpawnNum);
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
        List<ItemSpawner> notSpawned = new List<ItemSpawner>();
        lock (_spawnedMap)
        {
            foreach (ItemSpawner spawner in _spawnerList)
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
    private void ItemSpawn(List<ItemSpawner> spawnerList, int spawnNum)
    {
        // スポナーの数がスポーン数以下の場合は全てのスポナーからアイテムスポーン
        if (spawnerList.Count <= spawnNum) 
        {
            foreach (ItemSpawner spawner in spawnerList)
            {
                // スポーン実行
                SpawnItem item = spawner.Spawn();

                // アイテム消滅イベント設定
                item.SpawnItemDestroyEvent += SpawnItemDestroy;

                // スポーン済みアイテムに追加
                lock (_spawnedMap) _spawnedMap.Add(item, spawner);
            }

            return;
        }

        // 各スポナーからランダムにアイテムスポーン
        int num = 0;
        while (true)
        {
            foreach (ItemSpawner spawner in  spawnerList)
            {
                // 既にスポーン済の場合はスポーンを行わない
                if (_spawnedMap.ContainsValue(spawner)) break;

                // スポーン実行
                SpawnItem item = spawner.Spawn();

                // スポーン成功可否
                if (item == null) continue;

                // アイテム消滅イベント設定
                item.SpawnItemDestroyEvent += SpawnItemDestroy;

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
    private void SpawnItemDestroy(SpawnItem item)
    {
        // 消滅したアイテムのスポナー取得
        ItemSpawner spawner = _spawnedMap[item];

        // 消滅したアイテムからイベント削除
        item.SpawnItemDestroyEvent -= SpawnItemDestroy;

        // スポーン済みアイテムから削除
        lock (_spawnedMap) _spawnedMap.Remove(item);
    }
}
