using Battle.DroneItem;
using Drone.Battle;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Battle.SpawnItem
{
    public class SpawnStunGrenadeItem : MonoBehaviour, ISpawnItem, IRadarable
    {
        /// <summary>
        /// スポーンアイテム消滅イベント
        /// </summary>
        public event EventHandler OnSpawnItemDestroy;

        /// <summary>
        /// 取得時に使用可能となるアイテム
        /// </summary>
        public IDroneItem DroneItem => new StunGrenadeItem();

        public IRadarable.ObjectType Type => IRadarable.ObjectType.Item;

        public bool IsRadarable => true;

        public List<GameObject> NotRadarableList => new List<GameObject>();

        private void OnDestroy()
        {
            OnSpawnItemDestroy?.Invoke(this, EventArgs.Empty);
        }
    }
}