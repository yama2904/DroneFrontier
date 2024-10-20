using Offline;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class CpuBattleDrone : MonoBehaviour, IBattleDrone
{
    //コンポーネント用
    Transform _transform = null;
    Rigidbody _rigidbody = null;
    Animator animator = null;
    DroneMoveComponent baseAction = null;
    DroneRotateComponent _rotateComponent = null;
    DroneDamageComponent damageAction = null;
    DroneSoundComponent soundAction = null;
    Offline.CPU.DroneLockOnAction lockOnAction = null;

    [SerializeField] Transform cameraTransform = null;  //キャッシュ用

    [SerializeField] Camera _camera = null;

    //AudioListener
    AudioListener listener = null;

    //移動
    Vector3 moveSideDir = Vector3.zero;  //移動する方向(右か左)
    float moveSideTime = 0;       //横移動する時間
    float moveSideTimeCount = 0;  //時間計測
    bool isMoveSide = false;      //横移動するか

    //回転
    const float CHANGE_ROTATE_TIME = 3f;
    Vector3 angle = Vector3.zero;
    float rotateTimeCount = CHANGE_ROTATE_TIME;
    bool isRotate = false;

    //武器
    protected enum Weapon
    {
        MAIN,   //メイン武器
        SUB,    //サブ武器

        NONE
    }
    BaseWeapon mainWeapon = null;
    BaseWeapon subWeapon = null;

    float atackingSpeed = 1f;   //攻撃中の移動速度の変動用

    //攻撃処理
    int weaponTime = 0;
    float weaponTimeCount = 0;
    bool useMainWeapon = false;

    //ショットガン用
    float shotgunStayTime = 2f;
    float shotgunStayTimeCount = 0;


    //死亡処理用
    [SerializeField] GameObject explosion = null;
    [SerializeField] Transform droneObject = null;
    Quaternion deathRotate = Quaternion.Euler(28, -28, -28);
    float deathRotateSpeed = 2f;
    float gravityAccele = 1f;  //落下加速用
    float fallTime = 5.0f;   //死亡後の落下時間
    bool isDestroyFall = false;
    bool isDestroy = false;

    Transform target = null;
    bool isDamage = false;

    /// <summary>
    /// ドローンのゲームオブジェクト
    /// </summary>
    public GameObject GameObject { get; private set; } = null;

    /// <summary>
    /// ドローンの名前
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// ドローンのHP
    /// </summary>
    public float HP
    {
        get { return _hp; }
        set
        {
            if (_hp <= 0) return;

            if (value > 0)
            {
                _hp = value;
            }
            else
            {
                // HPが0になったら破壊処理
                _hp = 0;
                DestroyMe();
            }
        }
    }
    private float _hp = 0;

    /// <summary>
    /// 現在のストック数
    /// </summary>
    public int StockNum { get; set; } = 0;

    /// <summary>
    /// ドローンのサブ武器
    /// </summary>
    public BaseWeapon.Weapon SubWeapon { get; set; } = BaseWeapon.Weapon.SHOTGUN;

    /// <summary>
    /// ドローン破壊イベント
    /// </summary>
    public event System.EventHandler DroneDestroyEvent;

    [SerializeField, Tooltip("ドローンの最大HP")]
    private float _maxHP = 100f;

    protected void Awake()
    {
        //コンポーネントの取得
        GameObject = gameObject;
        _transform = transform;
        _rigidbody = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        baseAction = GetComponent<DroneMoveComponent>();
        _rotateComponent = GetComponent<DroneRotateComponent>();
        damageAction = GetComponent<DroneDamageComponent>();
        soundAction = GetComponent<DroneSoundComponent>();
        lockOnAction = GetComponent<Offline.CPU.DroneLockOnAction>();
        listener = GetComponent<AudioListener>();

        // HP初期化
        HP = _maxHP;

        // ダメージイベント設定
        damageAction.DamageEvent += DamageHandler;
    }

    private void Start()
    {
        //武器初期化
        mainWeapon = BaseWeapon.CreateWeapon(this, BaseWeapon.Weapon.GATLING, false);
        mainWeapon.SetParent(transform);
        subWeapon = BaseWeapon.CreateWeapon(this, SubWeapon, false);
        subWeapon.SetParent(transform);
    }

    private void Update()
    {
        //死亡処理中は操作不可
        if (isDestroyFall || isDestroy) return;

        //移動
        moveSideTimeCount += Time.deltaTime;
        if (!isDamage)
        {
            if (lockOnAction.Target == null)
            {
                baseAction.Move(_transform.forward * atackingSpeed);
            }
            else
            {
                Vector3 diff = lockOnAction.Target.transform.position - _transform.position;
                float changeDirDistance = 300f;
                if (!useMainWeapon)
                {
                    if (SubWeapon == BaseWeapon.Weapon.SHOTGUN)
                    {
                        changeDirDistance = 30f;
                    }
                    else
                    {
                        changeDirDistance = 500f;
                    }
                }

                //敵との一定の距離内に入ると左右移動
                if (diff.sqrMagnitude <= Mathf.Pow(changeDirDistance, 2))
                {
                    if (moveSideTimeCount >= moveSideTime)
                    {
                        StartSideMove();
                        moveSideTimeCount = 0;
                    }
                }
                //一定距離内にいないと直進
                else
                {
                    baseAction.Move(_transform.forward * atackingSpeed);
                    if (moveSideTimeCount >= moveSideTime)
                    {
                        moveSideTimeCount = moveSideTime;
                        isMoveSide = false;
                    }
                }

                if (isMoveSide)
                {
                    baseAction.Move(moveSideDir * atackingSpeed);
                }
            }

            //回転
            if (isRotate && lockOnAction.Target == null)
            {
                baseAction.RotateCamera(0.7f, 0.7f);
            }
            else
            {
                rotateTimeCount += Time.deltaTime;
                if (rotateTimeCount > CHANGE_ROTATE_TIME)
                {
                    StartRotate();
                    rotateTimeCount = 0;
                }
            }
        }
        //攻撃されたら止まって回転
        else
        {
            if (lockOnAction.Target == null)
            {
                if (target != null)
                {
                    Vector3 diff = target.position - cameraTransform.position;    //ターゲットとの距離
                    Quaternion rotation = Quaternion.LookRotation(diff);  //攻撃してきた敵の方向

                    //攻撃してきた敵の方向に向く
                    _transform.rotation = Quaternion.Slerp(_transform.rotation, rotation, 0.1f);
                }
            }
            else
            {
                isDamage = false;
            }
        }

        //常にロックオン処理
        if (lockOnAction.UseLockOn(0.3f))
        {
            //ロックオン対象があれば攻撃
            weaponTimeCount += Time.deltaTime;

            //一定時間で攻撃する武器切り替え
            if (weaponTimeCount >= weaponTime)
            {
                weaponTimeCount = 0;
                if (SubWeapon == BaseWeapon.Weapon.SHOTGUN)
                {
                    //ショットガンを使う場合は短時間
                    if (useMainWeapon)
                    {
                        weaponTime = 5;
                        shotgunStayTimeCount = 0;
                    }
                    else
                    {
                        weaponTime = Random.Range(8, 11);
                    }
                }
                if (SubWeapon == BaseWeapon.Weapon.MISSILE)
                {
                    weaponTime = Random.Range(3, 8);
                }
                if (SubWeapon == BaseWeapon.Weapon.LASER)
                {
                    weaponTime = Random.Range(7, 11);
                }
                useMainWeapon = !useMainWeapon;
            }

            if (useMainWeapon)
            {
                mainWeapon.Shot(lockOnAction.Target);
            }
            else
            {
                //ショットガンは敵に近づいてから攻撃する
                shotgunStayTimeCount += Time.deltaTime;
                if (shotgunStayTimeCount >= shotgunStayTime)
                {
                    subWeapon.Shot(lockOnAction.Target);
                }
            }

            //攻撃中の移動速度低下の設定
            if (useMainWeapon)
            {
                //ガトリング使用中は移動速度低下
                atackingSpeed = 0.5f;
            }
            //ミサイル使用中も移動速度低下
            else if (SubWeapon == BaseWeapon.Weapon.MISSILE)
            {
                atackingSpeed = 0.5f;
            }
            //レーザーを使っている場合は移動速度低下の増加
            else if (SubWeapon == BaseWeapon.Weapon.LASER)
            {
                atackingSpeed = 0.35f;
            }
        }
        else
        {
            weaponTimeCount = weaponTime;
            atackingSpeed = 1f;
        }
    }

    void FixedUpdate()
    {
        //死亡時落下処理
        if (isDestroyFall)
        {
            //加速しながら落ちる
            _rigidbody.AddForce(new Vector3(0, -10 * gravityAccele, 0), ForceMode.Acceleration);
            gravityAccele += 20 * Time.deltaTime;

            //ドローンを傾ける
            _rotateComponent.Rotate(deathRotate, deathRotateSpeed * Time.deltaTime);

            //メイン武器を傾ける
            mainWeapon.transform.localRotation = Quaternion.Slerp(mainWeapon.transform.localRotation, deathRotate, deathRotateSpeed * Time.deltaTime);

            //サブ武器を傾ける
            subWeapon.transform.localRotation = Quaternion.Slerp(subWeapon.transform.localRotation, deathRotate, deathRotateSpeed * Time.deltaTime);

            //プロペラ減速
            animator.speed *= 0.993f;

            return;
        }
    }

    /// <summary>
    /// ダメージハンドラー
    /// </summary>
    /// <param name="sender">イベントオブジェクト</param>
    /// <param name="source">ダメージを与えたオブジェクト</param>
    /// <param name="damage">ダメージ量</param>
    public void DamageHandler(DroneDamageComponent sender, GameObject source, float damage)
    {
        if (lockOnAction.Target == null)
        {
            isDamage = true;
            target = source.transform;
        }
    }
    //攻撃を受けたときに攻撃してきた敵に回転させる
    public void StartRotate(Transform target)
    {
    }

    //カメラの深度操作
    public void SetCameraDepth(int depth)
    {
        _camera.depth = depth;
    }

    //AudioListenerのオンオフ
    public void SetAudioListener(bool flag)
    {
        listener.enabled = flag;
    }


    void DestroyMe()
    {
        gravityAccele = 1f;
        isDestroyFall = true;
        isDestroy = true;

        // 移動コンポーネント停止
        baseAction.enabled = false;

        //死んだのでロックオン・レーダー解除
        lockOnAction.StopLockOn();

        //死亡SE再生
        soundAction.PlayOneShot(SoundManager.SE.DEATH, SoundManager.SEVolume);

        //死亡後爆破
        Invoke(nameof(CreateExplosion), 2.5f);
    }

    //ドローンを非表示にして爆破
    void CreateExplosion()
    {
        //ドローンの非表示
        droneObject.gameObject.SetActive(false);
        mainWeapon.gameObject.SetActive(false);
        subWeapon.gameObject.SetActive(false);

        //当たり判定も消す
        GetComponent<Collider>().enabled = false;

        //爆破生成
        Instantiate(explosion, _transform);

        //落下停止
        isDestroyFall = false;

        //爆破後一定時間で消去
        Destroy(gameObject, fallTime);
    }

    //回転の開始
    void StartRotate()
    {
        if (Random.Range(0, 2) == 0)
        {
            angle.x = Random.Range(-1f, 1f);
        }
        else
        {
            angle.y = Random.Range(-1f, 1f);
        }
        isRotate = true;
        Invoke(nameof(StopRotate), Random.Range(2, 4));
    }

    //回転の停止
    void StopRotate()
    {
        //正面に障害物があるか
        var hits = Physics.SphereCastAll(
                    cameraTransform.position,
                    20f,
                    cameraTransform.forward,
                    100f)
                   .Where(h => !h.transform.CompareTag(TagNameConst.ITEM))    //アイテム除外
                   .Where(h => !h.transform.CompareTag(TagNameConst.BULLET))  //弾丸除外
                   .Where(h => !h.transform.CompareTag(TagNameConst.GIMMICK)) //ギミック除外
                   .Where(h => !h.transform.CompareTag(TagNameConst.JAMMING)) //ジャミングエリア除外
                   .Where(h => !h.transform.CompareTag(TagNameConst.JAMMING_BOT)) //ジャミングボット除外
                   .Where(h => !h.transform.CompareTag(TagNameConst.PLAYER))  //プレイヤー除外
                   .Where(h => !h.transform.CompareTag(TagNameConst.CPU))     //CPU除外
                   .ToList();  //リスト化

        if (hits.Count > 0)
        {
            //障害物がある場合は再度ランダムに回転
            StartRotate();
            return;
        }

        angle = Vector3.zero;
        isRotate = false;
    }


    //左右移動
    void StartSideMove()
    {
        if (Random.Range(0, 2) == 0)
        {
            Quaternion leftAngle = Quaternion.Euler(0, -90, 0);
            Vector3 left = leftAngle.normalized * _transform.forward;
            moveSideDir = left;
        }
        else
        {
            Quaternion rightAngle = Quaternion.Euler(0, 90, 0);
            Vector3 right = rightAngle.normalized * _transform.forward;
            moveSideDir = right;
        }

        moveSideTime = Random.Range(2, 6);
        isMoveSide = true;
    }


    //リスト内で最も距離が近いRaycastHitを返す
    void GetNearestObject(out RaycastHit hit, List<RaycastHit> hits)
    {
        hit = hits[0];
        float minTargetDistance = float.MaxValue;   //初期化
        foreach (RaycastHit h in hits)
        {
            //距離が最小だったら更新
            if (h.distance < minTargetDistance)
            {
                minTargetDistance = h.distance;
                hit = h;
            }
        }
    }


    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(TagNameConst.PLAYER)) return;
        if (collision.gameObject.CompareTag(TagNameConst.CPU)) return;
        if (collision.gameObject.CompareTag(TagNameConst.JAMMING_BOT)) return;

        if (lockOnAction.Target == null)
        {
            StartRotate();
            rotateTimeCount = 0;
        }
        else
        {
            StartSideMove();
            moveSideTimeCount = 0;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag(TagNameConst.PLAYER)) return;
        if (collision.gameObject.CompareTag(TagNameConst.CPU)) return;
        if (collision.gameObject.CompareTag(TagNameConst.JAMMING_BOT)) return;
        if (lockOnAction.Target == null) return;

        if (moveSideTimeCount >= moveSideTime)
        {
            StartSideMove();
            moveSideTimeCount = 0;
        }
    }
}