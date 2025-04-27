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

            // ����I�u�W�F�N�g�ǂݍ���
            return Addressables.InstantiateAsync(addressKey).WaitForCompletion().GetComponent<IWeapon>();
        }
    }
}