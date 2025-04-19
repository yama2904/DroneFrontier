using Mono.CecilX;
using System;

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
    /// サブ武器
    /// </summary>
    public WeaponType SubWeapon { get; }

    /// <summary>
    /// 現在のストック数
    /// </summary>
    public int StockNum { get; }

    /// <summary>
    /// 初期化
    /// </summary>
    /// <param name="name">ドローン名</param>
    /// <param name="subWeapon">サブウェポン</param>
    /// <param name="stock">ストック数</param>
    public void Initialize(string name, WeaponType subWeapon, int stock);

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