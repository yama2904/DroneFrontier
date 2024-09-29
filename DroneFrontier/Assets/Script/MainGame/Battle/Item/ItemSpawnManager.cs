using System.Collections.Generic;
using System.Linq;
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
    /// アイテムスポナー（アイテムスポーン済みの場合はtrue）
    /// </summary>
    private Dictionary<ItemSpawner, bool> _spawners = new Dictionary<ItemSpawner, bool>();

    /// <summary>
    /// 前回スポーンからの経過時間
    /// </summary>
    float _spawnTimeCount = 0;

    private void Start()
    {
        // 各スポナーを検索して取得
        ItemSpawner[] spawners = FindObjectsByType<ItemSpawner>(FindObjectsSortMode.None).ToArray();
        
        // スポナー情報保持
        foreach (ItemSpawner spawner in spawners)
        {
            _spawners.Add(spawner, false);
        }

        // アイテムのランダムスポーン
        ItemSpawn(spawners, _maxSpawnNum);
    }

    private void Update()
    {
        _spawnTimeCount += Time.deltaTime;
        if (_spawnTimeCount < _spawnInterval) return;

        // 経過時間リセット
        _spawnTimeCount = 0;

        // 未スポーンのスポナーを集計
        List<ItemSpawner> notSpawned = new List<ItemSpawner>();
        lock (_spawners)
        {
            foreach (ItemSpawner spawner in _spawners.Keys)
            {
                if (!_spawners[spawner])
                {
                    notSpawned.Add(spawner);
                }
            }

            if (_spawners.Count - notSpawned.Count >= _maxSpawnNum) return;
        }

        // スポーン実行
        ItemSpawn(notSpawned.ToArray(), _spawnNum);
    }

    /// <summary>
    /// 指定されたスポナーの中からランダムにアイテムスポーン
    /// </summary>
    /// <param name="spawners">アイテムスポーンさせるスポナー</param>
    /// <param name="spawnNum">スポーン数</param>
    private void ItemSpawn(ItemSpawner[] spawners, int spawnNum)
    {
        for (int num = 0; num < spawnNum; num++)
        {
            // 各スポナーのスポーン確率を計算
            int maxRandom = 0;
            List<int> percents = new List<int>();
            foreach (ItemSpawner spawner in  spawners)
            {
                int percent = (int)(spawner.SpawnPercent * 100);
                percents.Add(maxRandom + percent);
                maxRandom += percent;
            }

            // スポーンアイテムをランダムに決定
            int value = Random.Range(0, maxRandom + 1);
            for (int i = 0; i < percents.Count; i++)
            {
                ItemSpawner spawner = spawners[i];

                if (value <= percents[i])
                {
                    spawner.SpawnItem();
                    _spawners[spawner] = true;
                    continue;
                }
            }
        }
    }

    /// <summary>
    /// スポーンアイテム消滅イベント
    /// </summary>
    /// <param name="spawner">消滅したアイテムのスポナー</param>
    private void SpawnItemDestroy(ItemSpawner spawner)
    {
        // 未スポーン状態へ更新
        lock (_spawners)
        {
            _spawners[spawner] = false;
        }
    }
}
