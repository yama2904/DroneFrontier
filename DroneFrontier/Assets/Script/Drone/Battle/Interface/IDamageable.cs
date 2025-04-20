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
        /// 実装したオブジェクトにダメージを与える
        /// </summary>
        /// <param name="source">ダメージ元オブジェクト</param>
        /// <param name="value">ダメージ量</param>
        /// <returns>ダメージに成功した場合はtrue</returns>
        bool Damage(GameObject source, float value);
    }
}