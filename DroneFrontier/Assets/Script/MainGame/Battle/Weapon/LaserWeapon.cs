using Battle.Weapon.Bullet;
using Common;
using Drone.Battle;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Battle.Weapon
{
    public class LaserWeapon : MonoBehaviour, IWeapon
    {
        public const string ADDRESS_KEY = "LaserWeapon";

        /// <summary>
        /// レーザー攻撃中のスピード低下率
        /// </summary>
        private const float SPEED_DOWN_PER = 0.25f;

        /// <summary>
        /// 発射可能な最低ゲージ量
        /// </summary>
        private const float SHOOTABLE_MIN_GAUGE = 0.2f;

        public GameObject Owner { get; private set; } = null;

        public event EventHandler OnBulletFull;

        public event EventHandler OnBulletEmpty;

        [SerializeField, Tooltip("弾丸")]
        private LaserBullet _bullet = null;

        [SerializeField, Tooltip("残弾UI（前面）")]
        private Image _bulletGaugeUI = null;

        [SerializeField, Tooltip("残弾UI（背面）")]
        private Image _bulletFrameUI = null;

        [SerializeField, Tooltip("レーザーの最大発射可能時間")]
        private float _maxShotTime = 10f;

        [SerializeField, Tooltip("レーザーの最大リキャスト時間")]
        private float _maxRecastTime = 8f;

        [SerializeField, Tooltip("威力")]
        private float _damage = 5f;

        [SerializeField, Tooltip("レーザーが敵を追う速度")]
        private float _trackingPower = 0.01f;

        /// <summary>
        /// 武器所有者Canvas
        /// </summary>
        private Canvas _bulletUICanvas = null;

        /// <summary>
        /// レーザーゲージを表示するUI
        /// </summary>
        private Image _laserGaugeUI = null;

        /// <summary>
        /// 現在のレーザーゲージ量
        /// </summary>
        private float _gaugeValue = 1f;

        /// <summary>
        /// 1秒ごとに消費するゲージ量
        /// </summary>
        private float _useGaugePerSec = 0;

        /// <summary>
        /// 1秒ごとに回復するゲージ量
        /// </summary>
        private float _addGaugePerSec = 0;

        /// <summary>
        /// Shotメソッド呼び出し履歴
        /// </summary>
        private ValueHistory<bool> _shotHistory = new ValueHistory<bool>();

        public string GetAddressKey()
        {
            return ADDRESS_KEY;
        }

        public void Initialize(GameObject owner)
        {
            Owner = owner;

            // ドローンの場合
            if (owner.TryGetComponent<IBattleDrone>(out var drone))
            {
                // レーザーゲージUI生成
                if (drone.BulletCanvas != null)
                {
                    _bulletUICanvas = drone.BulletCanvas;
                    _laserGaugeUI = Instantiate(_bulletGaugeUI);
                    Image gaugeFrameUI = Instantiate(_bulletFrameUI);

                    // Canvasを親に設定
                    _laserGaugeUI.transform.SetParent(_bulletUICanvas.transform, false);
                    gaugeFrameUI.transform.SetParent(_bulletUICanvas.transform, false);

                    // レーザーゲージ量をUIに反映
                    _laserGaugeUI.fillAmount = _gaugeValue;
                }

                // 攻撃中の移動低下率設定
                owner.GetComponent<DroneWeaponComponent>().SubSpeedDownPer = SPEED_DOWN_PER;
            }
        }

        public void Shot(GameObject target = null)
        {
            // 発射に必要な最低限のゲージがないと発射開始できない
            if (!_shotHistory.PreviousValue)
            {
                if (_gaugeValue < SHOOTABLE_MIN_GAUGE)
                {
                    return;
                }
            }

            _bullet.Shot(Owner, _damage, 0, _trackingPower, target);
            _shotHistory.CurrentValue = true;

            // チャージが完了してレーザーが発射されている間はゲージを減らす
            if (_bullet.IsShootingLaser)
            {
                _gaugeValue -= _useGaugePerSec * Time.deltaTime;

                // ゲージが無くなった場合はレーザー停止
                if (_gaugeValue <= 0)
                {
                    _gaugeValue = 0;
                    _shotHistory.CurrentValue = false;

                    // 残弾無しイベント発火
                    OnBulletEmpty?.Invoke(this, EventArgs.Empty);
                }

                // UIに反映
                if (_laserGaugeUI != null)
                {
                    _laserGaugeUI.fillAmount = _gaugeValue;
                }
            }
        }

        private void Awake()
        {
            // 1秒ごとのゲージ消費/回復量を事前に計算
            _useGaugePerSec = 1 / _maxShotTime;
            _addGaugePerSec = 1 / _maxRecastTime;
        }


        private void LateUpdate()
        {
            // レーザーを発射していない場合はゲージ回復
            if (!_shotHistory.CurrentValue)
            {
                if (_gaugeValue < 1.0f)
                {
                    // ゲージを回復
                    _gaugeValue += _addGaugePerSec * Time.deltaTime;
                    if (_gaugeValue > 1f)
                    {
                        _gaugeValue = 1f;

                        // 全弾補充イベント発火
                        OnBulletFull?.Invoke(this, EventArgs.Empty);
                    }

                    // UIに反映
                    if (_laserGaugeUI != null)
                    {
                        _laserGaugeUI.fillAmount = _gaugeValue;
                    }
                }
            }

            // Shotメソッド呼び出し履歴更新
            _shotHistory.UpdateCurrentValue(false);
        }
    }
}