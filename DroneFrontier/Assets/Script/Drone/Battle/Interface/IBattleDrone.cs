using System;
using UnityEngine;

namespace Drone.Battle
{
    /// <summary>
    /// バトルモードのドローンを実装するインターフェース
    /// </summary>
    public interface IBattleDrone
    {
        // <summary>
        /// ドローンの名前
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// ドローンのHP
        /// </summary>
        public float HP { get; }

        /// <summary>
        /// メイン武器
        /// </summary>
        public IWeapon MainWeapon { get; }

        /// <summary>
        /// サブ武器
        /// </summary>
        public IWeapon SubWeapon { get; }

        /// <summary>
        /// 現在のストック数
        /// </summary>
        public int StockNum { get; }

        /// <summary>
        /// UI表示Canvas
        /// </summary>
        public Canvas Canvas { get; }

        /// <summary>
        /// 弾丸UI表示Canvas
        /// </summary>
        public Canvas BulletCanvas { get; }

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="name">ドローン名</param>
        /// <param name="mainWeapon">メインウェポン</param>
        /// <param name="subWeapon">サブウェポン</param>
        /// <param name="stock">ストック数</param>
        public void Initialize(string name, IWeapon mainWeapon, IWeapon subWeapon, int stock);

        /// <summary>
        /// ドローンにダメージを与える
        /// </summary>
        /// <param name="value">ダメージ量</param>
        public void Damage(float value);

        /// <summary>
        /// ドローン破壊イベント
        /// </summary>
        public event EventHandler DroneDestroyEvent;
    }
}