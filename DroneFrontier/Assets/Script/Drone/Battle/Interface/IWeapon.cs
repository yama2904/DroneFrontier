using System;
using UnityEngine;

namespace Drone.Battle
{
    public interface IWeapon
    {
        /// <summary>
        /// íLÒ
        /// </summary>
        public GameObject Owner { get; }

        /// <summary>
        /// Seâ[Cxg
        /// </summary>
        public event EventHandler OnBulletFull;

        /// <summary>
        /// ce³µCxg
        /// </summary>
        public event EventHandler OnBulletEmpty;

        /// <summary>
        /// AddressableÌL[¼
        /// </summary>
        /// <returns></returns>
        public string GetAddressKey();

        /// <summary>
        /// ú»
        /// </summary>
        /// <param name="owner">íLÒ</param>
        public void Initialize(GameObject owner);

        /// <summary>
        /// eÛ­Ë
        /// </summary>
        /// <param name="target">Ç]æIuWFNg</param>
        public void Shot(GameObject target = null);
    }
}