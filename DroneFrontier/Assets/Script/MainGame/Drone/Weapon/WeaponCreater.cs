using UnityEngine;
using UnityEngine.AddressableAssets;

public class WeaponCreater
{
    /// <summary>
    /// �K�g�����O��AddressKey
    /// </summary>
    private const string GATLING_ADDRESS_KEY = "GatlingWeapon";

    /// <summary>
    /// �V���b�g�K����AddressKey
    /// </summary>
    private const string SHOTGUN_ADDRESS_KEY = "ShotgunWeapon";

    /// <summary>
    /// �~�T�C����AddressKey
    /// </summary>
    private const string MISSILE_ADDRESS_KEY = "MissileWeapon";

    /// <summary>
    /// ���[�U�[��AddressKey
    /// </summary>
    private const string LASER_ADDRESS_KEY = "LaserWeapon";

    public static GameObject CreateWeapon(WeaponType weapon)
    {
        // ����I�u�W�F�N�g�ǂݍ���
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

        // ����I�u�W�F�N�g�ǂݍ���
        var handle = Addressables.LoadAssetAsync<GameObject>(addressKey);
        GameObject prefab = handle.WaitForCompletion();
        GameObject obj = Object.Instantiate(prefab);
        Addressables.Release(handle);

        // �ǂݍ��񂾕���I�u�W�F�N�g�ԋp
        return obj;
    }
}
