using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Mirror;

namespace Online
{
    public class LaserWeapon : BaseWeapon
    {
        const float SHOT_POSSIBLE_MIN = 0.2f;  //発射可能な最低ゲージ量
        [SerializeField] LaserBullet laserBullet = null;
        [SyncVar] GameObject createdBullet = null;
        [SerializeField, Tooltip("威力")] protected float power = 5f;
        [SerializeField, Tooltip("リキャスト時間")] float recast = 8f;
        [SerializeField, Tooltip("何秒発射できるか")] float maxShotTime = 10;
        [SerializeField, Tooltip("レーザーのサイズ(Scaleの代わり")] float size = 10f;
        [SerializeField, Tooltip("チャージ時間")] float chargeTime = 2.0f;
        [SerializeField, Tooltip("レーザーの射程")] float lineRange = 175f;
        [SerializeField, Tooltip("1秒間にヒットする回数")] float hitPerSecond = 6.0f;

        [SerializeField] Image laserGaugeImage = null;
        [SerializeField] Image laserGaugeFrameImage = null;

        //攻撃中のフラグ
        enum ShotFlag
        {
            SHOT_START,     //攻撃を始めたらtrue
            SHOT_SHOTING,   //攻撃中は常に更新させる

            NONE
        }
        List<bool> isShots = new List<bool>();


        #region Init

        public override void Init()
        {
            for (int i = 0; i < (int)ShotFlag.NONE; i++)
            {
                isShots.Add(false);
            }
            CmdInit();
            laserGaugeImage.enabled = true;
            laserGaugeFrameImage.enabled = true;
            laserGaugeImage.fillAmount = 1.0f;
        }

        [Command(ignoreAuthority = true)]
        void CmdInit()
        {
            LaserBullet lb = Instantiate(laserBullet);
            lb.parentNetId = netId;
            lb.localPos = shotPos.localPosition;
            lb.localRot = shotPos.localRotation;

            NetworkServer.Spawn(lb.gameObject, connectionToClient);
            lb.RpcInit(shooter, power, size, chargeTime, lineRange, hitPerSecond);
            lb.TargetSetIsLocalTrue(connectionToClient);
            createdBullet = lb.gameObject;
        }

        #endregion

        #region Update

        public override void UpdateMe()
        {
            if (isShots.Count <= 0) return;

            //撃っていない間はリキャストの管理
            if (!isShots[(int)ShotFlag.SHOT_START])
            {
                //処理が無駄なのでゲージがMAXならスキップ
                if (laserGaugeImage.fillAmount < 1.0f)
                {
                    //ゲージを回復
                    laserGaugeImage.fillAmount += 1.0f / recast * Time.deltaTime;
                    if (laserGaugeImage.fillAmount > 1.0f)
                    {
                        laserGaugeImage.fillAmount = 1.0f;


                        //デバッグ用
                        Debug.Log("ゲージMAX");
                    }
                }
            }
        }

        void LateUpdate()
        {
            if (isShots.Count <= 0) return;

            //攻撃中かどうか
            if (isShots[(int)ShotFlag.SHOT_START])
            {
                //攻撃中は常にSHOT_SHOTINGの更新をかける
                if (isShots[(int)ShotFlag.SHOT_SHOTING])
                {
                    isShots[(int)ShotFlag.SHOT_SHOTING] = false;
                }
                //フラグの更新が止まっていたら攻撃をストップ
                else
                {
                    createdBullet.GetComponent<LaserBullet>().StopShot();

                    isShots[(int)ShotFlag.SHOT_START] = false;
                    isShots[(int)ShotFlag.SHOT_SHOTING] = false;
                }
            }
        }

        #endregion


        public override void Shot(GameObject target = null)
        {
            if (isShots.Count <= 0) return;

            //発射に必要な最低限のゲージがないと発射しない
            if (!isShots[(int)ShotFlag.SHOT_START])
            {
                if (laserGaugeImage.fillAmount < SHOT_POSSIBLE_MIN)
                {
                    return;
                }
                isShots[(int)ShotFlag.SHOT_START] = true;
            }

            LaserBullet lb = createdBullet.GetComponent<LaserBullet>();
            lb.Shot(target);
            isShots[(int)ShotFlag.SHOT_SHOTING] = true;

            //撃っている間はゲージを減らす
            if (lb.IsShotBeam)
            {
                //ゲージを減らす
                laserGaugeImage.fillAmount -= 1.0f / maxShotTime * Time.deltaTime;
                if (laserGaugeImage.fillAmount <= 0)    //ゲージがなくなったらレーザーを止める
                {
                    laserGaugeImage.fillAmount = 0;
                    isShots[(int)ShotFlag.SHOT_SHOTING] = false;
                }
            }
        }
    }
}