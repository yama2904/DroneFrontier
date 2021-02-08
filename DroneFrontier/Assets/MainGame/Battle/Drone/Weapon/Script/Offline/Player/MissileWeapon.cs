using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Offline
{
    public class MissileWeapon : BaseWeapon
    {
        [SerializeField] MissileBullet missile = null;  //複製する弾丸
        List<MissileBullet> settingBullets = new List<MissileBullet>();
        const int USE_INDEX = 0;
        bool setMissile = false;

        //弾丸のパラメータ
        [SerializeField, Tooltip("威力")] protected float power = 40f;
        [SerializeField, Tooltip("リキャスト時間")] protected float recast = 10f;
        [SerializeField, Tooltip("1秒間に発射する弾数")] protected float shotPerSecond = 0.2f;
        [SerializeField, Tooltip("1秒間に進む距離")] float speed = 500f;
        [SerializeField, Tooltip("射程")] float destroyTime = 2.0f;
        [SerializeField, Tooltip("誘導力")] float trackingPower = 2.5f;
        [SerializeField, Tooltip("ストック可能な弾数")] int maxBulletNum = 3;
        float shotInterval = 0;
        float shotCountTime = 0;
        float recastCountTime = 0;
        int haveBulletNum = 0;


        //所持弾数のUI用
        const float UI_POS_DIFF_X = 1.5f;
        const float UI_POS_Y = 175f;
        [SerializeField] Canvas UIParentCanvas = null;
        [SerializeField] Image bulletUIBack = null;
        [SerializeField] Image bulletUIFront = null;
        Image[] UIs;


        void Start()
        {
            //パラメータの初期化
            shotInterval = 1f / shotPerSecond;
            shotCountTime = shotInterval;
            haveBulletNum = maxBulletNum;

            //弾丸生成
            CreateMissile();
            setMissile = true;

            //所持弾数のUI作成
            UIs = new Image[maxBulletNum];
            for (int i = 0; i < maxBulletNum; i++)
            {
                //bulletUIBackの生成
                RectTransform back = Instantiate(bulletUIBack).GetComponent<RectTransform>();
                back.SetParent(UIParentCanvas.transform);
                back.anchoredPosition3D = new Vector3((back.sizeDelta.x * i * UI_POS_DIFF_X) + back.sizeDelta.x, UI_POS_Y, 0);
                back.localRotation = Quaternion.identity;

                //bulletUIFrontの生成
                RectTransform front = Instantiate(bulletUIFront).GetComponent<RectTransform>();
                front.SetParent(UIParentCanvas.transform);
                front.anchoredPosition3D = new Vector3((front.sizeDelta.x * i * UI_POS_DIFF_X) + front.sizeDelta.x, UI_POS_Y, 0);
                front.localRotation = Quaternion.identity;

                //配列に追加
                UIs[i] = front.GetComponent<Image>();
                UIs[i].fillAmount = 1f;
            }
        }

        void Update()
        {
            //発射間隔のカウント
            if (!setMissile)
            {
                shotCountTime += Time.deltaTime;
                if (shotCountTime > shotInterval)
                {
                    shotCountTime = shotInterval;
                    if (haveBulletNum > 0)  //弾丸が残っていない場合は処理しない
                    {
                        CreateMissile();
                        setMissile = true;
                        
                        //デバッグ用
                        Debug.Log("ミサイル発射可能");
                    }
                }
            }

            //リキャスト時間経過したら弾数を1個補充
            if (haveBulletNum < maxBulletNum)     //最大弾数持っていたら処理しない
            {
                recastCountTime += Time.deltaTime;
                if (recastCountTime >= recast)
                {
                    UIs[haveBulletNum].fillAmount = 1f;
                    haveBulletNum++;        //弾数を回復
                    recastCountTime = 0;    //リキャストのカウントをリセット


                    //デバッグ用
                    Debug.Log("ミサイルの弾丸が1回分補充されました");
                }
                else
                {
                    UIs[haveBulletNum].fillAmount = recastCountTime / recast;
                }
            }
        }

        MissileBullet CreateMissile()
        {
            //ミサイルの生成
            MissileBullet m = Instantiate(missile, transform);

            //リストに追加
            settingBullets.Add(m);
            setMissile = true;

            return m;
        }

        public override void Shot(GameObject target = null)
        {
            //前回発射して発射間隔分の時間が経過していなかったら撃たない
            if (shotCountTime < shotInterval) return;

            //バグ防止
            if (!setMissile) return;
            if (settingBullets.Count <= 0) return;

            //残り弾数が0だったら撃たない
            if (haveBulletNum <= 0) return;


            //ミサイル発射
            settingBullets[USE_INDEX].Init(shooter, power, trackingPower, speed, destroyTime, target);
            settingBullets[USE_INDEX].Shot(target);
            settingBullets.RemoveAt(USE_INDEX);
            setMissile = false;


            //所持弾丸のUIを灰色に変える
            for (int i = haveBulletNum - 1; i < maxBulletNum; i++)
            {
                UIs[i].fillAmount = 0;
            }

            //弾数を減らしてリキャスト開始
            if (haveBulletNum == maxBulletNum)
            {
                recastCountTime = 0;
            }
            haveBulletNum--;    //残り弾数を減らす
            shotCountTime = 0;  //発射間隔のカウントをリセット


            //デバッグ用
            Debug.Log("ミサイル発射 残り弾数: " + haveBulletNum);
        }
    }
}