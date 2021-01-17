using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Item : NetworkBehaviour
{
    const string FOLDER_PATH = "Item/";     //Resourcesフォルダのパス

    //アイテムの種類
    public enum ItemType
    {
        BARRIER_STRENGTH,
        JAMMING,
        STUN_GRENADE,

        NONE
    }
    [SerializeField] ItemType itemType = ItemType.NONE;
    public ItemType type { get; private set; } = ItemType.NONE;

    void Awake()
    {
        type = itemType;
    }

    //アイテムを使用する
    public static void UseItem(Player player, ItemType type)
    {
        //バリア強化
        if(type == ItemType.BARRIER_STRENGTH)
        {
            //強化できなかったらアイテムを消去しない
            if (!BarrierStrength.Strength(player))
            {
                return;
            }
        }

        //ジャミング
        else if(type == ItemType.JAMMING)
        {
            GameObject o = Instantiate(Resources.Load(FOLDER_PATH + "Jamming")) as GameObject;
            o.GetComponent<Jamming>().CreateBot(player.gameObject);
        }

        //スタングレネード
        else if(type == ItemType.STUN_GRENADE)
        {
            GameObject o = Instantiate(Resources.Load(FOLDER_PATH + "StunGrenade")) as GameObject;
            o.GetComponent<StunGrenade>().ThrowGrenade(player.gameObject);
        }

        //デバッグ用
        Debug.Log("アイテム使用");
    }
}
