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
    [SyncVar, HideInInspector] int type = (int)ItemType.NONE;
    public ItemType Type
    {
        get { return (ItemType)type; }
        //set
        //{
        //    int v = value;
        //    if(v < 0)
        //    {
        //        v = 0;
        //    }
        //    if(v > (int)ItemType.NONE)
        //    {
        //        v = (int)ItemType.NONE - 1;
        //    }
        //    type = v;
        //}
    }

    [SerializeField] GameObject barrierObecjt = null;
    [SerializeField] GameObject jammingObject = null;
    [SerializeField] GameObject stunGrenadeObject = null;


    public override void OnStartClient()
    {
        base.OnStartClient();

        if (type == (int)ItemType.NONE) return;

        GameObject o = null;
        if (type == (int)ItemType.BARRIER_STRENGTH)
        {
            o = Instantiate(barrierObecjt);
        }
        else if (type == (int)ItemType.JAMMING)
        {
            o = Instantiate(jammingObject);
        }
        else if (type == (int)ItemType.STUN_GRENADE)
        {
            o = Instantiate(stunGrenadeObject);
        }

        Transform t = o.transform;
        t.SetParent(transform);
        t.localPosition = new Vector3(0, 0, 0);
    }

    [Server]
    public void InitItemType()
    {
        type = Random.Range(0, (int)ItemType.NONE);        
    }
}
