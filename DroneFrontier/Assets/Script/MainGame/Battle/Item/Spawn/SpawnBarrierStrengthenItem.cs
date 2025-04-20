using Drone.Battle;
using System;
using System.Collections.Generic;
using UnityEngine;

public class SpawnBarrierStrengthenItem : MonoBehaviour, ISpawnItem, IRadarable
{
    /// <summary>
    /// スポーンアイテム消滅イベント
    /// </summary>
    public event EventHandler SpawnItemDestroyEvent;

    /// <summary>
    /// 取得時に使用可能となるアイテム
    /// </summary>
    public IDroneItem DroneItem => new BarrierStrengthenItem();

    public IRadarable.ObjectType Type => IRadarable.ObjectType.Item;

    public bool IsRadarable => true;

    public List<GameObject> NotRadarableList => new List<GameObject>();

    private void OnDestroy()
    {
        SpawnItemDestroyEvent?.Invoke(this, EventArgs.Empty);
    }
}