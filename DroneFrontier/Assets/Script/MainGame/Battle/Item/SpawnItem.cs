using UnityEngine;

public class SpawnItem : MonoBehaviour
{
    /// <summary>
    /// スポーンアイテム消滅イベント
    /// </summary>
    /// <param name="item">消滅したスポーンアイテム</param>
    public delegate void SpawnItemDestroyHandler(SpawnItem item);

    /// <summary>
    /// スポーンアイテム消滅イベント
    /// </summary>
    public event SpawnItemDestroyHandler SpawnItemDestroyEvent; 

    [SerializeField]
    private RectTransform _iconImage = null;

    [SerializeField]
    private GameObject _item = null;

    /// <summary>
    /// アイテムのアイコン
    /// </summary>
    public RectTransform IconImage { get { return _iconImage; } }

    /// <summary>
    /// スポーンしたアイテム
    /// </summary>
    public GameObject Item { get { return _item; } }

    private void OnDestroy()
    {
        SpawnItemDestroyEvent?.Invoke(this);
    }
}
