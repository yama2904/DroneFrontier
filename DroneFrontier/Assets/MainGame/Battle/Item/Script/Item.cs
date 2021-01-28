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

    [SerializeField] GameObject barrierObecjt = null;
    [SerializeField] GameObject jammingObject = null;
    [SerializeField] GameObject stunGrenadeObject = null;


    public override void OnStartClient()
    {
        base.OnStartClient();

        if (type == ItemType.NONE) return;

        GameObject o = null;
        if (type == ItemType.BARRIER_STRENGTH)
        {
            o = Instantiate(barrierObecjt);
        }
        else if (type == ItemType.JAMMING)
        {
            o = Instantiate(jammingObject);
        }
        else if (type == ItemType.STUN_GRENADE)
        {
            o = Instantiate(stunGrenadeObject);
        }

        Transform t = o.transform;
        t.SetParent(transform);
        t.localPosition = new Vector3(0, 0, 0);
    }
}
