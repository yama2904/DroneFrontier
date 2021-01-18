using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerItemAction : NetworkBehaviour
{
    [SerializeField] Jamming jamming = null;
    [SerializeField] StunGrenade stunGrenade = null;

    //アイテムを使用する
    public void UseItem(Item.ItemType type)
    {
        //バリア強化
        if (type == Item.ItemType.BARRIER_STRENGTH)
        {
            //強化できなかったらアイテムを消去しない
            if (!BarrierStrength.Strength(GetComponent<Player>()))
            {
                return;
            }
        }

        //ジャミング
        else if (type == Item.ItemType.JAMMING)
        {
            CmdCreateJamming(gameObject);
        }

        //スタングレネード
        else if (type == Item.ItemType.STUN_GRENADE)
        {
            CmdCreateStunGrenade(gameObject);
        }

        //デバッグ用
        Debug.Log("アイテム使用");
    }

    [Command(ignoreAuthority = true)]
    void CmdCreateJamming(GameObject player)
    {
        Jamming j = Instantiate(jamming);
        NetworkServer.Spawn(j.gameObject);
        j.CmdCreateBot(player);
    }

    [Command(ignoreAuthority = true)]
    void CmdCreateStunGrenade(GameObject player)
    {
        StunGrenade s = Instantiate(stunGrenade);
        s.ThrowGrenade(player);
        NetworkServer.Spawn(s.gameObject);
    }
}
