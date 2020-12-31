using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public abstract class BasePlayer : MonoBehaviour, IBarrier, ILockOn
{
    public float HP { get; protected set; } = 0; //HP
    protected Rigidbody _Rigidbody = null;

    //移動用
    public float MoveSpeed = 0;                  //移動速度
    public float MaxSpeed { get; set; } = 0;     //最高速度

    //回転用
    public float RotateSpeed { get; set; } = 3.0f;
    float LimitCameraTiltX { get; set; } = 40.0f;


    public float AtackingDecreaseSpeed { get; set; } = 0.5f;   //攻撃中の移動速度の低下率

    //ブースト用
    const float BOOST_POSSIBLE_MIN = 0.2f;  //ブースト可能な最低ゲージ量
    float boostAccele = 2.0f;  //ブーストの加速度
    float maxBoostTime = 5.0f; //ブーストできる最大の時間
    float boostRecastTime = 6.0f;  //ブーストのリキャスト時間
    bool isBoost = false;


    //武器
    protected enum Weapon
    {
        MAIN,   //メイン武器
        SUB,    //サブ武器

        NONE
    }
    protected BaseAtack[] weapons = new BaseAtack[(int)Weapon.NONE];  //ウェポン群


    //アイテム
    protected enum ItemNum
    {
        ITEM_1,   //アイテム枠1
        ITEM_2,   //アイテム枠2

        NONE
    }
    protected Item[] items = new Item[(int)ItemNum.NONE];


    //弱体や強化などの状態
    public enum Status
    {
        BARRIER_STRENGTH,   //バリア強化
        STUN,               //スタン
        JAMMING,            //ジャミング
        SPEED_DOWN,         //移動・攻撃中のスピードダウン
        BARRIER_WEAK,       //バリア弱体化

        NONE
    }
    protected bool[] isStatus = new bool[(int)Status.NONE];   //状態異常が付与されているか


    //デバッグ用
    bool isQ = true;


    protected virtual void Awake()
    {
        LockOnStart();
        _Rigidbody = GetComponent<Rigidbody>();

        //武器の初期化
        //メインウェポンの処理
        AtackManager.CreateAtack(out GameObject main, AtackManager.Weapon.GATLING);    //Gatlingの生成
        Transform mainTransform = main.transform;   //キャッシュ
        mainTransform.SetParent(transform);         //作成したGatlingを子オブジェクトにする

        //位置と角度の初期設定
        mainTransform.localPosition = new Vector3(0, 0, 0);
        mainTransform.localRotation = Quaternion.Euler(0, 0, 0);

        //コンポーネントの取得
        BaseAtack ba = main.GetComponent<BaseAtack>(); //名前省略
        ba.Shooter = this;    //自分をヒットさせない
        weapons[(int)Weapon.MAIN] = ba;

        //配列初期化
        for (int i = 0; i < (int)Status.NONE; i++)
        {
            isStatus[i] = false;
        }
    }
    protected virtual void Start() { }
    protected virtual void Update()
    {
        BarrierUpdate();
        LockOnUpdate();

        if (Input.GetKeyDown(KeyCode.V))
        {
            isQ = !isQ;
            Debug.Log("移動処理切り替え");
        }
    }

    //移動処理
    protected virtual void Move(float speed, float _maxSpeed, Vector3 direction)
    {
        if (!isQ)
        {
            //最大速度に達していなかったら移動処理
            if (_Rigidbody.velocity.sqrMagnitude < Mathf.Pow(_maxSpeed, 2))
            {
                _Rigidbody.AddForce(direction * speed, ForceMode.Force);


                //デバッグ用
                //Debug.Log(Mathf.Pow(_maxSpeed, 2));
            }
        }
        else
        {
            _Rigidbody.AddForce(direction * speed + (direction * speed - _Rigidbody.velocity), ForceMode.Force);
        }


        //デバッグ用
        //Debug.Log(_rigidbody.velocity.sqrMagnitude);
    }

    //回転処理
    protected virtual void Rotate(float valueX, float valueY, float speed)
    {
        if (MainGameManager.IsCursorLock)
        {
            Vector3 angle = new Vector3(valueX * speed, valueY * speed, 0);

            //カメラの左右回転
            playerTransform.RotateAround(playerTransform.position, Vector3.up, angle.x);

            //カメラの上下の回転に制限をかける
            Vector3 localAngle = playerTransform.localEulerAngles;
            localAngle.x += angle.y * -1;
            if (localAngle.x > LimitCameraTiltX && localAngle.x < 180)
            {
                localAngle.x = LimitCameraTiltX;
            }
            if (localAngle.x < 360 - LimitCameraTiltX && localAngle.x > 180)
            {
                localAngle.x = 360 - LimitCameraTiltX;
            }
            playerTransform.localEulerAngles = localAngle;
        }
    }

    //ブースト処理
    protected virtual IEnumerator UseBoost(float speedMgnf, float time)
    {
        MoveSpeed *= speedMgnf;
        MaxSpeed *= speedMgnf;

        //time秒後に速度を戻す
        yield return new WaitForSeconds(time);
        MoveSpeed /= speedMgnf;
        MaxSpeed /= speedMgnf;
    }

    //攻撃処理
    protected virtual void UseWeapon(Weapon weapon)
    {
        weapons[(int)weapon].Shot(GetLockOn().Target);
    }

    //アイテム使用
    protected virtual void UseItem(ItemNum item)
    {
        int num = (int)item;  //名前省略

        //アイテム枠1にアイテムを持っていたら使用
        if (items[num] != null)
        {
            items[num].UseItem(this);


            //デバッグ用
            Debug.Log("アイテム使用");
        }
    }

    //プレイヤーにダメージを与える
    public virtual void Damage(float power)
    {
        float p = Useful.DecimalPointTruncation(power, 1);  //小数点第2以下切り捨て

        //バリアが破壊されていなかったらバリアにダメージを肩代わりさせる
        if (GetBarrier().HP > 0)
        {
            GetBarrier().Damage(p);
        }
        //バリアが破壊されていたらドローンが直接ダメージを受ける
        else
        {
            HP -= p;
            if (HP < 0)
            {
                HP = 0;
            }


            //デバッグ用
            Debug.Log(name + "に" + p + "のダメージ\n残りHP: " + HP);
        }
    }

    //サブウェポンをセットする
    public virtual void SetWeapon(AtackManager.Weapon weapon)
    {
        //サブウェポンの処理
        AtackManager.CreateAtack(out GameObject sub, weapon);    //武器の作成
        Transform subTransform = sub.transform; //キャッシュ
        subTransform.SetParent(transform);   //作成したGatlingを子オブジェクトにする

        //位置と角度の初期設定
        subTransform.localPosition = new Vector3(0, 0, 0);
        subTransform.localRotation = Quaternion.Euler(0, 0, 0);

        //コンポーネントの取得
        BaseAtack ba = sub.GetComponent<BaseAtack>();
        ba.Shooter = this;    //自分をヒットさせない
        weapons[(int)Weapon.SUB] = ba;
    }

    //指定したプレイヤーの状態状態を返す
    public bool GetStatus(Status status)
    {
        return isStatus[(int)status];
    }

    //バリア強化
    public virtual void SetBarrierStrength(float strengthValue, float time)
    {
        if (isStatus[(int)Status.BARRIER_STRENGTH])
        {
            return;
        }

        GetBarrier().Reduction = strengthValue;
        StartCoroutine(ReturnBarrierStrength(time));

        isStatus[(int)Status.BARRIER_STRENGTH] = true;
    }
    IEnumerator ReturnBarrierStrength(float time)
    {
        yield return new WaitForSeconds(time);
        GetBarrier().Reduction = 1;

        isStatus[(int)Status.BARRIER_STRENGTH] = false;
    }


    //バリア処理
    const float BARRIER_MAX_HP = 100;
    [SerializeField] GameObject barrierObject = null;
    float barrierCountTime = 0;
    bool isRegene = true;  //ゲーム開始時はHPMAXで回復の必要がないのでtrue

    //インタフェースの変数
    float IBarrier.HP { get; set; } = BARRIER_MAX_HP;
    float IBarrier.RegeneTime { get; set; } = 8.0f;
    float IBarrier.RegeneValue { get; set; } = 5.0f;
    float IBarrier.RepairBarrierTime { get; set; } = 15.0f;
    float IBarrier.Reduction { get; set; } = 1;

    protected IBarrier GetBarrier()
    {
        IBarrier barrier = this;
        return barrier;
    }

    void BarrierUpdate()
    {
        IBarrier b = GetBarrier();

        //バリアの修復処理
        if (b.HP <= 0)
        {
            if (barrierCountTime >= b.RepairBarrierTime)
            {
                Debug.Log("バリア修復");


                isRegene = true;
                b.RepairBarrier(10);  //HP10でバリアを修復する
            }
        }
        //バリアのHP自動回復処理
        else if (!isRegene)
        {
            if (barrierCountTime >= b.RegeneTime)
            {
                isRegene = true;
                b.Regene(b.RegeneValue);
            }
        }
        barrierCountTime += Time.deltaTime;
    }


    void IBarrier.Regene(float value)
    {
        StartCoroutine(regene(value));
    }

    IEnumerator regene(float value)
    {
        while (true)
        {
            IBarrier b = GetBarrier();

            //攻撃を受けたら処理をやめる
            if (!isRegene)
            {
                yield break;
            }

            b.HP += value;
            if (b.HP >= BARRIER_MAX_HP)
            {
                b.HP = BARRIER_MAX_HP;
                Debug.Log(name + ": バリアHPMAX: " + b.HP);
                yield break;
            }
            Debug.Log(name + ": リジェネ後バリアHP: " + b.HP);

            yield return new WaitForSeconds(1.0f);
        }
    }

    //破壊されたバリアを修復させる
    void IBarrier.RepairBarrier(float repairHP)
    {
        IBarrier b = GetBarrier();
        if (b.HP > 0)
        {
            return;
        }

        //修復直後はHP10
        b.HP = repairHP - b.RegeneValue;
        b.Regene(b.RegeneValue);
    }

    void IBarrier.Damage(float power)
    {
        IBarrier b = GetBarrier();

        float p = Useful.DecimalPointTruncation(power * GetBarrier().Reduction, 1);  //小数点第2以下切り捨て
        b.HP -= p;
        if (b.HP < 0)
        {
            b.HP = 0;
        }
        barrierCountTime = 0;
        isRegene = false;


        Debug.Log(name + ": バリアに" + p + "のダメージ\n残りHP: " + b.HP);
    }


    //ロックオン処理
    [SerializeField] Camera _camera = null;
    [SerializeField] Image lockOnImage = null;  //ロックオンした際に表示する画像
    List<GameObject> notLockOnObjects = new List<GameObject>();
    Transform playerTransform = null;
    Transform cameraTransform = null;
    Transform targetTransform = null;
    bool isTarget = false;

    //インタフェースの変数
    GameObject ILockOn.Target { get; set; } = null;
    float ILockOn.SearchRadius { get; set; } = 100.0f;
    float ILockOn.TrackingSpeed { get; set; } = 0.1f;

    protected ILockOn GetLockOn()
    {
        ILockOn lockOn = this;
        return lockOn;
    }

    void LockOnStart()
    {
        notLockOnObjects.Add(gameObject);    //操作している自分をロックオン対象に入れない
        playerTransform = transform;         //キャッシュ用
        cameraTransform = _camera.transform; //キャッシュ用
        lockOnImage.enabled = false;    //画像を非表示
    }

    void LockOnUpdate()
    {
        //ロックオン中なら追従処理
        if (isTarget)
        {
            ILockOn l = GetLockOn();

            //ロックオンの対象オブジェクトが消えていないなら継続して追尾
            if (l.Target != null)
            {
                Vector3 diff = targetTransform.position - cameraTransform.position;   //ターゲットとの距離
                Quaternion rotation = Quaternion.LookRotation(diff);      //ロックオンしたオブジェクトの方向

                //カメラの角度からtrackingSpeed(0～1)の速度でロックオンしたオブジェクトの角度に向く
                playerTransform.rotation = Quaternion.Slerp(playerTransform.rotation, rotation, l.TrackingSpeed);
            }
            //ロックオンしている最中に対象が消えたらロックオン解除
            else
            {
                isTarget = false;
                lockOnImage.enabled = false;
            }
        }
    }

    void ILockOn.StartLockOn()
    {
        //何もロックオンしていない場合はロックオン対象を探す
        if (!isTarget)
        {
            ILockOn l = GetLockOn();

            //取得したRaycastHit配列から各RaycastHitクラスのgameObjectを抜き取ってリスト化する
            var hits = Physics.SphereCastAll(
                cameraTransform.position,
                l.SearchRadius,
                cameraTransform.forward,
                0.01f).Select(h => h.transform.gameObject).ToList();

            hits = FilterTargetObject(hits);
            if (hits.Count > 0)
            {
                float minTargetDistance = float.MaxValue;   //初期化
                GameObject t = null;    //target

                foreach (var hit in hits)
                {
                    //ビューポートに変換
                    Vector3 targetScreenPoint = _camera.WorldToViewportPoint(hit.transform.position);

                    //画面の中央との距離を計算
                    float targetDistance = (new Vector2(0.5f, 0.5f) - new Vector2(targetScreenPoint.x, targetScreenPoint.y)).sqrMagnitude;

                    //距離が最小だったら更新
                    if (targetDistance < minTargetDistance)
                    {
                        minTargetDistance = targetDistance;
                        t = hit.gameObject;
                    }
                }
                l.Target = t;
                targetTransform = t.transform;
                lockOnImage.enabled = true;
                isTarget = true;
            }
        }
    }

    void ILockOn.ReleaseLockOn()
    {
        if (isTarget)
        {
            GetLockOn().Target = null;
            targetTransform = null;
            isTarget = false;
            lockOnImage.enabled = false;
        }
    }

    //リストから必要な要素だけ抜き取る
    List<GameObject> FilterTargetObject(List<GameObject> hits)
    {
        return hits.Where(h =>
        {
            //各要素の座標をビューポートに変換(画面左下が0:0、右上が1:1)して条件に合うものだけリストに詰め込む
            Vector3 screenPoint = _camera.WorldToViewportPoint(h.transform.position);
            return screenPoint.x > 0.25f && screenPoint.x < 0.75f && screenPoint.y > 0.15f && screenPoint.y < 0.85f && screenPoint.z > 0;
        }).Where(h => h.CompareTag(Player.PLAYER_TAG) ||   //プレイヤー
           h.CompareTag(CPUController.CPU_TAG) ||          //CPU
           h.CompareTag(JammingBot.JAMMING_BOT_TAG))       //ジャミングボット
          .Where(h =>
          {
              //notLockOnObjects内のオブジェクトがある場合は除外
              if (notLockOnObjects.FindIndex(o => ReferenceEquals(o, h.gameObject)) == -1)
              {
                  return true;
              }
              return false;
          })
          .ToList();
    }
}