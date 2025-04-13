using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 実装したオブジェクトをレーダー照射可能とするインターフェース
/// </summary>
public interface IRadarable
{
    /// <summary>
    /// オブジェクトタイプ
    /// </summary>
    public enum ObjectType
    {
        /// <summary>
        /// 敵
        /// </summary>
        Enemy,

        /// <summary>
        /// アイテム
        /// </summary>
        Item
    }

    /// <summary>
    /// レーダー照射された際に返すオブジェクトタイプ
    /// </summary>
    public ObjectType Type { get; }

    /// <summary>
    /// レーダー照射可能であるか
    /// </summary>
    public bool IsRadarable { get; }

    /// <summary>
    /// レーダー照射不可オブジェクト
    /// </summary>
    public List<GameObject> NotRadarableList { get; }
}