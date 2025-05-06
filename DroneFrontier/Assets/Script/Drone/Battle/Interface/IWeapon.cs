using System;
using UnityEngine;

namespace Drone.Battle
{
    public interface IWeapon
    {
        /// <summary>
        /// 武器所有者
        /// </summary>
        public GameObject Owner { get; }

        /// <summary>
        /// 全弾補充イベント
        /// </summary>
        public event EventHandler OnBulletFull;

        /// <summary>
        /// 残弾無しイベント
        /// </summary>
        public event EventHandler OnBulletEmpty;

        /// <summary>
        /// Addressableのキー名
        /// </summary>
        /// <returns></returns>
        public string GetAddressKey();

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="owner">武器所有者</param>
        public void Initialize(GameObject owner);

        /// <summary>
        /// 弾丸発射
        /// </summary>
        /// <param name="target">追従先オブジェクト</param>
        public void Shot(GameObject target = null);
    }
}