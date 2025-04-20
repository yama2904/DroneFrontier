using Drone.Battle;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class StunGrenadeItem : IDroneItem
{
    /// <summary>
    /// 投擲速度
    /// </summary>
    public float ThrowSpeed { get; set; } = 550f;

    /// <summary>
    /// 着弾時間（秒）
    /// </summary>
    public float ImpactSec { get; set; } = 1.0f;

    /// <summary>
    /// 投擲時の重さ
    /// </summary>
    public float Weight { get; set; } = 450f;

    /// <summary>
    /// スタン状態の時間（秒）
    /// </summary>
    public float StunSec { get; set; } = 9.0f;

    public Image InstantiateIcon()
    {
        return Addressables.InstantiateAsync("GrenadeIconImage").WaitForCompletion().GetComponent<Image>();
    }

    public bool UseItem(GameObject drone)
    {
        // ドローンの座標と向きでスタングレネードを生成
        Transform _throwerPos = drone.transform;
        StunGrenade grenade = Addressables.InstantiateAsync("StunGrenade", _throwerPos.position, _throwerPos.rotation).WaitForCompletion().GetComponent<StunGrenade>();

        // 投てき処理
        grenade.ThrowGrenade(drone, ThrowSpeed, ImpactSec, Weight, StunSec);

        return true;
    }
}