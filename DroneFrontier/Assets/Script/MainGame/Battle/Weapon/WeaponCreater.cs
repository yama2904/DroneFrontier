using Drone.Battle;
using Drone.Battle.Network;
using UnityEngine.AddressableAssets;

public class WeaponCreater
{
    public static IWeapon CreateWeapon(WeaponType weapon)
    {
        // 武器オブジェクト読み込み
        string addressKey = "";
        switch (weapon)
        {
            case WeaponType.GATLING:
                addressKey = GatlingWeapon.ADDRESS_KEY;
                break;

            case WeaponType.SHOTGUN:
                addressKey = ShotgunWeapon.ADDRESS_KEY;
                break;

            case WeaponType.MISSILE:
                addressKey = MissileWeapon.ADDRESS_KEY;
                break;

            case WeaponType.LASER:
                addressKey = LaserWeapon.ADDRESS_KEY;
                break;
        }

        // 武器オブジェクト読み込み
        return Addressables.InstantiateAsync(addressKey).WaitForCompletion().GetComponent<IWeapon>();
    }
}
