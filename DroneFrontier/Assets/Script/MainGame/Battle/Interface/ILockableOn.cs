using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 実装したオブジェクトをロックオン可能とするインターフェース
/// </summary>
public interface ILockableOn
{
    /// <summary>
    /// ロックオン可能であるか
    /// </summary>
    public bool IsLockableOn { get; }

    /// <summary>
    /// ロックオン不可オブジェクト
    /// </summary>
    public List<GameObject> NotLockableOnList { get; }
}