using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class DroneChildObject : NetworkBehaviour
{
    public enum Child
    {
        DRONE_OBJECT,
        BARRIER,
        MAIN_WEAPON,
        SUB_WEAPON,

        NONE
    }
    NetworkTransformChild[] childs = new NetworkTransformChild[(int)Child.NONE];
    SyncList<GameObject> syncObjects = new SyncList<GameObject>();


    void Awake()
    {
        base.OnStartClient();
        childs = GetComponents<NetworkTransformChild>();

        for(int i = 0; i < (int)Child.NONE; i++)
        {
            syncObjects.Add(childs[i].gameObject);
        }
    }

    public void SetChild(Transform target, Child child)
    {
        childs[(int)child].target = target;
    }

    public Transform GetChild(Child child)
    {
        return childs[(int)child].target;
    }
}
