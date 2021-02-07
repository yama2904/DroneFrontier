using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Offline
{
    namespace CPU
    {
        public class DroneItemAction : MonoBehaviour
        {
            //アイテムを使用した際に生成するオブジェクト
            [SerializeField] Jamming jamming = null;
            [SerializeField] StunGrenade stunGrenade = null;


            //取得しているアイテム情報
            class ItemData
            {
                public Item.ItemType type = Item.ItemType.NONE;
                public bool isUsing = false;
            }
            List<ItemData> itemDatas = new List<ItemData>();


            //初期化
            public void Init(int itemNum)
            {
                for (int i = itemNum - 1; i >= 0; i--)
                {
                    //リストに追加
                    itemDatas.Add(new ItemData());
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

                    //リストの情報を更新
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
                    if (!BarrierStrength.Strength(GetComponent<DroneStatusAction>()))
                    {
                        return false;
                    }
                }

                //ジャミング
                else if (type == Item.ItemType.JAMMING)
                {
                    Jamming j = Instantiate(jamming);
                    j.CreateBot(GetComponent<BaseDrone>());
                }

                //スタングレネード
                else if (type == Item.ItemType.STUN_GRENADE)
                {
                    StunGrenade s = Instantiate(stunGrenade);
                    s.ThrowGrenade(gameObject);
                }

                //デバッグ用
                Debug.Log("アイテム使用");


                return true;
            }
        }
    }
}