using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class BarrierStrengthenItem : IDroneItem
{
    /// <summary>
    /// ダメージ軽減率
    /// </summary>
    public float DamageDownPercent { get; set; } = 0.5f;

    /// <summary>
    /// 強化時間（秒）
    /// </summary>
    public int StrengthenSec { get; set; } = 10;

    public Image InstantiateIcon()
    {
        return Addressables.InstantiateAsync("BarrierIconImage").WaitForCompletion().GetComponent<Image>();
    }

    public bool UseItem(GameObject drone)
    {
        return drone.GetComponent<DroneStatusComponent>().AddStatus(new BarrierStrengthenStatus(), StrengthenSec, DamageDownPercent);
    }
}