using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Offline
{
    public abstract class BaseWeapon : MonoBehaviour
    {
        protected BattleDrone shooter = null;  //武器の所持者
        [SerializeField] protected Transform weaponLocalPos = null;
        [SerializeField] protected Transform shotPos = null;


        public abstract void Shot(GameObject target = null);
        public virtual void SetParent(Transform parent)
        {
            Transform t = transform;
            t.SetParent(parent);
            t.localPosition = weaponLocalPos.localPosition;
            t.localRotation = weaponLocalPos.localRotation;
        }


        public enum Weapon
        {
            SHOTGUN,
            GATLING,
            MISSILE,
            LASER,

            NONE
        }

        public static BaseWeapon CreateWeapon(BattleDrone shooter, Weapon weapon)
        {
            const string FOLDER_PATH = "Weapon/Offline/";
            GameObject o = null;
            if (weapon == Weapon.SHOTGUN)
            {
                //ResourcesフォルダからShotgunオブジェクトを複製してロード
                o = Instantiate(Resources.Load(FOLDER_PATH + "Shotgun_Offline")) as GameObject;
            }
            else if (weapon == Weapon.GATLING)
            {
                //ResourcesフォルダからGatlingオブジェクトを複製してロード
                o = Instantiate(Resources.Load(FOLDER_PATH + "Gatling_Offline")) as GameObject;
            }
            else if (weapon == Weapon.MISSILE)
            {
                //ResourcesフォルダからMissileShotオブジェクトを複製してロード
                o = Instantiate(Resources.Load(FOLDER_PATH + "MissileWeapon_Offline")) as GameObject;
            }
            else if (weapon == Weapon.LASER)
            {
                //ResourcesフォルダからLaserオブジェクトを複製してロード
                o = Instantiate(Resources.Load(FOLDER_PATH + "LaserWeapon_Offline")) as GameObject;
            }
            else
            {
                //エラー
                Application.Quit();
            }

            BaseWeapon bw = o.GetComponent<BaseWeapon>();
            bw.shooter = shooter;
            return bw;
        }
    }
}