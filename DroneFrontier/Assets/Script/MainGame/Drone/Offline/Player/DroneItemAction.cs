using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Offline
{
    namespace Player
    {
        public class DroneItemAction : MonoBehaviour
        {
            //アイテムを使用した際に生成するオブジェクト
            [SerializeField] Jamming jamming = null;
            [SerializeField] StunGrenade stunGrenade = null;

            //所持アイテムUIを表示させるCanvas
            [SerializeField] Canvas UIParentCanvas = null;

            //アイテム枠の画像
            [SerializeField] RectTransform itemFrameImage = null;

            /// <summary>
            /// 所持アイテム情報
            /// </summary>
            private class ItemData
            {
                /// <summary>
                /// アイコンの座標
                /// </summary>
                public Vector3 AnchoredPosition { get; set; }

                /// <summary>
                /// アイテム本体
                /// </summary>
                public IGameItem Item { get; set; }

                /// <summary>
                /// アイテムアイコン
                /// </summary>
                public Image Icon { get; set; }

                /// <summary>
                /// アイテム所持中であるか
                /// </summary>
                public bool having = false;
            }
            private List<ItemData> itemDatas = new List<ItemData>();


            //初期化
            public void Init(int itemNum)
            {
                for (int i = itemNum - 1; i >= 0; i--)
                {
                    //アイテム枠の画像の設定
                    RectTransform rect = Instantiate(itemFrameImage);
                    rect.SetParent(UIParentCanvas.transform);
                    rect.anchoredPosition = new Vector3(((itemFrameImage.sizeDelta.x * i) + itemFrameImage.sizeDelta.x * 0.5f) * -1, itemFrameImage.sizeDelta.y * 0.5f);

                    //リストに追加
                    ItemData id = new ItemData();
                    id.AnchoredPosition = rect.anchoredPosition;
                    itemDatas.Add(id);
                }
            }

            /// <summary>
            /// 所持アイテムを設定
            /// </summary>
            /// <param name="item">設定するアイテム</param>
            /// <returns>アイテム枠が全て埋まっている場合はfalse</returns>
            public bool SetItem(SpawnItem item)
            {
                foreach (ItemData data in itemDatas)
                {
                    // アイテム所持中の場合は次の枠
                    if (data.having) continue;

                    // 設定するアイテムのアイコン表示
                    RectTransform rect = Instantiate(item.IconImage);
                    rect.SetParent(UIParentCanvas.transform);
                    rect.anchoredPosition = data.AnchoredPosition;

                    // リストの情報を更新
                    data.Item = item.Item.GetComponent<IGameItem>();
                    data.Icon = rect.GetComponent<Image>();
                    data.having = true;

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
                ItemData data = itemDatas[number];

                // アイテムを持っていない
                if (!data.having) return false;

                // アイテム存在チェック
                if (data.Item == null) return false;

                // アイテム使用
                if (!data.Item.UseItem(gameObject)) return false;

                // リストの情報を更新
                Destroy(data.Icon);
                data.Item = null;
                data.having = false;

                return true;
            }
        }
    }
}