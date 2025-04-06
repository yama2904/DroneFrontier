using System;
using UnityEngine;
using UnityEngine.UI;

namespace Offline
{
    public class LaserWeapon : MonoBehaviour, IWeapon
    {
        public GameObject Owner { get; set; } = null;

        public Transform ShotPosition
        {
            get { return _shotPosition; }
            set { _shotPosition = value; }
        }

        public Canvas BulletUICanvas
        {
            get
            {
                return _bulletUICanvas;
            }
            set
            {
                _bulletUICanvas = value;
                if (_bulletUICanvas == null) return;

                // レーザーゲージUI生成
                _laserGaugeUI = Instantiate(_bulletGaugeUI);
                Image gaugeFrameUI = Instantiate(_bulletFrameUI);

                // Canvasを親に設定
                _laserGaugeUI.transform.SetParent(_bulletUICanvas.transform, false);
                gaugeFrameUI.transform.SetParent(_bulletUICanvas.transform, false);

                // レーザーゲージ量をUIに反映
                _laserGaugeUI.fillAmount = _gaugeValue;
            }
        }
        private Canvas _bulletUICanvas = null;

        public event EventHandler OnBulletFull;

        public event EventHandler OnBulletEmpty;

        /// <summary>
        /// 発射可能な最低ゲージ量
        /// </summary>
        private const float SHOOTABLE_MIN_GAUGE = 0.2f;

        [SerializeField, Tooltip("弾丸")]
        private LaserBullet _bullet = null;

        [SerializeField, Tooltip("残弾UI（前面）")]
        private Image _bulletGaugeUI = null;

        [SerializeField, Tooltip("残弾UI（背面）")]
        private Image _bulletFrameUI = null;

        [SerializeField, Tooltip("レーザー発射座標")]
        private Transform _shotPosition = null;

        [SerializeField, Tooltip("レーザーの最大発射可能時間")]
        private float _maxShotTime = 10f;

        [SerializeField, Tooltip("レーザーの最大リキャスト時間")]
        private float _maxRecastTime = 8f;

        [SerializeField, Tooltip("威力")] 
        private float _damage = 5f;

        [SerializeField, Tooltip("レーザーが敵を追う速度")]
        private float _trackingPower = 0.01f;

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
        /// Shotメソッド呼び出し履歴<br/>
        /// [0]:現在のフレーム<br/>
        /// [1]:1フレーム前
        /// </summary>
        private bool[] _isShooted = new bool[2];

        public void Shot(GameObject target = null)
        {
            // 発射に必要な最低限のゲージがないと発射開始できない
            if (!_isShooted[1])
            {
                if (_gaugeValue < SHOOTABLE_MIN_GAUGE)
                {
                    return;
                }
            }

            _bullet.Shot(Owner, _damage, 0, _trackingPower, target);
            _isShooted[0] = true;

            // チャージが完了してレーザーが発射されている間はゲージを減らす
            if (_bullet.IsShootingLaser)
            {
                _gaugeValue -= _useGaugePerSec * Time.deltaTime;

                // ゲージが無くなった場合はレーザー停止
                if (_gaugeValue <= 0)
                {
                    _gaugeValue = 0;
                    _isShooted[0] = false;

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
            if (!_isShooted[0])
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
            _isShooted[1] = _isShooted[0];
            _isShooted[0] = false;
        }
    }
}