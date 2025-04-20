using Drone.Battle;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Network
{
    public class NetworkSpawnBarrierStrengthenItem : MyNetworkBehaviour, ISpawnItem, IRadarable
    {
        /// <summary>
        /// �X�|�[���A�C�e�����ŃC�x���g
        /// </summary>
        public event EventHandler SpawnItemDestroyEvent;

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
            SpawnItemDestroyEvent?.Invoke(this, EventArgs.Empty);
        }
    }
}