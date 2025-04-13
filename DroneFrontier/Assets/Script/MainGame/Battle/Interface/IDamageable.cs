using UnityEngine;

/// <summary>
/// 実装したオブジェクトをダメージ可能とするインターフェース
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// ダメージを与えないオブジェクト
    /// </summary>
    GameObject NoDamageObject { get; }

    /// <summary>
    /// 実装したオブジェクトにダメージを与える
    /// </summary>
    /// <param name="source">ダメージ元オブジェクト</param>
    /// <param name="value">ダメージ量</param>
    void Damage(GameObject source, float value);
}
