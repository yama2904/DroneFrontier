using System;

namespace Drone.Battle
{
    public interface ISpawnItem
    {
        /// <summary>
        /// スポーンアイテム消滅イベント
        /// </summary>
        public event EventHandler SpawnItemDestroyEvent;

        /// <summary>
        /// 取得時に使用可能となるアイテム
        /// </summary>
        public IDroneItem DroneItem { get; }
    }
}