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


            [SerializeField] Transform _camera = null;

            const float CHANGE_ROTATE_TIME = 3f;
            Vector3 angle = Vector3.zero;
            float rotateCountTime = CHANGE_ROTATE_TIME;
            bool isRotate = false;

            //ドローンが移動した際にオブジェクトが傾く処理用
            float moveRotateSpeed = 2f;
            Quaternion frontMoveRotate = Quaternion.Euler(50, 0, 0);
            Quaternion leftMoveRotate = Quaternion.Euler(0, 0, 60);
            Quaternion rightMoveRotate = Quaternion.Euler(0, 0, -60);
            Quaternion backMoveRotate = Quaternion.Euler(-70, 0, 0);

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
            bool[] usingWeapons = new bool[(int)Weapon.NONE];    //使用中の武器
            [SerializeField, Tooltip("攻撃中の移動速度の低下率")] float atackingDownSpeed = 0.5f;   //攻撃中の移動速度の低下率

            //死亡処理用
            [SerializeField] GameObject explosion = null;
            [SerializeField] Transform droneObject = null;
            Quaternion deathRotate = Quaternion.Euler(28, -28, -28);
            float deathRotateSpeed = 2f;
            float gravityAccele = 1f;  //落下加速用
            float fallTime = 5.0f;   //死亡後の落下時間
            bool isDestroyFall = false;
            bool isDestroy = false;

            [Header("デバッグ用")]
            [SerializeField] Weapon isAtack = Weapon.NONE;


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

                if (isAtack == Weapon.MAIN)
                {
                    mainWeapon.Shot();
                }
                else if (isAtack == Weapon.SUB)
                {
                    subWeapon.Shot();
                }

                //移動
                baseAction.Move(cacheTransform.forward);

                //回転
                if (isRotate)
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

            void FixedUpdate()
            {
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
                            10f,              
                            _camera.forward,    
                            100f) 
                            .ToList();  //リスト化  
                hits = FilterTargetRaycast(hits);
                if(hits.Count > 0)
                {
                    StartRotate();
                    return;
                }
            
                angle = Vector3.zero;
                isRotate = false;
            }

            //リストから必要な要素だけ抜き取る
            List<RaycastHit> FilterTargetRaycast(List<RaycastHit> hits)
            {
                //不要な要素を除外する
                return hits.Where(h => !h.transform.CompareTag(TagNameManager.ITEM))    //アイテム除外
                           .Where(h => !h.transform.CompareTag(TagNameManager.BULLET))  //弾丸除外
                           .Where(h => !h.transform.CompareTag(TagNameManager.GIMMICK)) //ギミック除外
                           .Where(h => !h.transform.CompareTag(TagNameManager.JAMMING)) //ジャミングエリア除外
                           .Where(h => !h.transform.CompareTag(TagNameManager.JAMMING_BOT)) //ジャミングボット除外
                           .Where(h => !h.transform.CompareTag(TagNameManager.PLAYER))  //プレイヤー除外
                           .Where(h => !h.transform.CompareTag(TagNameManager.CPU))     //CPU除外
                           .ToList();  //リスト化 
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