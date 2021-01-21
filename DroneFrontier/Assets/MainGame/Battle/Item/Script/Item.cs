using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Item : NetworkBehaviour
{
    //アイテムの種類
    public enum ItemType
    {
        BARRIER_STRENGTH,
        JAMMING,
        STUN_GRENADE,

        NONE
    }
    public ItemType type = ItemType.NONE;
}
