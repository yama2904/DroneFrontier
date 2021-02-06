using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Offline
{
    namespace CPU
    {
        public class BattleDrone : BaseDrone
        {
            //コンポーネント用
            Transform cacheTransform = null;
            Rigidbody _rigidbody = null;
            Animator animator = null;
            DroneBaseAction baseAction = null;

            //武器
            protected enum Weapon
            {
                MAIN,   //メイン武器
                SUB,    //サブ武器

                NONE
            }
            BaseWeapon mainWeapon = null;
            BaseWeapon subWeapon = null;
            public BaseWeapon.Weapon setSubWeapon = BaseWeapon.Weapon.SHOTGUN;
            bool[] usingWeapons = new bool[(int)Weapon.NONE];    //使用中の武器
            [SerializeField, Tooltip("攻撃中の移動速度の低下率")] float atackingDownSpeed = 0.5f;   //攻撃中の移動速度の低下率

            [Header("デバッグ用")]
            [SerializeField] Weapon isAtack = Weapon.NONE;


            protected override void Awake()
            {
                base.Awake();

                //コンポーネントの取得
                cacheTransform = transform;
                _rigidbody = GetComponent<Rigidbody>();
                animator = GetComponent<Animator>();
                baseAction = GetComponent<DroneBaseAction>();
            }

            protected override void Start()
            {
                base.Start();

                //武器初期化
                mainWeapon = BaseWeapon.CreateWeapon(this, BaseWeapon.Weapon.GATLING, false);
                mainWeapon.SetParent(transform);
                subWeapon = BaseWeapon.CreateWeapon(this, setSubWeapon, false);
                subWeapon.SetParent(transform);
            }

            void Update()
            {
                if (!MainGameManager.Singleton.StartFlag) return;  //ゲーム開始フラグが立っていなかったら処理しない

                if(isAtack == Weapon.MAIN)
                {
                    mainWeapon.Shot();
                }
                else if(isAtack == Weapon.SUB)
                {
                    subWeapon.Shot();
                }
            }
        }
    }
}