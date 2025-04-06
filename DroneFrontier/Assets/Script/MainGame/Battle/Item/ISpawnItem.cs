using System;
using UnityEngine.UI;

public interface ISpawnItem
{
    /// <summary>
    /// スポーンアイテム消滅イベント
    /// </summary>
    public event EventHandler SpawnItemDestroyEvent;

    /// <summary>
    /// 取得時に使用可能となるアイテム
    /// </summary>
    public IDroneItem DroneItem { get; }

    /// <summary>
    /// アイテムのアイコン生成
    /// </summary>
    /// <returns></returns>
    public Image InstantiateIcon();
}
