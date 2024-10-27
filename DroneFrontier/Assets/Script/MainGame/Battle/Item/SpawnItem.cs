using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpawnItem : MonoBehaviour, IRadarable
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

    /// <summary>
    /// アイテムのアイコン
    /// </summary>
    public Image IconImage { get { return _iconImage; } }

    /// <summary>
    /// 取得時に使用可能となるアイテム
    /// </summary>
    public GameObject DroneItem { get { return _droneItem; } }

    public IRadarable.ObjectType Type => IRadarable.ObjectType.Item;

    public bool IsRadarable => true;

    public List<GameObject> NotRadarableList => new List<GameObject>();

    [SerializeField, Tooltip("アイテム所持中に表示するアイコン")]
    private Image _iconImage = null;

    [SerializeField, Tooltip("取得時に使用可能となるアイテム")]
    private GameObject _droneItem = null;

    private void OnDestroy()
    {
        SpawnItemDestroyEvent?.Invoke(this);
    }
}
