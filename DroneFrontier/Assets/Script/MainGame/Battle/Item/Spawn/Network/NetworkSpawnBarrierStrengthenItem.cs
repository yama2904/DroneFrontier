using Battle.DroneItem;
using Drone.Battle;
using Network;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Battle.SpawnItem.Network
{
    public class NetworkSpawnBarrierStrengthenItem : NetworkBehaviour, ISpawnItem, IRadarable
    {
        /// <summary>
        /// スポーンアイテム消滅イベント
        /// </summary>
        public event EventHandler OnSpawnItemDestroy;

        /// <summary>
        /// 取得時に使用可能となるアイテム
        /// </summary>
        public IDroneItem DroneItem => new BarrierStrengthenItem();

        public IRadarable.ObjectType Type => IRadarable.ObjectType.Item;

        public bool IsRadarable => true;

        public List<GameObject> NotRadarableList => new List<GameObject>();

        public override string GetAddressKey()
        {
            return "NetworkSpawnBarrierStrengthenItem";
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            OnSpawnItemDestroy?.Invoke(this, EventArgs.Empty);
        }
    }
}