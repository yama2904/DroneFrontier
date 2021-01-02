using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    const string FOLDER_PATH = "Item/";     //Resourcesフォルダのパス
    public const string ITEM_TAG = "Item";  //タグ名

    //アイテムの種類
    public enum ItemType
    {
        BARRIER_STRENGTH,
        JAMMING,
        STUN_GRENADE,

        NONE
    }
    [SerializeField] ItemType itemType = ItemType.NONE;

    //アイテムを使用する
    public void UseItem(BasePlayer player)
    {
        //バリア強化
        if(itemType == ItemType.BARRIER_STRENGTH)
        {
            BarrierStrength.Strength(player);
        }

        //ジャミング
        else if(itemType == ItemType.JAMMING)
        {
            GameObject o = Instantiate(Resources.Load(FOLDER_PATH + "Jamming")) as GameObject;
            o.GetComponent<Jamming>().CreateBot(player.gameObject);
        }

        //スタングレネード
        else if(itemType == ItemType.STUN_GRENADE)
        {
            GameObject o = Instantiate(Resources.Load(FOLDER_PATH + "StunGrenade")) as GameObject;
            o.GetComponent<StunGrenade>().ThrowGrenade(player.gameObject);
        }

        //アイテムを使用したら消去
        Destroy(gameObject);
    }
}
