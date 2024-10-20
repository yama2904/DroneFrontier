using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DroneItemComponent : MonoBehaviour
{
    [SerializeField, Tooltip("所持できるアイテム数")]
    private int _maxItemNum = 2;

    [SerializeField, Tooltip("所持アイテムの枠画像")]
    private RectTransform[] _itemFrameImages = null;

    /// <summary>
    /// 所持アイテム情報
    /// </summary>
    private class ItemData
    {
        /// <summary>
        /// アイコンの親となるアイテム枠の座標
        /// </summary>
        public Transform ItemFrameTransform { get; set; } = null;

        /// <summary>
        /// アイテム本体
        /// </summary>
        public IGameItem Item { get; set; } = null;

        /// <summary>
        /// アイテムアイコン
        /// </summary>
        public Image Icon { get; set; } = null;

        /// <summary>
        /// アイテム所持中であるか
        /// </summary>
        public bool Having { get; set; } = false;
    }

    /// <summary>
    /// 各アイテム情報
    /// </summary>
    private List<ItemData> _itemDatas = new List<ItemData>();

    /// <summary>
    /// 所持アイテムを設定
    /// </summary>
    /// <param name="item">設定するアイテム</param>
    /// <returns>アイテム枠が全て埋まっている場合はfalse</returns>
    public bool SetItem(SpawnItem item)
    {
        foreach (ItemData data in _itemDatas)
        {
            // アイテム所持中の場合は次の枠
            if (data.Having) continue;

            // アイテム枠がある場合は設定するアイテムのアイコンを表示
            RectTransform rect = null;
            if (data.ItemFrameTransform != null)
            {
                rect = Instantiate(item.IconImage);
                rect.SetParent(data.ItemFrameTransform, false);
            }

            // リストの情報を更新
            data.Item = item.Item.GetComponent<IGameItem>();
            data.Icon = rect?.GetComponent<Image>();
            data.Having = true;

            return true;
        }
        return false;
    }


    /// <summary>
    /// 指定された番号のアイテム使用
    /// </summary>
    /// <param name="number">アイテムを使用する番号</param>
    /// <returns>使用に成功した場合true</returns>
    public bool UseItem(int number)
    {
        ItemData data = _itemDatas[number];

        // アイテムを持っていない
        if (!data.Having) return false;

        // アイテム存在チェック
        if (data.Item == null) return false;

        // アイテム使用
        if (!data.Item.UseItem(gameObject)) return false;

        // アイコンを表示している場合は削除
        if (data.Icon != null)
        {
            Destroy(data.Icon);
        }

        // リストの情報を初期化
        data.Item = null;
        data.Icon = null;
        data.Having = false;

        return true;
    }

    private void Awake()
    {
        // アイテム情報初期化
        for (int i = 0; i < _maxItemNum; i++)
        {
            ItemData itemData = new ItemData();
            if (_itemFrameImages.Length > i)
            {
                itemData.ItemFrameTransform = _itemFrameImages[i];
            }
            _itemDatas.Add(itemData);
        }
    }
}