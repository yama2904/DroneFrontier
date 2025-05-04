using UnityEngine;

namespace Drone.Battle
{
    /// <summary>
    /// 実装したオブジェクトをダメージ可能とするインターフェース
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// 実装オブジェクトの所有者
        /// </summary>
        GameObject Owner { get; }

        /// <summary>
        /// ダメージイベント
        /// </summary>
        event DamageHandler OnDamage;

        /// <summary>
        /// 実装したオブジェクトにダメージを与える
        /// </summary>
        /// <param name="source">ダメージ元オブジェクト</param>
        /// <param name="value">ダメージ量</param>
        /// <returns>ダメージに成功した場合はtrue</returns>
        bool Damage(GameObject source, float value);
    }

    /// <summary>
    /// ダメージハンドラー
    /// </summary>
    /// <param name="sender">イベントオブジェクト</param>
    /// <param name="source">ダメージを与えたオブジェクト</param>
    /// <param name="damage">ダメージ量</param>
    public delegate void DamageHandler(IDamageable sender, GameObject source, float damage);
}