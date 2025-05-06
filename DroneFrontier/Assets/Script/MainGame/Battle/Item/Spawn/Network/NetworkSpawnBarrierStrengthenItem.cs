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
        /// �X�|�[���A�C�e�����ŃC�x���g
        /// </summary>
        public event EventHandler OnSpawnItemDestroy;

        /// <summary>
        /// �擾���Ɏg�p�\�ƂȂ�A�C�e��
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