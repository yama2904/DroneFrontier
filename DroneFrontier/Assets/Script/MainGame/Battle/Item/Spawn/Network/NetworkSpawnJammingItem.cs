using Offline;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Network
{
    public class NetworkSpawnJammingItem : MyNetworkBehaviour, ISpawnItem, IRadarable
    {
        /// <summary>
        /// スポーンアイテム消滅イベント
        /// </summary>
        public event EventHandler SpawnItemDestroyEvent;

        /// <summary>
        /// 取得時に使用可能となるアイテム
        /// </summary>
        public IDroneItem DroneItem => new JammingItem();

        public IRadarable.ObjectType Type => IRadarable.ObjectType.Item;

        public bool IsRadarable => true;

        public List<GameObject> NotRadarableList => new List<GameObject>();

        public override string GetAddressKey()
        {
            return "NetworkSpawnJammingItem";
        }

        protected override void OnDestroy()
        {
            SpawnItemDestroyEvent?.Invoke(this, EventArgs.Empty);
        }
    }
}