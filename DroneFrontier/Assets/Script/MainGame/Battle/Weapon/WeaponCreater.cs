using Drone.Battle;
using UnityEngine.AddressableAssets;

namespace Battle.Weapon
{
    public class WeaponCreater
    {
        public static IWeapon CreateWeapon(WeaponType weapon)
        {
            // 武器オブジェクト読み込み
            string addressKey = "";
            switch (weapon)
            {
                case WeaponType.Gatling:
                    addressKey = GatlingWeapon.ADDRESS_KEY;
                    break;

                case WeaponType.Shotgun:
                    addressKey = ShotgunWeapon.ADDRESS_KEY;
                    break;

                case WeaponType.Missile:
                    addressKey = MissileWeapon.ADDRESS_KEY;
                    break;

                case WeaponType.Lazer:
                    addressKey = LaserWeapon.ADDRESS_KEY;
                    break;
            }

            // 武器オブジェクト読み込み
            return Addressables.InstantiateAsync(addressKey).WaitForCompletion().GetComponent<IWeapon>();
        }
    }
}