using Drone.Battle;
using UnityEngine.AddressableAssets;

namespace Battle.Weapon
{
    public class WeaponCreater
    {
        public static IWeapon CreateWeapon(WeaponType weapon)
        {
            // ����I�u�W�F�N�g�ǂݍ���
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

            // ����I�u�W�F�N�g�ǂݍ���
            return Addressables.InstantiateAsync(addressKey).WaitForCompletion().GetComponent<IWeapon>();
        }
    }
}