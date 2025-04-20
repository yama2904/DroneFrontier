using Drone.Battle;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class WeaponCreater
{
    /// <summary>
    /// ガトリングのAddressKey
    /// </summary>
    private const string GATLING_ADDRESS_KEY = "GatlingWeapon";

    /// <summary>
    /// ショットガンのAddressKey
    /// </summary>
    private const string SHOTGUN_ADDRESS_KEY = "ShotgunWeapon";

    /// <summary>
    /// ミサイルのAddressKey
    /// </summary>
    private const string MISSILE_ADDRESS_KEY = "MissileWeapon";

    /// <summary>
    /// レーザーのAddressKey
    /// </summary>
    private const string LASER_ADDRESS_KEY = "LaserWeapon";

    public static IWeapon CreateWeapon(WeaponType weapon)
    {
        // 武器オブジェクト読み込み
        string addressKey = "";
        switch (weapon)
        {
            case WeaponType.GATLING:
                addressKey = GATLING_ADDRESS_KEY;
                break;

            case WeaponType.SHOTGUN:
                addressKey = SHOTGUN_ADDRESS_KEY;
                break;

            case WeaponType.MISSILE:
                addressKey = MISSILE_ADDRESS_KEY;
                break;

            case WeaponType.LASER:
                addressKey = LASER_ADDRESS_KEY;
                break;
        }

        // 武器オブジェクト読み込み
        return Addressables.InstantiateAsync(addressKey).WaitForCompletion().GetComponent<IWeapon>();
    }
}
