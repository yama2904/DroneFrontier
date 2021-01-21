using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerItemAction : NetworkBehaviour
{
    [SerializeField] Jamming jamming = null;
    [SerializeField] StunGrenade stunGrenade = null;

    //アイテムを使用する
    //成功したらtrue
    public bool UseItem(Item.ItemType type)
    {
        //バリア強化
        if (type == Item.ItemType.BARRIER_STRENGTH)
        {
            //強化できなかったらアイテムを消去しない
            if (!BarrierStrength.Strength(GetComponent<BattleDrone>()))
            {
                Debug.Log("バリア強化中なので使用できません");
                return false;
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
            CmdCreateStunGrenade(netId);
        }

        //デバッグ用
        Debug.Log("アイテム使用");


        return true;
    }

    [Command(ignoreAuthority = true)]
    void CmdCreateJamming(GameObject player)
    {
        Jamming j = Instantiate(jamming);
        NetworkServer.Spawn(j.gameObject);
        j.CmdCreateBot(player);
    }

    [Command(ignoreAuthority = true)]
    void CmdCreateStunGrenade(uint netId)
    {
        StunGrenade s = Instantiate(stunGrenade);
        s.ThrowGrenade(gameObject);
        NetworkServer.Spawn(s.gameObject);
    }
}
