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
    public ItemType type = ItemType.NONE;

    [SerializeField] Jamming jammingInspector = null;
    [SerializeField] StunGrenade stunGrenadeInspector = null;
    static Jamming jamming = null;
    static StunGrenade stunGrenade = null;

    void Awake()
    {
        jamming = jammingInspector;
        stunGrenade = stunGrenadeInspector;
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
            Jamming j = Instantiate(jamming);
            NetworkServer.Spawn(j.gameObject);
            j.CmdCreateBot(player.gameObject);
        }

        //スタングレネード
        else if(type == ItemType.STUN_GRENADE)
        {
            StunGrenade s = Instantiate(stunGrenade);
            s.ThrowGrenade(player.gameObject);
            NetworkServer.Spawn(s.gameObject);
        }

        //デバッグ用
        Debug.Log("アイテム使用");
    }
}
