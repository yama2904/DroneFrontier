using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Offline
{
    public abstract class BaseWeapon : MonoBehaviour
    {
        protected IBattleDrone shooter = null;  //武器の所持者
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

        public static async UniTask<BaseWeapon> CreateWeapon(IBattleDrone shooter, Weapon weapon, bool isPlayer)
        {
            string addressKey = "";
            switch (weapon)
            {
                case Weapon.SHOTGUN:
                    addressKey = isPlayer ? "Shotgun" : "CPUShotgun";
                    break;

                case Weapon.GATLING:
                    addressKey = "Gatling";
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
            BaseWeapon bw = Instantiate(handle.Result).GetComponent<BaseWeapon>();

            // 破棄
            Addressables.Release(handle);

            bw.shooter = shooter;

            return bw;
        }
    }
}