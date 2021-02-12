using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

namespace Online
{
    public class MissieWeapon : BaseWeapon
    {
        [SerializeField] MissileBullet missile = null;  //複製する弾丸
        SyncList<GameObject> settingBullets = new SyncList<GameObject>();
        const int USE_INDEX = 0;
        bool setMissile = false;

        //弾丸のパラメータ
        [SerializeField, Tooltip("威力")] float power = 40f;
        [SerializeField, Tooltip("リキャスト時間")] float recast = 10f;
        [SerializeField, Tooltip("1秒間に発射する弾数")] float shotPerSecond = 0.2f;
        [SerializeField, Tooltip("1秒間に進む距離")] float speed = 500f;
        [SerializeField, Tooltip("射程")] float destroyTime = 2.0f;
        [SerializeField, Tooltip("誘導力")] float trackingPower = 2.3f;
        [SerializeField, Tooltip("ストック可能な弾数")] int maxBulletNum = 3;
        float shotInterval = 0;     //発射間隔
        float shotTimeCount = 0;    //時間計測用
        float recastTimeCount = 0;  //時間計測用
        int haveBulletNum = 0;      //残り弾数


        //所持弾数のUI用
        const float UI_POS_DIFF_X = 1.5f;
        const float UI_POS_Y = 175f;
        [SerializeField] Canvas UIParentCanvas = null;
        [SerializeField] Image bulletUIBack = null;
        [SerializeField] Image bulletUIFront = null;
        Image[] UIs;


        public override void OnStartClient()
        {
            base.OnStartClient();

            //パラメータの初期化
            shotInterval = 1f / shotPerSecond;
            shotTimeCount = shotInterval;
            haveBulletNum = maxBulletNum;
        }

        public override void Init()
        {
            CmdCreateMissile();
            setMissile = true;

            //所持弾数のUI作成
            UIs = new Image[maxBulletNum];
            for (int i = 0; i < maxBulletNum; i++)
            {
                //bulletUIBackの生成
                RectTransform back = Instantiate(bulletUIBack).GetComponent<RectTransform>();
                back.SetParent(UIParentCanvas.transform);
                back.anchoredPosition = new Vector2((back.sizeDelta.x * i * UI_POS_DIFF_X) + back.sizeDelta.x, UI_POS_Y);

                //bulletUIFrontの生成
                RectTransform front = Instantiate(bulletUIFront).GetComponent<RectTransform>();
                front.SetParent(UIParentCanvas.transform);
                front.anchoredPosition = new Vector2((front.sizeDelta.x * i * UI_POS_DIFF_X) + front.sizeDelta.x, UI_POS_Y);

                //配列に追加
                UIs[i] = front.GetComponent<Image>();
                UIs[i].fillAmount = 1f;
            }
        }

        public override void UpdateMe()
        {
            //発射間隔のカウント
            if (!setMissile)
            {
                shotTimeCount += Time.deltaTime;
                if (shotTimeCount > shotInterval)
                {
                    shotTimeCount = shotInterval;
                    if (haveBulletNum > 0)  //弾丸が残っていない場合は処理しない
                    {
                        CmdCreateMissile();
                        setMissile = true;


                        //デバッグ用
                        Debug.Log("ミサイル発射可能");
                    }
                }
            }

            //リキャスト時間経過したら弾数を1個補充
            if (haveBulletNum < maxBulletNum)     //最大弾数持っていたら処理しない
            {
                recastTimeCount += Time.deltaTime;
                if (recastTimeCount >= recast)
                {
                    UIs[haveBulletNum].fillAmount = 1f;
                    haveBulletNum++;        //弾数を回復
                    recastTimeCount = 0;    //リキャストのカウントをリセット


                    //デバッグ用
                    Debug.Log("ミサイルの弾丸が1回分補充されました");
                }
                else
                {
                    UIs[haveBulletNum].fillAmount = recastTimeCount / recast;
                }
            }
        }

        [Command]
        void CmdDestroyMissile()
        {
            NetworkServer.Destroy(settingBullets[USE_INDEX]);
        }

        #region CreateMissile

        MissileBullet CreateMissile()
        {
            MissileBullet m = Instantiate(missile);    //ミサイルの複製
            m.parentNetId = netId;
            
            return m;
        }

        [Command(ignoreAuthority = true)]
        void CmdCreateMissile()
        {
            MissileBullet m = CreateMissile();
            NetworkServer.Spawn(m.gameObject, connectionToClient);

            settingBullets.Add(m.gameObject);
        }

        #endregion

        public override void Shot(GameObject target = null)
        {
            //前回発射して発射間隔分の時間が経過していなかったら撃たない
            if (shotTimeCount < shotInterval) return;

            //バグ防止
            if (!setMissile) return;
            if (settingBullets.Count <= 0) return;

            //残り弾数が0だったら撃たない
            if (haveBulletNum <= 0) return;


            //ミサイル発射
            CmdShot(target);
            setMissile = false;


            //所持弾丸のUIを灰色に変える
            for (int i = haveBulletNum - 1; i < maxBulletNum; i++)
            {
                UIs[i].fillAmount = 0;
            }

            //弾数を減らしてリキャスト開始
            if (haveBulletNum == maxBulletNum)
            {
                recastTimeCount = 0;
            }
            haveBulletNum--;    //残り弾数を減らす
            shotTimeCount = 0;  //発射間隔のカウントをリセット


            //デバッグ用
            Debug.Log("ミサイル発射 残り弾数: " + haveBulletNum);
        }

        [Command(ignoreAuthority = true)]
        void CmdShot(GameObject target)
        {
            MissileBullet m = settingBullets[USE_INDEX].GetComponent<MissileBullet>();
            m.Init(shooter.netId, power, trackingPower, speed, destroyTime, target);
            m.Shot(target);

            settingBullets.RemoveAt(USE_INDEX);
        }
    }
}