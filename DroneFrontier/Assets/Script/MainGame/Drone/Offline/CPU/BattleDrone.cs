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
            [SerializeField] Transform _camera = null;

            //移動
            Vector3 moveDir = Vector3.zero;
            float moveDirTime = 0;
            float moveDirCountTime = 0;

            //回転
            const float CHANGE_ROTATE_TIME = 3f;
            Vector3 angle = Vector3.zero;
            float rotateCountTime = CHANGE_ROTATE_TIME;
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
            public BaseWeapon.Weapon setSubWeapon = BaseWeapon.Weapon.SHOTGUN;

            //攻撃処理
            int weaponTime = 0;
            float weaponCountTime = 0;
            bool useMainWeapon = false;


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

                if (!isDamage)
                {
                    //移動
                    if (lockOnAction.Target == null)
                    {
                        baseAction.Move(cacheTransform.forward);
                    }
                    else
                    {

                        Vector3 diff = lockOnAction.Target.transform.position - cacheTransform.position;
                        float changeDirDistance = 400f;
                        if (setSubWeapon == BaseWeapon.Weapon.SHOTGUN)
                        {
                            changeDirDistance = 75f;
                        }

                        //敵との一定の距離内に入ると移動方向切り替え
                        if (diff.sqrMagnitude <= Mathf.Pow(changeDirDistance, 2))
                        {
                            moveDirCountTime += Time.deltaTime;
                            if (moveDirCountTime >= moveDirTime)
                            {
                                if (Random.Range(0, 2) == 0)
                                {
                                    Quaternion leftAngle = Quaternion.Euler(0, -90, 0);
                                    Vector3 left = leftAngle.normalized * cacheTransform.forward;
                                    moveDir = left;
                                }
                                else
                                {
                                    Quaternion rightAngle = Quaternion.Euler(0, 90, 0);
                                    Vector3 right = rightAngle.normalized * cacheTransform.forward;
                                    moveDir = right;
                                }

                                moveDirCountTime = 0;
                                moveDirTime = Random.Range(2, 6);
                            }
                            baseAction.Move(moveDir * 2f);
                        }
                        else
                        {
                            baseAction.Move(cacheTransform.forward);
                        }
                    }

                    //回転
                    if (isRotate && lockOnAction.Target == null)
                    {
                        baseAction.Rotate(angle * 0.15f);
                    }
                    else
                    {
                        rotateCountTime += Time.deltaTime;
                        if (rotateCountTime > CHANGE_ROTATE_TIME)
                        {
                            StartRotate();
                            rotateCountTime = 0;
                        }
                    }
                }
                else
                {
                    if (lockOnAction.Target == null)
                    {
                        if (target != null)
                        {
                            Vector3 diff = target.position - _camera.position;    //ターゲットとの距離
                            Quaternion rotation = Quaternion.LookRotation(diff);  //ロックオンしたオブジェクトの方向

                            //カメラの角度からtrackingSpeed(0～1)の速度でロックオンしたオブジェクトの角度に向く
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
                    weaponCountTime += Time.deltaTime;
                    if (weaponCountTime >= weaponTime)
                    {
                        weaponCountTime = 0;
                        if (setSubWeapon == BaseWeapon.Weapon.SHOTGUN)
                        {
                            if (useMainWeapon)
                            {
                                weaponTime = Random.Range(8, 11);
                            }
                            else
                            {
                                weaponTime = 3;
                            }
                        }
                        if (setSubWeapon == BaseWeapon.Weapon.LASER)
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
                        subWeapon.Shot(lockOnAction.Target);
                    }
                }
                else
                {
                    weaponCountTime = weaponTime;
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


            public void StartRotate(Transform target)
            {
                if (lockOnAction.Target == null)
                {
                    isDamage = true;
                    this.target = target;
                }
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
                var hits = Physics.SphereCastAll(
                            _camera.position,
                            20f,
                            _camera.forward,
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
                    StartRotate();
                    return;
                }

                angle = Vector3.zero;
                isRotate = false;
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

                StartRotate();
                rotateCountTime = 0;
            }
        }
    }
}