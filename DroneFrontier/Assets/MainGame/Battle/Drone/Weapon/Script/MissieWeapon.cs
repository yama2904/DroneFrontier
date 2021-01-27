using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class MissieWeapon : BaseWeapon
{
    [SerializeField] MissileBullet missile = null;  //複製する弾丸
    SyncList<GameObject> settingBullets = new SyncList<GameObject>();
    const int USE_INDEX = 0;
    bool setMissile = false;

    //弾丸のパラメータ
    [SerializeField, Tooltip("1秒間に進む距離")] float speedPerSecond = 13.0f;  //1秒間に進む量
    [SerializeField, Tooltip("射程")] float destroyTime = 2.0f;      //発射してから消えるまでの時間(射程)
    [SerializeField, Tooltip("誘導力")] float trackingPower = 2.3f;  //追従力
    [SerializeField, Tooltip("1秒間に発射する弾数")] float shotPerSecond = 1.0f;    //1秒間に発射する弾数

    [SerializeField, Tooltip("リキャスト時間")] float _recast = 10f;
    [SerializeField, Tooltip("ストック可能な弾数")] int _maxBullets = 3;
    [SerializeField, Tooltip("威力")] float _power = 20f;


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
        Recast = _recast;
        ShotInterval = 1.0f / shotPerSecond;
        ShotCountTime = ShotInterval;
        MaxBullets = _maxBullets;
        BulletsRemain = MaxBullets;
        BulletPower = _power;
    }

    protected override void Start() { }
    protected override void Update() { }

    public override void Init()
    {
        BulletPower = 20.0f;
        CmdCreateMissile();
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

    public override void UpdateMe()
    {
        //発射間隔のカウント
        if (!setMissile)
        {
            ShotCountTime += Time.deltaTime;
            if (ShotCountTime > ShotInterval)
            {
                ShotCountTime = ShotInterval;
                if (BulletsRemain > 0)  //弾丸が残っていない場合は処理しない
                {
                    CmdCreateMissile();
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
            CmdDestroyMissile();
        }
        CmdCreateMissile();
        setMissile = true;
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

        //弾丸のパラメータ設定
        m.Shooter = shooter;    //撃ったプレイヤーを登録
        m.SpeedPerSecond = speedPerSecond;  //スピード
        m.DestroyTime = destroyTime;        //射程
        m.TrackingPower = trackingPower;    //誘導力
        m.Power = BulletPower;              //威力

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
        if (ShotCountTime < ShotInterval) return;

        //バグ防止
        if (!setMissile) return;
        if (settingBullets.Count <= 0) return;

        //残り弾数が0だったら撃たない
        if (BulletsRemain <= 0) return;


        //ミサイル発射
        CmdShot(target);
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

    [Command(ignoreAuthority = true)]
    void CmdShot(GameObject target)
    {
        MissileBullet m = settingBullets[USE_INDEX].GetComponent<MissileBullet>();
        m.CmdShot(target);

        settingBullets.RemoveAt(USE_INDEX);
    }
}