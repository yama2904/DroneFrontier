using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Offline
{
    public class Item : MonoBehaviour
    {
        //アイテムの種類
        public enum ItemType
        {
            BARRIER_STRENGTH,
            JAMMING,
            STUN_GRENADE,

            NONE
        }
        public ItemType Type { get; private set; } = ItemType.NONE;

        [SerializeField] GameObject barrierObecjt = null;
        [SerializeField] GameObject jammingObject = null;
        [SerializeField] GameObject stunGrenadeObject = null;


        void Start()
        {
            if (Type == ItemType.NONE) return;

            GameObject o = null;
            if (Type == ItemType.BARRIER_STRENGTH)
            {
                o = Instantiate(barrierObecjt);
            }
            else if (Type == ItemType.JAMMING)
            {
                o = Instantiate(jammingObject);
            }
            else if (Type == ItemType.STUN_GRENADE)
            {
                o = Instantiate(stunGrenadeObject);
            }

            Transform t = o.transform;
            t.SetParent(transform);
            t.localPosition = new Vector3(0, 0, 0);
        }

        public void SetRandomItemType()
        {
            Type = (ItemType)Random.Range(0, (int)ItemType.NONE);
        }

        void OnDestroy()
        {
            ItemCreateManager.Singleton.NewItemSpawn();
        }
    }
}