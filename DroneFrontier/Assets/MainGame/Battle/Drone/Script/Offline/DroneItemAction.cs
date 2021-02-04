using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Offline
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

        //各アイテムのアイコン
        [SerializeField] RectTransform barrierIconImage = null;
        [SerializeField] RectTransform jammingIconImage = null;
        [SerializeField] RectTransform stunGrenadeIconImage = null;


        //画像を生成する際に必要な情報
        class ItemData
        {
            public Vector3 anchoredPos;  //画像を生成した際にセットするローカル座標
            public Image createImage = null;   //生成した画像
            public Item.ItemType type = Item.ItemType.NONE;
            public bool isUsing = false;
        }
        List<ItemData> itemDatas = new List<ItemData>();


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
                id.anchoredPos = rect.anchoredPosition;
                itemDatas.Add(id);
            }
        }

        public void ResetItem()
        {
            foreach (ItemData id in itemDatas)
            {
                if (!id.isUsing) continue;

                Destroy(id.createImage);
                id.type = Item.ItemType.NONE;
                id.isUsing = false;
            }
        }

        //所持アイテムを更新する
        //成功したらtrue
        public bool SetItem(Item.ItemType type)
        {
            //バグ防止
            if (type == Item.ItemType.NONE) return false;

            foreach (ItemData id in itemDatas)
            {
                if (id.isUsing) continue;

                //取得したアイテムのUIの表示
                RectTransform rect = null;
                if (type == Item.ItemType.BARRIER_STRENGTH)
                {
                    rect = Instantiate(barrierIconImage);
                }
                if (type == Item.ItemType.JAMMING)
                {
                    rect = Instantiate(jammingIconImage);
                }
                if (type == Item.ItemType.STUN_GRENADE)
                {
                    rect = Instantiate(stunGrenadeIconImage);
                }
                rect.SetParent(UIParentCanvas.transform);
                rect.anchoredPosition = id.anchoredPos;

                //リストの情報を更新
                id.createImage = rect.GetComponent<Image>();
                id.type = type;
                id.isUsing = true;

                return true;
            }
            return false;
        }


        //アイテムを使用する
        //成功したらtrue
        public bool UseItem(int number)
        {
            //バグ防止
            if (number >= itemDatas.Count) return false;

            ItemData id = itemDatas[number];  //名前省略

            //アイテムを持っていない
            if (!id.isUsing) return false;

            //アイテム使用失敗したらfalse
            if (!UseItem(id.type)) return false;

            //リストの情報を更新
            Destroy(id.createImage);
            id.type = Item.ItemType.NONE;
            id.isUsing = false;

            return true;
        }

        bool UseItem(Item.ItemType type)
        {
            //バリア強化
            if (type == Item.ItemType.BARRIER_STRENGTH)
            {
                //強化できなかったらアイテムを消去しない
                if (!BarrierStrength.Strength(GetComponent<BattleDrone>()))
                {
                    return false;
                }
            }

            //ジャミング
            else if (type == Item.ItemType.JAMMING)
            {
                CreateJamming(gameObject);
            }

            //スタングレネード
            else if (type == Item.ItemType.STUN_GRENADE)
            {
                CreateStunGrenade(gameObject);
            }

            //デバッグ用
            Debug.Log("アイテム使用");


            return true;
        }
        
        void CreateJamming(GameObject player)
        {
            Jamming j = Instantiate(jamming);
            j.CreateBot(player);
        }
        
        void CreateStunGrenade(GameObject player)
        {
            StunGrenade s = Instantiate(stunGrenade);
            s.ThrowGrenade(player);
        }
    }
}