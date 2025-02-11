using Cysharp.Threading.Tasks;
using Offline;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Network
{
    public class NetworkBattleDrone : MyNetworkBehaviour, IBattleDrone
    {
        #region public

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
                    // 小数点第2以下切り捨て
                    _hp = Useful.Floor(value, 1);
                }
                else
                {
                    // HPが0になったら破壊処理
                    _hp = 0;
                    Destroy().Forget();
                }
            }
        }

        /// <summary>
        /// 現在のストック数
        /// </summary>
        public int StockNum
        {
            get { return _stockNum; }
            set
            {
                _stockNum = value;
                _stockText.text = value.ToString();
            }
        }

        /// <summary>
        /// ドローンのサブ武器
        /// </summary>
        public WeaponType SubWeapon { get; set; }

        /// <summary>
        /// ロックオン可能であるか
        /// </summary>
        public bool IsLockableOn { get; } = true;

        /// <summary>
        /// ロックオン不可にするオブジェクト
        /// </summary>
        public List<GameObject> NotLockableOnList { get; } = new List<GameObject>();

        public IRadarable.ObjectType Type => IRadarable.ObjectType.Enemy;

        public bool IsRadarable => true;

        public List<GameObject> NotRadarableList { get; } = new List<GameObject>();

        /// <summary>
        /// 操作するドローンか
        /// </summary>
        public bool IsControl
        {
            get { return _isControl; }
            set
            {
                _camera.depth = 5;
                _isControl = value;
            }
        }

        public Camera Camera => _camera;

        /// <summary>
        /// ドローン破壊イベント
        /// </summary>
        public event EventHandler DroneDestroyEvent;

        #endregion

        /// <summary>
        /// 所持アイテム番号
        /// </summary>
        private enum ItemNum
        {
            /// <summary>
            /// アイテム1
            /// </summary>
            Item1,

            /// <summary>
            /// アイテム2
            /// </summary>
            Item2
        }

        /// <summary>
        /// 死亡時の回転量
        /// </summary>
        private readonly Quaternion DEATH_ROTATE = Quaternion.Euler(28, -28, -28);

        /// <summary>
        /// 死亡時の回転速度
        /// </summary>
        private const float DEATH_ROTATE_SPEED = 2f;

        /// <summary>
        /// 死亡時の落下時間
        /// </summary>
        private const float DEATH_FALL_TIME = 2.5f;

        [SerializeField, Tooltip("ドローン本体オブジェクト")]
        private Transform _droneObject = null;

        [SerializeField, Tooltip("ドローン死亡時の爆発オブジェクト")]
        private GameObject _explosion = null;

        [SerializeField, Tooltip("ストック数を表示するTextコンポーネント")]
        private Text _stockText = null;

        [SerializeField, Tooltip("オブジェクト探索コンポーネント")]
        private ObjectSearchComponent _searchComponent = null;

        [SerializeField, Tooltip("ドローンのHP")]
        private float _hp = 100f;

        [SerializeField, Tooltip("ストック数")]
        private int _stockNum = 2;

        /// <summary>
        /// 死亡フラグ
        /// </summary>
        private bool _isDestroy = false;

        // コンポーネントキャッシュ
        Rigidbody _rigidbody = null;
        Animator _animator = null;
        DroneMoveComponent _moveComponent = null;
        DroneRotateComponent _rotateComponent = null;
        DroneSoundComponent _soundComponent = null;
        DroneLockOnComponent _lockOnComponent = null;
        DroneRadarComponent _radarComponent = null;
        DroneItemComponent _itemComponent = null;
        DroneWeaponComponent _weaponComponent = null;
        DroneBoostComponent _boostComponent = null;

        private bool _isControl = false;

        [SerializeField, Tooltip("カメラ")]
        private Camera _camera = null;

        private void Start()
        {
        
        }

        private void Update()
        {
        
        }



        /// <summary>
        /// オブジェクト探索イベント
        /// </summary>
        /// <param name="other">発見オブジェクト</param>
        private void ObjectSearchEvent(Collider other)
        {
            // 死亡処理中は操作不可
            if (_isDestroy) return;

            // Eキーでアイテム取得
            if (Input.GetKey(KeyCode.E))
            {
                if (other.CompareTag(TagNameConst.ITEM))
                {
                    SpawnItem item = other.GetComponent<SpawnItem>();
                    if (_itemComponent.SetItem(item))
                    {
                        Destroy(other.gameObject);
                    }
                }
            }
        }

        /// <summary>
        /// 指定した番号のアイテム使用
        /// </summary>
        /// <param name="item">使用するアイテム番号</param>
        private void UseItem(ItemNum item)
        {
            // アイテム枠にアイテムを持っていたら使用
            if (_itemComponent.UseItem((int)item))
            {
                _soundComponent.PlayOneShot(SoundManager.SE.USE_ITEM, SoundManager.SEVolume);
            }
        }

        /// <summary>
        /// 死亡処理
        /// </summary>
        private async UniTask Destroy()
        {
            // 死亡フラグを立てる
            _isDestroy = true;

            // 移動コンポーネント停止
            _moveComponent.enabled = false;

            // ロックオン・レーダー解除
            _lockOnComponent.StopLockOn();
            _radarComponent.StopRadar();

            // 死亡SE再生
            _soundComponent.PlayOneShot(SoundManager.SE.DEATH, SoundManager.SEVolume);

            // 一定時間経過してから爆破
            await UniTask.Delay(TimeSpan.FromSeconds(DEATH_FALL_TIME));

            // ドローンの非表示
            _droneObject.gameObject.SetActive(false);

            // 当たり判定も消す
            GetComponent<Collider>().enabled = false;

            // 爆破生成
            _explosion.SetActive(true);

            // Update停止
            enabled = false;

            // 爆破後一定時間でオブジェクト破棄
            await UniTask.Delay(5000);

            // ドローン破壊イベント通知
            DroneDestroyEvent?.Invoke(this, EventArgs.Empty);

            // オブジェクト破棄
            Destroy(gameObject);
        }
    }
}