using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Offline
{
    namespace CPU
    {
        public class BattleDrone : BaseDrone
        {
            //コンポーネント用
            Transform cacheTransform = null;
            Rigidbody _rigidbody = null;
            Animator animator = null;
            DroneBaseAction baseAction = null;
            DroneDamageAction damageAction = null;
            DroneSoundAction soundAction = null;
            DroneLockOnAction lockOnAction = null;
            DroneBarrierAction barrierAction = null;

            //カメラ
            [SerializeField] Transform cameraTransform = null;  //キャッシュ用

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
            public BaseWeapon.Weapon setSubWeapon = BaseWeapon.Weapon.NONE;
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


            protected override void Awake()
            {
                base.Awake();

                //コンポーネントの取得
                cacheTransform = transform;
                _rigidbody = GetComponent<Rigidbody>();
                animator = GetComponent<Animator>();
                baseAction = GetComponent<DroneBaseAction>();
                damageAction = GetComponent<DroneDamageAction>();
                soundAction = GetComponent<DroneSoundAction>();
                lockOnAction = GetComponent<DroneLockOnAction>();
                barrierAction = GetComponent<DroneBarrierAction>();
                listener = GetComponent<AudioListener>();
            }

            protected override void Start()
            {
                base.Start();

                //武器初期化
                mainWeapon = BaseWeapon.CreateWeapon(this, BaseWeapon.Weapon.GATLING, false);
                mainWeapon.SetParent(transform);
                subWeapon = BaseWeapon.CreateWeapon(this, setSubWeapon, false);
                subWeapon.SetParent(transform);
            }

            void Update()
            {
                if (!MainGameManager.Singleton.StartFlag) return;  //ゲーム開始フラグが立っていなかったら処理しない

                //死亡処理中は操作不可
                if (isDestroyFall || isDestroy) return;

                if (damageAction.HP <= 0)
                {
                    DestroyMe();
                }

                //移動
                moveSideTimeCount += Time.deltaTime;
                if (!isDamage)
                {
                    if (lockOnAction.Target == null)
                    {
                        baseAction.Move(cacheTransform.forward * atackingSpeed);
                    }
                    else
                    {
                        Vector3 diff = lockOnAction.Target.transform.position - cacheTransform.position;
                        float changeDirDistance = 300f;
                        if (!useMainWeapon)
                        {
                            if (setSubWeapon == BaseWeapon.Weapon.SHOTGUN)
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
                            baseAction.Move(cacheTransform.forward * atackingSpeed);
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
                        baseAction.Rotate(angle * 0.15f);
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
                            cacheTransform.rotation = Quaternion.Slerp(cacheTransform.rotation, rotation, 0.1f);
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
                        if (setSubWeapon == BaseWeapon.Weapon.SHOTGUN)
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
                        if (setSubWeapon == BaseWeapon.Weapon.MISSILE)
                        {
                            weaponTime = Random.Range(3, 8);
                        }
                        if (setSubWeapon == BaseWeapon.Weapon.LASER)
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
                    else if (setSubWeapon == BaseWeapon.Weapon.MISSILE)
                    {
                        atackingSpeed = 0.5f;
                    }
                    //レーザーを使っている場合は移動速度低下の増加
                    else if (setSubWeapon == BaseWeapon.Weapon.LASER)
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
                    baseAction.RotateDroneObject(deathRotate, deathRotateSpeed * Time.deltaTime);

                    //メイン武器を傾ける
                    mainWeapon.transform.localRotation = Quaternion.Slerp(mainWeapon.transform.localRotation, deathRotate, deathRotateSpeed * Time.deltaTime);

                    //サブ武器を傾ける
                    subWeapon.transform.localRotation = Quaternion.Slerp(subWeapon.transform.localRotation, deathRotate, deathRotateSpeed * Time.deltaTime);

                    //プロペラ減速
                    animator.speed *= 0.993f;

                    return;
                }
            }


            //攻撃を受けたときに攻撃してきた敵に回転させる
            public void StartRotate(Transform target)
            {
                if (lockOnAction.Target == null)
                {
                    isDamage = true;
                    this.target = target;
                }
            }

            //カメラの深度操作
            public void SetCameraDepth(int depth)
            {
                baseAction._Camera.depth = depth;
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

                //死んだのでロックオン・レーダー解除
                lockOnAction.StopLockOn();

                //死亡SE再生
                soundAction.PlayOneShot(SoundManager.SE.DEATH, SoundManager.BaseSEVolume);

                //死亡後爆破
                Invoke(nameof(CreateExplosion), 2.5f);
            }

            //ドローンを非表示にして爆破
            void CreateExplosion()
            {
                //ドローンの非表示
                droneObject.gameObject.SetActive(false);
                barrierAction.BarrierObject.SetActive(false);
                mainWeapon.gameObject.SetActive(false);
                subWeapon.gameObject.SetActive(false);

                //当たり判定も消す
                GetComponent<Collider>().enabled = false;

                //爆破生成
                Instantiate(explosion, cacheTransform);

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
                           .Where(h => !h.transform.CompareTag(TagNameManager.ITEM))    //アイテム除外
                           .Where(h => !h.transform.CompareTag(TagNameManager.BULLET))  //弾丸除外
                           .Where(h => !h.transform.CompareTag(TagNameManager.GIMMICK)) //ギミック除外
                           .Where(h => !h.transform.CompareTag(TagNameManager.JAMMING)) //ジャミングエリア除外
                           .Where(h => !h.transform.CompareTag(TagNameManager.JAMMING_BOT)) //ジャミングボット除外
                           .Where(h => !h.transform.CompareTag(TagNameManager.PLAYER))  //プレイヤー除外
                           .Where(h => !h.transform.CompareTag(TagNameManager.CPU))     //CPU除外
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
                    Vector3 left = leftAngle.normalized * cacheTransform.forward;
                    moveSideDir = left;
                }
                else
                {
                    Quaternion rightAngle = Quaternion.Euler(0, 90, 0);
                    Vector3 right = rightAngle.normalized * cacheTransform.forward;
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
                if (collision.gameObject.CompareTag(TagNameManager.PLAYER)) return;
                if (collision.gameObject.CompareTag(TagNameManager.CPU)) return;
                if (collision.gameObject.CompareTag(TagNameManager.JAMMING_BOT)) return;

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
                if (collision.gameObject.CompareTag(TagNameManager.PLAYER)) return;
                if (collision.gameObject.CompareTag(TagNameManager.CPU)) return;
                if (collision.gameObject.CompareTag(TagNameManager.JAMMING_BOT)) return;
                if (lockOnAction.Target == null) return;

                if (moveSideTimeCount >= moveSideTime)
                {
                    StartSideMove();
                    moveSideTimeCount = 0;
                }
            }
        }
    }
}