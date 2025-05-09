using Common;
using UnityEngine;
using UnityEngine.UI;

namespace Drone
{
    public class DroneBoostComponent : MonoBehaviour, IDroneComponent
    {
        /// <summary>
        /// ブーストの加速度
        /// </summary>
        public float BoostAccele
        {
            get { return _boostAccele; }
            set { _boostAccele = value; }
        }

        /// <summary>
        /// 最大ブースト可能時間
        /// </summary>
        public float MaxBoostTime
        {
            get { return _maxBoostTime; }
            set
            {
                _maxBoostTime = value;
                _useGaugePerSec = 1 / _maxBoostTime;
            }
        }

        /// <summary>
        /// ブーストの最大リキャスト時間
        /// </summary>
        public float MaxBoostRecastTime
        {
            get { return _maxBoostRecastTime; }
            set
            {
                _maxBoostRecastTime = value;
                _addGaugePerSec = 1 / _maxBoostRecastTime;
            }
        }

        /// <summary>
        /// ブースト可能な最低ゲージ量
        /// </summary>
        private const float BOOSTABLE_MIN_GAUGE = 0.2f;

        /// <summary>
        /// ブーストゲージUI
        /// </summary>
        [SerializeField, Tooltip("ブーストゲージUI")]
        private Image _boostGaugeUI = null;

        [SerializeField, Tooltip("ブーストの加速度")]
        private float _boostAccele = 2.1f;

        [SerializeField, Tooltip("最大ブースト可能時間")]
        private float _maxBoostTime = 6.0f;

        [SerializeField, Tooltip("ブーストの最大リキャスト時間")]
        private float _maxBoostRecastTime = 8.0f;

        /// <summary>
        /// 現在のブーストゲージ量
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
        /// ブーストSE再生時に発行されたSE番号
        /// </summary>
        private int _boostSEId = -1;

        /// <summary>
        /// ブースト時に発行された移動速度変更ID
        /// </summary>
        private int _changeSpeedId = -1;

        /// <summary>
        /// ブースト中であるか
        /// </summary>
        private bool _isBoost = false;

        // コンポーネントキャッシュ
        private DroneMoveComponent _moveComponent = null;
        private DroneSoundComponent _soundComponent;

        public void Initialize() { }

        /// <summary>
        /// ブースト開始
        /// </summary>
        public void StartBoost()
        {
            // 既にブースト中の場合は何もしない
            if (_isBoost) return;

            // ブーストに必要な最低限のゲージがないとブースト開始できない
            if (_gaugeValue < BOOSTABLE_MIN_GAUGE) return;

            // 移動速度上昇
            _changeSpeedId = _moveComponent.ChangeMoveSpeedPercent(_boostAccele);

            // ブーストSE再生
            _boostSEId = _soundComponent.Play(SoundManager.SE.Boost, 0.15f, true);

            // ブーストフラグON
            _isBoost = true;
        }

        /// <summary>
        /// ブースト停止
        /// </summary>
        public void StopBoost()
        {
            // ブースト中でない場合は何もしない
            if (!_isBoost) return;

            // 移動速度を戻す
            _moveComponent.ResetMoveSpeed(_changeSpeedId);

            // ブーストSE停止
            _soundComponent.StopSE(_boostSEId);

            // ブーストフラグOFF
            _isBoost = false;
        }

        private void Awake()
        {
            // コンポーネントキャッシュ
            _moveComponent = GetComponent<DroneMoveComponent>();
            _soundComponent = GetComponent<DroneSoundComponent>();

            // プロパティ初期化
            MaxBoostTime = _maxBoostTime;
            MaxBoostRecastTime = _maxBoostRecastTime;
        }

        private void LateUpdate()
        {
            // ブースト中はゲージを減らす
            if (_isBoost)
            {
                _gaugeValue -= _useGaugePerSec * Time.deltaTime;

                // ゲージが無くなった場合はブースト停止
                if (_gaugeValue <= 0)
                {
                    _gaugeValue = 0;
                    StopBoost();
                }
            }
            else
            {
                // ブースト中でない場合はゲージ回復
                if (_gaugeValue < 1.0f)
                {
                    // ゲージを回復
                    _gaugeValue += _addGaugePerSec * Time.deltaTime;
                    if (_gaugeValue > 1f)
                    {
                        _gaugeValue = 1f;
                    }
                }
            }

            // UIに反映
            if (_boostGaugeUI != null)
            {
                _boostGaugeUI.fillAmount = _gaugeValue;
            }
        }
    }
}