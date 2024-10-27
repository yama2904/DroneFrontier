using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

namespace Offline
{
    public class DroneDamageComponent : MonoBehaviour
    {
        /// <summary>
        /// ダメージハンドラー
        /// </summary>
        /// <param name="sender">イベントオブジェクト</param>
        /// <param name="source">ダメージを与えたオブジェクト</param>
        /// <param name="damage">ダメージ量</param>
        public delegate void DamageHandler(DroneDamageComponent sender, GameObject source, float damage);

        /// <summary>
        /// ダメージイベント
        /// </summary>
        public event DamageHandler DamageEvent;

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
        /// ダメージ可能であるか
        /// </summary>
        private bool _damageable = false;

        /// <summary>
        /// 1フレーム内のダメージ回数
        /// </summary>
        private int _damageCount = 0;

        private void Awake()
        {
            // コンポーネント取得
            _drone = GetComponent<IBattleDrone>();
            _barrier = GetComponent<DroneBarrierComponent>();

            // ドローンが破壊された場合は本コンポーネントを停止
            _drone.DroneDestroyEvent += DroneDestroyEvent;
        }

        private void LateUpdate()
        {
            // ダメージ回数リセット
            _damageCount = 0;
        }

        private async void OnEnable()
        {
            // 起動直後は一定時間無敵
            _damageable = false;
            await UniTask.Delay(TimeSpan.FromSeconds(_notDamageableSec));
            _damageable = true;
        }

        /// <summary>
        /// ドローンへダメージを与える
        /// </summary>
        /// <param name="source">ダメージ元オブジェクト</param>
        /// <param name="value">ダメージ量</param>
        public void Damage(GameObject source, float value)
        {
            // ダメージ不可能の場合は処理しない
            if (!_damageable) return;

            // 1フレーム内のダメージ回数が最大に達している場合は処理しない
            if (_damageCount > _oneFrameMaxCount) return;

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
                _drone.HP -= value;
                Debug.Log($"{_drone.Name}:ドローンに{value}のダメージ\n残りHP:{_drone.HP}");
            }

            // ダメージ回数加算
            _damageCount++;

            // ダメージイベント発火
            DamageEvent?.Invoke(this, source, value);
        }

        /// <summary>
        /// ドローン破壊イベント
        /// </summary>
        /// <param name="o">イベントオブジェクト</param>
        /// <param name="e">イベント引数</param>
        private void DroneDestroyEvent(object o, EventArgs e)
        {
            // 本コンポーネント停止
            enabled = false;

            // イベント削除
            _drone.DroneDestroyEvent -= DroneDestroyEvent;
        }
    }
}