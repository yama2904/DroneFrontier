using Common;
using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

namespace Drone.Battle
{
    public class DroneDamageComponent : MonoBehaviour, IDamageable, IDroneComponent
    {
        public GameObject Owner => gameObject;

        public event DamageHandler OnDamage;

        /// <summary>
        /// ダメージ可能であるか
        /// </summary>
        internal bool _damageable = true;

        [SerializeField, Tooltip("復活後の無敵時間（秒）")]
        private int _notDamageableSec = 4;

        [SerializeField, Tooltip("1フレームでの最大ダメージ回数")]
        private int _oneFrameMaxCount = 8;

        /// <summary>
        /// ダメージ先ドローン
        /// </summary>
        private IBattleDrone _drone = null;

        /// <summary>
        /// ダメージ先ドローンのバリア
        /// </summary>
        private DroneBarrierComponent _barrier = null;

        /// <summary>
        /// 1フレーム内のダメージ回数
        /// </summary>
        private int _damageCount = 0;

        /// <summary>
        /// 初期化済みであるか
        /// </summary>
        private bool _isInitialized = false;

        public async void Initialize() 
        {
            // 起動直後は一定時間無敵
            await UniTask.Delay(TimeSpan.FromSeconds(_notDamageableSec));
            _isInitialized = true;
        }

        /// <summary>
        /// ドローンへダメージを与える
        /// </summary>
        /// <param name="source">ダメージ元オブジェクト</param>
        /// <param name="value">ダメージ量</param>
        public bool Damage(GameObject source, float value)
        {
            // 初期化後にダメージ可能
            if (!_isInitialized) return false;

            // 自分の攻撃は受けない
            if (source == gameObject) return false;

            // ダメージ不可能の場合は処理しない
            if (!_damageable) return false;

            // 1フレーム内のダメージ回数が最大に達している場合は処理しない（呼び出し側からはダメージ成功と見えるようにする）
            if (_damageCount > _oneFrameMaxCount) return true;

            // 小数点第2以下切り捨て
            value = Useful.Floor(value, 1);

            // バリアが破壊されていない場合はバリアにダメージ
            if (_barrier.HP > 0)
            {
                _barrier.Damage(value);
            }
            else
            {
                // バリアが破壊されている場合はドローン本体へダメージ
                _drone.Damage(value);
            }

            // ダメージ回数加算
            _damageCount++;

            // ダメージイベント発火
            OnDamage?.Invoke(this, source, value);

            return true;
        }

        internal void Damage(float value)
        {
            // 小数点第2以下切り捨て
            value = Useful.Floor(value, 1);

            // バリアが破壊されていない場合はバリアにダメージ
            if (_barrier.HP > 0)
            {
                _barrier.Damage(value);
            }
            else
            {
                // バリアが破壊されている場合はドローン本体へダメージ
                _drone.Damage(value);
            }
        }

        private void Awake()
        {
            // コンポーネント取得
            _drone = GetComponent<IBattleDrone>();
            _barrier = GetComponent<DroneBarrierComponent>();

            // ドローンが破壊された場合は本コンポーネントを停止
            _drone.OnDroneDestroy += OnDroneDestroy;
        }

        private void LateUpdate()
        {
            // ダメージ回数リセット
            _damageCount = 0;
        }

        /// <summary>
        /// ドローン破壊イベント
        /// </summary>
        /// <param name="o">イベントオブジェクト</param>
        /// <param name="e">イベント引数</param>
        private void OnDroneDestroy(object o, EventArgs e)
        {
            // イベント削除
            _drone.OnDroneDestroy -= OnDroneDestroy;
        }
    }
}