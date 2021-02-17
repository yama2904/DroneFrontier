using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

namespace Online
{
    public class Shotgun : BaseWeapon
    {
        //ショットガンのパラメータ
        [SerializeField] Bullet bullet = null;
        [SerializeField, Tooltip("拡散力")] float angle = 4.2f;
        [SerializeField, Tooltip("拡散力のランダム値")] float angleDiff = 2.8f;
        AudioSource audioSource = null;

        //弾丸のパラメータ
        [SerializeField, Tooltip("威力")] float power = 5.5f;
        [SerializeField, Tooltip("リキャスト時間")] float recast = 2f;
        [SerializeField, Tooltip("ストック可能な弾数")] int maxBulletNum = 5;
        [SerializeField, Tooltip("1秒間に進む距離")] float speed = 800f;
        [SerializeField, Tooltip("射程")] float destroyTime = 0.6f;
        [SerializeField, Tooltip("1秒間に発射する弾数")] float shotPerSecond = 2.0f;
        float shotInterval = 0;     //発射間隔
        float shotTimeCount = 0;    //時間計測用
        float recastTimeCount = 0;  //時間計測用
        int haveBulletNum = 0;


        //所持弾数のUI用
        const float UI_POS_DIFF_X = 1.2f;
        const float UI_POS_Y = 175f;
        [SerializeField] Canvas UIParentCanvas = null;
        [SerializeField] Image bulletUIBack = null;
        [SerializeField] Image bulletUIFront = null;
        Image[] UIs;


        public override void OnStartClient()
        {
            base.OnStartClient();
            audioSource = GetComponent<AudioSource>();
            audioSource.clip = SoundManager.GetAudioClip(SoundManager.SE.SHOTGUN);
            audioSource.volume = SoundManager.BaseSEVolume;
        }

        void Start()
        {
            shotInterval = 1.0f / shotPerSecond;
            shotTimeCount = shotInterval;
            haveBulletNum = maxBulletNum;
        }

        public override void Init()
        {
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

        public override void UpdateMe()
        {
            //リキャストと発射間隔のカウント
            recastTimeCount += Time.deltaTime;
            if (recastTimeCount > recast)
            {
                recastTimeCount = recast;
            }

            shotTimeCount += Time.deltaTime;
            if (shotTimeCount > shotInterval)
            {
                shotTimeCount = shotInterval;
            }

            //最大弾数持っているなら処理しない
            if (haveBulletNum >= maxBulletNum) return;

            //リキャスト時間経過したら弾数を1個補充
            if (recastTimeCount >= recast)
            {
                UIs[haveBulletNum].fillAmount = 1f;
                haveBulletNum++;        //弾数を回復
                recastTimeCount = 0;    //リキャストのカウントをリセット


                //デバッグ用
                Debug.Log("ショットガンの弾丸が1回分補充されました");
            }
            else
            {
                UIs[haveBulletNum].fillAmount = recastTimeCount / recast;
            }
        }

        public override void Shot(GameObject target = null)
        {
            //前回発射して発射間隔分の時間が経過していなかったら撃たない
            if (shotTimeCount < shotInterval) return;

            //残り弾数が0だったら撃たない
            if (haveBulletNum <= 0) return;


            //敵の位置に応じて発射角度を修正
            Quaternion rotation = shotPos.rotation;
            if (target != null)
            {
                Vector3 diff = target.transform.position - shotPos.position;   //ターゲットとの距離
                rotation = Quaternion.LookRotation(diff);   //ロックオンしたオブジェクトの方向
            }

            //弾を散らす
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    CmdCreateBullet(shotPos.position, rotation, angle * i, angle * j, target);
                }
            }

            //SE再生
            CmdCallPlaySE();

            //所持弾丸のUIを灰色に変える
            for (int i = haveBulletNum - 1; i < maxBulletNum; i++)
            {
                UIs[i].fillAmount = 0;
            }

            //残り弾丸がMAXで撃つと一瞬で弾丸が1個回復するので
            //残り弾丸がMAXで撃った場合のみリキャストを0にする
            if (haveBulletNum == maxBulletNum)
            {
                recastTimeCount = 0;
            }
            haveBulletNum--;    //残り弾数を減らす
            shotTimeCount = 0;  //発射間隔のカウントをリセット


            //デバッグ用
            Debug.Log("残り弾数: " + haveBulletNum);
        }

        Bullet CreateBullet(Vector3 pos, Quaternion rotation, float angleX, float angleY, GameObject target)
        {
            Bullet b = Instantiate(bullet, pos, rotation);    //弾丸の複製

            //弾丸のパラメータ設定
            b.Init(shooter.netId, power, 0, speed, destroyTime, target);

            //弾丸の進む方向を変えて散らす処理
            Transform t = b.transform;  //キャッシュ
            float rotateX = angleX + Random.Range(angleDiff * -1, angleDiff);  //左右の角度
            float rotateY = angleY + Random.Range(angleDiff * -1, angleDiff);   //上下の角度
            t.RotateAround(t.position, t.right, rotateY);
            t.RotateAround(t.position, t.up, rotateX);

            return b;
        }

        [Command]
        void CmdCreateBullet(Vector3 pos, Quaternion rotation, float angleX, float angleY, GameObject target)
        {
            Bullet b = CreateBullet(pos, rotation, angleX, angleY, target);
            NetworkServer.Spawn(b.gameObject, connectionToClient);
        }

        [Command]
        void CmdCallPlaySE()
        {
            RpcPlaySE();
        }

        [ClientRpc]
        void RpcPlaySE()
        {
            audioSource.Play();
        }
    }
}