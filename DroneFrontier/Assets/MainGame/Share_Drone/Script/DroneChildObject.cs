using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace Online
{
    public class DroneChildObject : NetworkBehaviour
    {
        public enum Child
        {
            DRONE_OBJECT,
            BARRIER,

            NONE
        }
        [SerializeField] Transform droneObject = null;
        [SerializeField] Transform barrier = null;

        NetworkTransformChild[] childs = new NetworkTransformChild[(int)Child.NONE];


        public override void OnStartClient()
        {
            base.OnStartClient();

            childs = GetComponents<NetworkTransformChild>();
            childs[(int)Child.DRONE_OBJECT].target = droneObject;
            childs[(int)Child.BARRIER].target = barrier;
        }

        public void SetChild(Transform target, Child child)
        {
            childs[(int)child].target = target;
        }

        public Transform GetChild(Child child)
        {
            return childs[(int)child].target;
        }

        [ClientRpc]
        public void RpcSetActive(bool flag, Child child)
        {
            childs[(int)child].target.gameObject.SetActive(flag);
        }
    }
}