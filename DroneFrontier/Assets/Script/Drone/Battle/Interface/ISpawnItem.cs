using System;

namespace Drone.Battle
{
    public interface ISpawnItem
    {
        /// <summary>
        /// �X�|�[���A�C�e�����ŃC�x���g
        /// </summary>
        public event EventHandler SpawnItemDestroyEvent;

        /// <summary>
        /// �擾���Ɏg�p�\�ƂȂ�A�C�e��
        /// </summary>
        public IDroneItem DroneItem { get; }
    }
}