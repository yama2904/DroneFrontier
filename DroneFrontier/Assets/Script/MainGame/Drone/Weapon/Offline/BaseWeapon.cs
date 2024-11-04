using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Offline
{
    public abstract class BaseWeapon : MonoBehaviour
    {
        protected GameObject shooter = null;  //武器の所持者
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

        public static async UniTask<GameObject> CreateWeapon(GameObject shooter, Weapon weapon, bool isPlayer)
        {
            string addressKey = "";
            switch (weapon)
            {
                case Weapon.SHOTGUN:
                    addressKey = isPlayer ? "ShotgunWeapon" : "CPUShotgun";
                    break;

                case Weapon.GATLING:
                    addressKey = "GatlingWeapon";
                    break;

                case Weapon.MISSILE:
                    addressKey = isPlayer ? "MissileWeapon" : "CPUMissileWeapon";
                    break;

                case Weapon.LASER:
                    addressKey = isPlayer ? "LaserWeapon" : "CPULaserWeapon";
                    break;

                default:
                    // エラー
                    Application.Quit();
                    break;
            }

            // オブジェクトをロードして複製
            var handle = Addressables.LoadAssetAsync<GameObject>(addressKey);
            await handle;
            GameObject o = Instantiate(handle.Result);

            // 破棄
            Addressables.Release(handle);

            o.GetComponent<IWeapon>().Owner = shooter;

            return o;
        }
    }
}