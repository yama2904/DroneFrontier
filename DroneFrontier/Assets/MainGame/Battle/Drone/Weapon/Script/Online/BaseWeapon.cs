﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace Online
{
    public abstract class BaseWeapon : NetworkBehaviour
    {
        [SerializeField] protected Transform weaponLocalPos = null;
        [SerializeField] protected Transform shotPos = null;

        [SyncVar] protected GameObject shooter = null;      //武器の所持者
        protected float RecastCountTime { get; set; } = 0;   //リキャスト時間をカウントする変数
        protected float ShotCountTime { get; set; } = 0;     //1発ごとの間隔をカウントする変数
        protected float BulletPower { get; set; } = -1;      //弾丸の威力

        [SyncVar, HideInInspector] public uint parentNetId = 0;

        //プロパティ用
        float recast = 0;
        float shotInterval = 0;
        int maxBullets = 0;
        int bulletsRemain = 0;

        //リキャスト時間
        protected float Recast
        {
            get
            {
                return recast;
            }
            set
            {
                if (value >= 0) recast = value;
            }
        }

        //1発ごとの間隔
        protected float ShotInterval
        {
            get
            {
                return shotInterval;
            }
            set
            {
                if (value >= 0) shotInterval = value;
            }
        }

        //最大弾数
        protected int MaxBullets
        {
            get
            {
                return maxBullets;
            }
            set
            {
                if (value >= 0) maxBullets = value;

            }
        }

        //残り弾数
        protected int BulletsRemain
        {
            get
            {
                return bulletsRemain;
            }
            set
            {
                if (value >= 0) bulletsRemain = value;
            }
        }


        public override void OnStartClient()
        {
            base.OnStartClient();
            GameObject parent = NetworkIdentity.spawned[parentNetId].gameObject;
            transform.SetParent(parent.transform);
            transform.localPosition = weaponLocalPos.localPosition;
            transform.localRotation = weaponLocalPos.localRotation;
        }

        void Awake() { }
        protected abstract void Start();

        //リキャスト時間と発射間隔を管理する
        protected virtual void Update()
        {
            RecastCountTime += Time.deltaTime;
            if (RecastCountTime > recast)
            {
                RecastCountTime = recast;
            }

            ShotCountTime += Time.deltaTime;
            if (ShotCountTime > shotInterval)
            {
                ShotCountTime = shotInterval;
            }
        }

        public abstract void Init();
        public abstract void UpdateMe();
        public abstract void ResetWeapon();
        public abstract void Shot(GameObject target = null);

        public virtual void SetChild(Transform parent)
        {
            //Transform t = transform;
            //t.SetParent(parent);
            //t.localPosition = weaponLocalPos.localPosition;
            //t.localRotation = weaponLocalPos.localRotation;
        }


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
            bw.shooter = shooter;
            return bw;
        }
    }
}