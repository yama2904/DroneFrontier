using System;

/// <summary>
/// バトルモードのドローンを実装するインターフェース
/// </summary>
public interface IBattleDrone
{
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
    public WeaponType SubWeapon { get; set; }

    /// <summary>
    /// 初期化
    /// </summary>
    public void Initialize();

    /// <summary>
    /// ドローン破壊イベント
    /// </summary>
    public event EventHandler DroneDestroyEvent;
}