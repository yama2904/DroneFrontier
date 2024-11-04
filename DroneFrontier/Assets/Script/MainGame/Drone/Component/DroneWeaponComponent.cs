using System;
using UnityEngine;

public class DroneWeaponComponent : MonoBehaviour
{
    public enum Weapon
    {
        /// <summary>
        /// メイン武器
        /// </summary>
        MAIN,

        /// <summary>
        /// サブ武器
        /// </summary>
        SUB,

        NONE
    }

    [SerializeField, Tooltip("メイン武器装備位置")]
    private Transform _mainWeaponPos = null;

    [SerializeField, Tooltip("サブ武器装備位置")]
    private Transform _subWeaponPos = null;

    [SerializeField, Tooltip("メイン武器発射位置")]
    private Transform _mainShotPos = null;

    [SerializeField, Tooltip("サブ武器発射位置")]
    private Transform _subShotPos = null;

    [SerializeField, Tooltip("弾丸UI表示Canvas")]
    private Canvas _bulletUICanvs = null;

    /// <summary>
    /// メイン武器
    /// </summary>
    private IWeapon _mainWeapon = null;

    /// <summary>
    /// サブ武器
    /// </summary>
    private IWeapon _subWeapon = null;

    /// <summary>
    /// 武器を使用して弾丸発射
    /// </summary>
    /// <param name="weapon">使用する武器</param>
    /// <param name="target">追従対象</param>
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
                throw new Exception("想定外の武器が使用されました。");
        }

        // 発射
        useWeapon.Shot(target);
    }

    private void Start()
    {
        // メイン武器読み込み
        GameObject mainWeapon = WeaponCreater.CreateWeapon(WeaponType.GATLING);
        mainWeapon.transform.SetParent(_mainWeaponPos, false);
        _mainWeapon = mainWeapon.GetComponent<IWeapon>();
        _mainWeapon.Owner = gameObject;
        _mainWeapon.ShotPosition = _mainShotPos;

        // サブ武器読み込み
        GameObject subWeapon = WeaponCreater.CreateWeapon(GetComponent<IBattleDrone>().SubWeapon);
        subWeapon.transform.SetParent(_subWeaponPos, false);
        _subWeapon = subWeapon.GetComponent<IWeapon>();
        _subWeapon.Owner = gameObject;
        _subWeapon.ShotPosition = _subShotPos;
        _subWeapon.BulletUICanvas = _bulletUICanvs;
    }
}
