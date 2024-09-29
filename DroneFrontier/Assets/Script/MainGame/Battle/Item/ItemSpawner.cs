using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    /// <summary>
    /// スポーン確率（0～1）
    /// </summary>
    public float SpawnPercent
    {
        get { return _spawnPercent; }
    }

    /// <summary>
    /// スポーンアイテム消滅イベント
    /// </summary>
    /// <param name="spawner">消滅したアイテムのスポナー</param>
    public delegate void SpawnItemDestroyHandler(ItemSpawner spawner);

    /// <summary>
    /// スポーンアイテム消滅イベント
    /// </summary>
    public event SpawnItemDestroyHandler SpawnItemDestroyEvent;

    [SerializeField, Tooltip("スポーンさせるアイテム一覧")]
    private GameObject[] _spawnItems = null;

    [SerializeField,  Tooltip("スポーン確率(0～1)")] 
    private float _spawnPercent = 0.5f;

    /// <summary>
    /// スポーンさせるアイテムの各クラス
    /// </summary>
    private System.Type[] _spawnItemClasses = null;

    /// <summary>
    /// 生成したアイテム
    /// </summary>
    private GameObject _createdItem = null;

    /// <summary>
    /// キャッシュ用Transform
    /// </summary>
    private Transform _transform = null;

    /// <summary>
    /// ランダムなアイテムをスポーンさせる
    /// </summary>
    public void SpawnItem()
    {
        // 前回スポーンしたアイテムが残っていることを考慮して削除
        if (_createdItem != null)
        {
            Destroy(_createdItem);
        }

        // ランダムにスポーン
        int index = Random.Range(0, _spawnItems.Length);
        _createdItem = Instantiate(_spawnItems[index], _transform);
        _createdItem.transform.SetParent(_transform);

        // アイテム消去イベント設定
        _createdItem.GetComponent<SpawnItem>().SpawnItemDestroyEvent += (item) =>
        {
            SpawnItemDestroyEvent?.Invoke(this);
        };
    }

    /// <summary>
    /// 指定したアイテムをスポーンさせる
    /// </summary>
    /// <param name="itemType">スポーンさせるアイテム</param>
    /// <returns>true:指定されたアイテムが存在する場合はtrue</returns>
    public bool SpawnItem(System.Type itemType)
    {
        // 指定されたアイテムに合致したらスポーン
        for (int i = 0; i < _spawnItems.Length; i++)
        {
            // 指定されたアイテムであるか
            if (_spawnItemClasses[i] == itemType)
            {
                // 前回スポーンしたアイテムが残っていることを考慮して削除
                if (_createdItem != null)
                {
                    Destroy(_createdItem);
                }

                // アイテムスポーン
                _createdItem = Instantiate(_spawnItems[i], _transform);
                _createdItem.transform.SetParent(_transform);

                // アイテム消去イベント設定
                _createdItem.GetComponent<SpawnItem>().SpawnItemDestroyEvent += (item) =>
                {
                    SpawnItemDestroyEvent?.Invoke(this);
                };

                return true;
            }
        }

        // 指定されたアイテムが存在しない
        return false;
    }

    void Start()
    {
        // Transformをキャッシュ保存しておく
        _transform = transform;

        // 各アイテムのクラスを保持しておく
        _spawnItemClasses = new System.Type[_spawnItems.Length];
        for (int i = 0; i < _spawnItems.Length; i++)
        {
            _spawnItemClasses[i] = _spawnItems[i].GetType();
        }
    }

    void Update() { }
}
