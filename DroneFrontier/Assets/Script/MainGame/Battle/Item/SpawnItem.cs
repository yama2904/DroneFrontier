using UnityEngine;
using UnityEngine.UI;

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

    [SerializeField, Tooltip("アイテム所持中に表示するアイコン")]
    private Image _iconImage = null;

    [SerializeField, Tooltip("スポーンさせるアイテム")]
    private GameObject _item = null;

    /// <summary>
    /// アイテムのアイコン
    /// </summary>
    public Image IconImage { get { return _iconImage; } }

    /// <summary>
    /// スポーンしたアイテム
    /// </summary>
    public GameObject Item { get { return _item; } }

    private void OnDestroy()
    {
        SpawnItemDestroyEvent?.Invoke(this);
    }
}
