using System;
using UnityEngine;

public class DroneWeaponComponent : MonoBehaviour
{
    public enum Weapon
    {
        /// <summary>
        /// ���C������
        /// </summary>
        MAIN,

        /// <summary>
        /// �T�u����
        /// </summary>
        SUB,

        NONE
    }

    [SerializeField, Tooltip("���C�����푕���ʒu")]
    private Transform _mainWeaponPos = null;

    [SerializeField, Tooltip("�T�u���푕���ʒu")]
    private Transform _subWeaponPos = null;

    [SerializeField, Tooltip("���C�����픭�ˈʒu")]
    private Transform _mainShotPos = null;

    [SerializeField, Tooltip("�T�u���픭�ˈʒu")]
    private Transform _subShotPos = null;

    [SerializeField, Tooltip("�e��UI�\��Canvas")]
    private Canvas _bulletUICanvs = null;

    /// <summary>
    /// ���C������
    /// </summary>
    private IWeapon _mainWeapon = null;

    /// <summary>
    /// �T�u����
    /// </summary>
    private IWeapon _subWeapon = null;

    /// <summary>
    /// ������g�p���Ēe�۔���
    /// </summary>
    /// <param name="weapon">�g�p���镐��</param>
    /// <param name="target">�Ǐ]�Ώ�</param>
    public void Shot(Weapon weapon, GameObject target = null)
    {
        IWeapon useWeapon;
        switch (weapon)
        {
            case Weapon.MAIN:
                useWeapon = _mainWeapon;
                break;

            case Weapon.SUB:
                useWeapon = _subWeapon;
                break;

            default:
                throw new Exception("�z��O�̕��킪�g�p����܂����B");
        }

        // ����
        useWeapon.Shot(target);
    }

    private void Start()
    {
        // ���C������ǂݍ���
        GameObject mainWeapon = WeaponCreater.CreateWeapon(WeaponType.GATLING);
        mainWeapon.transform.SetParent(_mainWeaponPos, false);
        _mainWeapon = mainWeapon.GetComponent<IWeapon>();
        _mainWeapon.Owner = gameObject;
        _mainWeapon.ShotPosition = _mainShotPos;

        // �T�u����ǂݍ���
        GameObject subWeapon = WeaponCreater.CreateWeapon(GetComponent<IBattleDrone>().SubWeapon);
        subWeapon.transform.SetParent(_subWeaponPos, false);
        _subWeapon = subWeapon.GetComponent<IWeapon>();
        _subWeapon.Owner = gameObject;
        _subWeapon.ShotPosition = _subShotPos;
        _subWeapon.BulletUICanvas = _bulletUICanvs;
    }
}
