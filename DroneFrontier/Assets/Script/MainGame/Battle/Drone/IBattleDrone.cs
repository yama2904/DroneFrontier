using Offline;
using System;
using UnityEngine;

/// <summary>
/// バトルモードのドローンを実装するインターフェース
/// </summary>
public interface IBattleDrone
{
    /// <summary>
    /// ドローンのゲームオブジェクト
    /// </summary>
    public GameObject GameObject { get; }

    // <summary>
    /// ドローンの名前
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// ドローンのHP
    /// </summary>
    public float HP { get; set; }

    /// <summary>
    /// 現在のストック数
    /// </summary>
    public int StockNum { get; set; }

    /// <summary>
    /// サブ武器
    /// </summary>
    public BaseWeapon.Weapon SubWeapon { get; set; }

    /// <summary>
    /// ドローン破壊イベント
    /// </summary>
    public event EventHandler DroneDestroyEvent;
}