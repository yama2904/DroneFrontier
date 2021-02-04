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
        [SerializeField, Tooltip("1秒間に進む距離")] float speedPerSecond = 500f;
        [SerializeField, Tooltip("射程")] float destroyTime = 2.0f;
        [SerializeField, Tooltip("誘導力")] float trackingPower = 2.5f;
        [SerializeField, Tooltip("1秒間に発射する弾数")] float shotPerSecond = 0.2f;

        [SerializeField, Tooltip("リキャスト時間")] float _recast = 10f;
        [SerializeField, Tooltip("ストック可能な弾数")] int _maxBullets = 3;
        [SerializeField, Tooltip("威力")] float _power = 40f;


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
            Recast = _recast;
            ShotInterval = 1.0f / shotPerSecond;
            ShotCountTime = ShotInterval;
            MaxBullets = _maxBullets;
            BulletsRemain = MaxBullets;
            BulletPower = _power;

            //弾丸生成
            CreateMissile();
            setMissile = true;

            //所持弾数のUI作成
            UIs = new Image[_maxBullets];
            for (int i = 0; i < _maxBullets; i++)
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

        protected override void Update()
        {
            base.Update();

            //発射間隔のカウント
            if (!setMissile)
            {
                ShotCountTime += Time.deltaTime;
                if (ShotCountTime > ShotInterval)
                {
                    ShotCountTime = ShotInterval;
                    if (BulletsRemain > 0)  //弾丸が残っていない場合は処理しない
                    {
                        CreateMissile();
                        setMissile = true;


                        //デバッグ用
                        Debug.Log("ミサイル発射可能");
                    }
                }
            }

            //リキャスト時間経過したら弾数を1個補充
            if (BulletsRemain < MaxBullets)     //最大弾数持っていたら処理しない
            {
                RecastCountTime += Time.deltaTime;
                if (RecastCountTime >= Recast)
                {
                    UIs[BulletsRemain].fillAmount = 1f;
                    BulletsRemain++;        //弾数を回復
                    RecastCountTime = 0;    //リキャストのカウントをリセット


                    //デバッグ用
                    Debug.Log("ミサイルの弾丸が1回分補充されました");
                }
                else
                {
                    UIs[BulletsRemain].fillAmount = RecastCountTime / Recast;
                }
            }
        }

        public override void ResetWeapon()
        {
            RecastCountTime = 0;
            ShotCountTime = ShotInterval;
            BulletsRemain = MaxBullets;

            //弾数UIのリセット
            for (int i = 0; i < UIs.Length; i++)
            {
                UIs[i].fillAmount = 1f;
            }

            //既にある弾丸の削除と新しい弾丸の生成
            if (setMissile)
            {
                Destroy(settingBullets[USE_INDEX]);
            }
            CreateMissile();
            
        }

        MissileBullet CreateMissile()
        {
            MissileBullet m = Instantiate(missile, transform);    //ミサイルの複製

            //弾丸のパラメータ設定
            m.Shooter = shooter;    //撃ったプレイヤーを登録
            m.SpeedPerSecond = speedPerSecond;  //スピード
            m.DestroyTime = destroyTime;        //射程
            m.TrackingPower = trackingPower;    //誘導力
            m.Power = BulletPower;              //威力
            
            //リストに追加
            settingBullets.Add(m);
            setMissile = true;

            return m;
        }

        public override void Shot(GameObject target = null)
        {
            //前回発射して発射間隔分の時間が経過していなかったら撃たない
            if (ShotCountTime < ShotInterval) return;

            //バグ防止
            if (!setMissile) return;
            if (settingBullets.Count <= 0) return;

            //残り弾数が0だったら撃たない
            if (BulletsRemain <= 0) return;


            //ミサイル発射
            settingBullets[USE_INDEX].Shot(target);
            settingBullets.RemoveAt(USE_INDEX);
            setMissile = false;


            //所持弾丸のUIを灰色に変える
            for (int i = BulletsRemain - 1; i < MaxBullets; i++)
            {
                UIs[i].fillAmount = 0;
            }

            //弾数を減らしてリキャスト開始
            if (BulletsRemain == MaxBullets)
            {
                RecastCountTime = 0;
            }
            BulletsRemain--;    //残り弾数を減らす
            ShotCountTime = 0;  //発射間隔のカウントをリセット


            //デバッグ用
            Debug.Log("ミサイル発射 残り弾数: " + BulletsRemain);
        }
    }
}