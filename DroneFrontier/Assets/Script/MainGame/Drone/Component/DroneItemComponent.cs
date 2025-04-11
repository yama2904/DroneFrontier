using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DroneItemComponent : MonoBehaviour, IDroneComponent
{
    /// <summary>
    /// アイテムUIを非表示にするか
    /// </summary>
    public bool HideItemUI
    {
        get { return _hideItemUI; }
        set
        {
            if (_itemFrameImages != null)
            {
                foreach (Image image in _itemFrameImages)
                {
                    image.enabled = !value;
                }
            }
            _hideItemUI = value;
        }
    }
    private bool _hideItemUI = false;

    [SerializeField, Tooltip("所持できるアイテム数")]
    private int _maxItemNum = 2;

    [SerializeField, Tooltip("所持アイテムの枠画像")]
    private Image[] _itemFrameImages = null;

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
        public IDroneItem Item { get; set; } = null;

        /// <summary>
        /// アイテムアイコン
        /// </summary>
        public Image Icon { get; set; } = null;

        /// <summary>
        /// アイテム所持中であるか
        /// </summary>
        public bool HasItem { get; set; } = false;
    }

    /// <summary>
    /// 各アイテム情報
    /// </summary>
    private List<ItemData> _itemDatas = new List<ItemData>();

    public void Initialize() { }

    /// <summary>
    /// 所持アイテムを設定
    /// </summary>
    /// <param name="item">設定するアイテム</param>
    /// <returns>アイテム枠が全て埋まっている場合はfalse</returns>
    public bool SetItem(IDroneItem item)
    {
        foreach (ItemData data in _itemDatas)
        {
            // アイテム所持中の場合は次の枠
            if (data.HasItem) continue;

            // アイテム情報更新
            data.Item = item;
            data.HasItem = true;

            // アイテム枠が表示されており、所持アイテムにアイコンが設定されている場合はアイコンを表示
            if (!_hideItemUI && data.ItemFrameTransform != null)
            {
                // アイコン生成
                Image icon = item.InstantiateIcon();
                if (icon != null)
                {
                    icon.transform.SetParent(data.ItemFrameTransform, false);
                    data.Icon = icon;
                }
            }

            return true;
        }
        return false;
    }

    /// <summary>
    /// 指定された番号にアイテムを持っているか
    /// </summary>
    /// <param name="number">アイテムを持っているかチェックする番号</param>
    /// <returns>アイテムを持っている場合はtrue</returns>
    public bool HasItem(int number)
    {
        return _itemDatas[number].HasItem;
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
        if (!data.HasItem) return false;

        // アイテム存在チェック
        if (data.Item == null) return false;

        // アイテム使用
        if (!data.Item.UseItem(gameObject)) return false;

        // アイコンを表示している場合は削除
        if (data.Icon != null)
        {
            Destroy(data.Icon.gameObject);
        }

        // リストの情報を初期化
        data.Item = null;
        data.Icon = null;
        data.HasItem = false;

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
                itemData.ItemFrameTransform = _itemFrameImages[i].rectTransform;
            }
            _itemDatas.Add(itemData);
        }
    }
}