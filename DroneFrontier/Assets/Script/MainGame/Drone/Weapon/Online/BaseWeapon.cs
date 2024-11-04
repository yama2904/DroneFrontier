using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace Online
{
    public abstract class BaseWeapon : NetworkBehaviour
    {
        protected BattleDrone shooter = null;      //武器の所持者
        [SerializeField] protected Transform weaponLocalPos = null;
        [SerializeField] protected Transform shotPos = null;
        [SyncVar, HideInInspector] public uint parentNetId = 0;


        public override void OnStartClient()
        {
            base.OnStartClient();
            GameObject parent = NetworkIdentity.spawned[parentNetId].gameObject;
            transform.SetParent(parent.transform);
            transform.localPosition = weaponLocalPos.localPosition;
            transform.localRotation = weaponLocalPos.localRotation;
        }
        public abstract void Init();
        public abstract void UpdateMe();
        public abstract void Shot(GameObject target = null);


        public enum Weapon
        {
            SHOTGUN,
            GATLING,
            MISSILE,
            LASER,

            NONE
        }

        public static BaseWeapon CreateWeapon(GameObject shooter, Weapon weapon)
        {
            const string FOLDER_PATH = "Weapon/Online/";
            GameObject o = null;
            if (weapon == Weapon.SHOTGUN)
            {
                //ResourcesフォルダからShotgunオブジェクトを複製してロード
                o = Instantiate(Resources.Load(FOLDER_PATH + "Shotgun_Online")) as GameObject;
            }
            else if (weapon == Weapon.GATLING)
            {
                //ResourcesフォルダからGatlingオブジェクトを複製してロード
                o = Instantiate(Resources.Load(FOLDER_PATH + "Gatling_Online")) as GameObject;
            }
            else if (weapon == Weapon.MISSILE)
            {
                //ResourcesフォルダからMissileShotオブジェクトを複製してロード
                o = Instantiate(Resources.Load(FOLDER_PATH + "MissileWeapon_Online")) as GameObject;
            }
            else if (weapon == Weapon.LASER)
            {
                //ResourcesフォルダからLaserオブジェクトを複製してロード
                o = Instantiate(Resources.Load(FOLDER_PATH + "LaserWeapon_Online")) as GameObject;
            }
            else
            {
                //エラー
                Application.Quit();
            }
            BaseWeapon bw = o.GetComponent<BaseWeapon>();
            bw.shooter = shooter.GetComponent<BattleDrone>();
            return bw;
        }
    }
}