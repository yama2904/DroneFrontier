using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class RaceDrone : NetworkBehaviour
{
    //コンポーネント用
    Transform cacheTransform = null;
    DroneBaseAction baseAction = null;

    //移動用
    [SerializeField, Tooltip("移動速度")] float moveSpeed = 150.0f;  //移動速度
    [HideInInspector] float maxSpeed = 100;       //最高速度
    [HideInInspector] float minSpeed = 100;       //最低速度

    //回転用
    [SerializeField, Tooltip("回転速度")] public float rotateSpeed = 5.0f;

    //ドローンが移動した際にオブジェクトが傾く処理用
    float moveRotateSpeed = 0.02f;
    Quaternion frontMoveRotate = Quaternion.Euler(50, 0, 0);
    Quaternion leftMoveRotate = Quaternion.Euler(0, 0, 60);
    Quaternion rightMoveRotate = Quaternion.Euler(0, 0, -60);
    Quaternion backMoveRotate = Quaternion.Euler(-70, 0, 0);

    //ブースト用
    const float BOOST_POSSIBLE_MIN = 0.2f;  //ブースト可能な最低ゲージ量
    [SerializeField] Image boostGaugeImage = null;   //ブーストのゲージ画像
    [SerializeField] Image boostGaugeFrameImage = null; //ゲージ枠
    [SerializeField, Tooltip("ブーストの加速度")] float boostAccele = 4.0f;  //ブーストの加速度
    [SerializeField, Tooltip("ブースト時間")] float maxBoostTime = 10.0f;   //ブーストできる最大の時間
    [SerializeField, Tooltip("ブーストのリキャスト時間")] float boostRecastTime = 8.0f;  //ブーストのリキャスト時間
    bool isBoost = false;

    //サウンド
    enum SE
    {
        Boost,          //ブースト
        PROPELLER,      //プロペラ
        WALL_STUN,      //見えない壁に触れる

        NONE
    }
    AudioSource[] audios;


    public override void OnStartClient()
    {
        base.OnStartClient();
        RaceManager.AddPlayerData(this, connectionToClient);

        //AudioSourceの初期化
        audios = GetComponents<AudioSource>();
        audios[(int)SE.Boost].clip = SoundManager.GetAudioClip(SoundManager.SE.BOOST);
        audios[(int)SE.PROPELLER].clip = SoundManager.GetAudioClip(SoundManager.SE.PROPELLER);
        audios[(int)SE.WALL_STUN].clip = SoundManager.GetAudioClip(SoundManager.SE.WALL_STUN);

        //プロペラは延々流す
        PlaySE((int)SE.PROPELLER, SoundManager.BaseSEVolume, true);
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        //ブースト初期化
        boostGaugeImage.enabled = true;
        boostGaugeImage.fillAmount = 1;
        boostGaugeFrameImage.enabled = true;


        Debug.Log("End: OnStartLocalPlayer");
    }

    void Awake()
    {
        //コンポーネントの初期化
        cacheTransform = transform;
        baseAction = GetComponent<DroneBaseAction>();

        maxSpeed = moveSpeed * 10;
        minSpeed = moveSpeed * 0.2f;
    }

    void Start()
    {
    }

    void Update()
    {
        if (!isLocalPlayer) return;
        if (!MainGameManager.Singleton.StartFlag) return;  //ゲーム開始フラグが立っていなかったら処理しない


        #region Move

        //移動処理
        //前進
        if (Input.GetKey(KeyCode.W))
        {
            baseAction.Move(moveSpeed, cacheTransform.forward);
            baseAction.RotateDroneObject(frontMoveRotate, moveRotateSpeed);
        }
        else
        {
            baseAction.RotateDroneObject(Quaternion.identity, moveRotateSpeed);
        }

        //左移動
        if (Input.GetKey(KeyCode.A))
        {
            Quaternion leftAngle = Quaternion.Euler(0, -90, 0);
            Vector3 left = leftAngle.normalized * cacheTransform.forward;
            baseAction.Move(moveSpeed, left);
            baseAction.RotateDroneObject(leftMoveRotate, moveRotateSpeed);
        }
        else
        {
            baseAction.RotateDroneObject(Quaternion.identity, moveRotateSpeed);
        }

        //後退
        if (Input.GetKey(KeyCode.S))
        {
            Quaternion backwardAngle = Quaternion.Euler(0, 180, 0);
            Vector3 backward = backwardAngle.normalized * cacheTransform.forward;
            baseAction.Move(moveSpeed, backward);
            baseAction.RotateDroneObject(backMoveRotate, moveRotateSpeed);
        }
        else
        {
            baseAction.RotateDroneObject(Quaternion.identity, moveRotateSpeed);
        }

        //右移動
        if (Input.GetKey(KeyCode.D))
        {
            Quaternion rightAngle = Quaternion.Euler(0, 90, 0);
            Vector3 right = rightAngle.normalized * cacheTransform.forward;
            baseAction.Move(moveSpeed, right);
            baseAction.RotateDroneObject(rightMoveRotate, moveRotateSpeed);
        }
        else
        {
            baseAction.RotateDroneObject(Quaternion.identity, moveRotateSpeed);
        }

        //上下移動
        if (Input.mouseScrollDelta.y != 0)
        {
            Quaternion upAngle = Quaternion.Euler(-90, 0, 0);
            Vector3 upward = upAngle.normalized * Vector3.forward;
            baseAction.Move(moveSpeed * 1.7f * Input.mouseScrollDelta.y, upward);
        }
        if (Input.GetKey(KeyCode.R))
        {
            Quaternion upAngle = Quaternion.Euler(-90, 0, 0);
            Vector3 upward = upAngle.normalized * Vector3.forward;
            baseAction.Move(moveSpeed, upward);
        }
        if (Input.GetKey(KeyCode.F))
        {
            Quaternion downAngle = Quaternion.Euler(90, 0, 0);
            Vector3 down = downAngle.normalized * Vector3.forward;
            baseAction.Move(moveSpeed, down);
        }

        #endregion


        //
        //設定画面中はここより下の処理は行わない
        if (MainGameManager.IsConfig)
        {
            return;
        }
        //
        //


        //回転処理
        if (MainGameManager.IsCursorLock)
        {
            float x = Input.GetAxis("Mouse X");
            float y = Input.GetAxis("Mouse Y");
            baseAction.Rotate(x, y, rotateSpeed * CameraManager.CameraSpeed);
        }

        #region Boost

        //ブースト使用
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //ブーストが使用可能なゲージ量ならブースト使用
            if (boostGaugeImage.fillAmount >= BOOST_POSSIBLE_MIN)
            {
                moveSpeed = baseAction.ModifySpeed(moveSpeed, minSpeed, maxSpeed, boostAccele);
                isBoost = true;
                PlaySE((int)SE.Boost, SoundManager.BaseSEVolume * 0.15f, true);    //加速音の再生


                //デバッグ用
                Debug.Log("ブースト使用");
            }
        }
        //ブースト使用中の処理
        if (isBoost)
        {
            //キーを押し続けている間はゲージ消費
            if (Input.GetKey(KeyCode.Space))
            {
                boostGaugeImage.fillAmount -= 1.0f / maxBoostTime * Time.deltaTime;

                //ゲージが空になったらブースト停止
                if (boostGaugeImage.fillAmount <= 0)
                {
                    boostGaugeImage.fillAmount = 0;

                    moveSpeed = baseAction.ModifySpeed(moveSpeed, minSpeed, maxSpeed, 1 / boostAccele);
                    isBoost = false;
                    StopSE((int)SE.Boost);


                    //デバッグ用
                    Debug.Log("ブースト終了");
                }
            }
            //キーを離したらブースト停止
            if (Input.GetKeyUp(KeyCode.Space))
            {
                moveSpeed = baseAction.ModifySpeed(moveSpeed, minSpeed, maxSpeed, 1 / boostAccele);
                isBoost = false;
                StopSE((int)SE.Boost);


                //デバッグ用
                Debug.Log("ブースト終了");
            }
        }

        //ブースト未使用時にゲージ回復
        if (!isBoost)
        {
            if (boostGaugeImage.fillAmount < 1.0f)
            {
                boostGaugeImage.fillAmount += 1.0f / boostRecastTime * Time.deltaTime;
                if (boostGaugeImage.fillAmount >= 1.0f)
                {
                    boostGaugeImage.fillAmount = 1;
                }
            }
        }

        #endregion
    }

    #region Sound

    //SE再生
    void PlaySE(int index, float volume, bool loop = false)
    {
        if (index >= (int)SE.NONE) return;
        if (volume > 1.0f)
        {
            volume = 1.0f;
        }

        audios[index].volume = volume;
        audios[index].loop = loop;
        audios[index].Play();
    }

    //SE停止
    void StopSE(int index)
    {
        if (index >= (int)SE.NONE) return;
        audios[index].Stop();
    }

    #endregion

    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        RaceManager.Singleton.SetGoalDrone(netId);
    }
}
