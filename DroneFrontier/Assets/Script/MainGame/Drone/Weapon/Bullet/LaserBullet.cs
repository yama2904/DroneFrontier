using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Offline
{
    public class LaserBullet : MonoBehaviour, IBullet
    {
        public GameObject Shooter { get; private set; } = null;

        /// <summary>
        /// チャージが完了してレーザー発射中であるか
        /// </summary>
        public bool IsShootingLaser { get; private set; } = false;

        /// <summary>
        /// チャージ時間（秒）
        /// </summary>
        public float ChargeSec
        {
            get { return _chargeSec; }
            set 
            {
                _chargeSec = value;
                _addChargeParticlePerSec = MAX_RATE_OVER_TIME / _chargeSec;
            }
        }

        /// <summary>
        /// レーザーの半径
        /// </summary>
        public float LaserRadius
        {
            get { return _laserRadius; }
            set
            { 
                _laserRadius = value;
                _laserLineObject.localScale = new Vector3(_laserLineObject.localScale.x, _laserRadius, _laserRadius);
            }
        }

        /// <summary>
        /// レーザーの射程
        /// </summary>
        public float LaserRange
        {
            get { return _laserRange; }
            set 
            {
                _laserRange = value;

                // レーザーオブジェクトに射程適用
                ApplyLaserLineLength(_laserRange);
            }
        }

        /// <summary>
        /// 1秒間にヒットする回数
        /// </summary>
        public float HitPerSecond
        {
            get { return _hitPerSecond; }
            set
            {
                _hitPerSecond = value;
                _hitInterval = 1 / value;
            }
        }

        /// <summary>
        /// チャージのパーティクルのrateOverTime最大値
        /// </summary>
        private const int MAX_RATE_OVER_TIME = 128;

        [SerializeField, Tooltip("チャージパーティクル")]
        private ParticleSystem _chargeParticle = null;

        [SerializeField, Tooltip("レーザーオブジェクト")]
        private Transform _laserLineObject = null;

        [SerializeField, Tooltip("レーザー終端オブジェクト")]
        private Transform _laserEndObject = null;

        [SerializeField, Tooltip("チャージ時間（秒）")]
        private float _chargeSec = 3f;

        [SerializeField, Tooltip("レーザーの半径")]
        private float _laserRadius = 500f;

        [SerializeField, Tooltip("レーザーの射程")]
        private float _laserRange = 1000f;

        [SerializeField, Tooltip("1秒間にヒットする回数")]
        private float _hitPerSecond = 6f;

        /// <summary>
        /// 再生/停止させる全てのParticleSystem
        /// </summary>
        private List<ParticleSystem> _particleList = new List<ParticleSystem>();

        /// <summary>
        /// チャージ用パーティクルのEmissionModuleキャッシュ
        /// </summary>
        ParticleSystem.EmissionModule _chargeEmission;

        /// <summary>
        /// ヒット間隔
        /// </summary>
        private float _hitInterval = 0;

        /// <summary>
        /// 各オブジェクトごとのヒット間隔計測用タイマー
        /// </summary>
        private Dictionary<Transform, float> _hitTimerMap = new Dictionary<Transform, float>();

        /// <summary>
        /// 1秒ごとに増加させるチャージパーティクルのrateOverTime
        /// </summary>
        private float _addChargeParticlePerSec;

        /// <summary>
        /// Shotメソッド呼び出し履歴
        /// </summary>
        private ValueHistory<bool> _shotHistory = new ValueHistory<bool>();

        /// <summary>
        /// チャージ中であるか
        /// </summary>
        private bool _isCharging = true;

        // コンポーネントキャッシュ
        Transform _transform = null;
        AudioSource _audioSource = null;

        public void Shot(GameObject shooter, float damage, float speed, float trackingPower = 0, GameObject target = null)
        {
            Shooter = shooter;

            if (_isCharging)
            {
                // --- チャージ start

                // チャージ開始時はチャージパーティクルとチャージSE再生
                if (!_shotHistory.PreviousValue)
                {
                    _chargeParticle.Play();

                    _audioSource.clip = SoundManager.GetAudioClip(SoundManager.SE.BeamChange);
                    _audioSource.time = 0.2f;
                    _audioSource.volume = SoundManager.MasterSEVolume * 0.15f;
                    _audioSource.Play();
                }

                // 徐々にチャージのエフェクトを増す
                _chargeEmission.rateOverTime = _chargeEmission.rateOverTime.constant + _addChargeParticlePerSec * Time.deltaTime;

                // チャージ完了したら発射
                if (_chargeEmission.rateOverTime.constant > MAX_RATE_OVER_TIME)
                {
                    // チャージを止める
                    _chargeParticle.Stop();
                    _audioSource.Stop();

                    // チャージ以外のパーティクル再生
                    foreach (ParticleSystem particle in _particleList)
                    {
                        if (particle == _chargeParticle) continue;
                        particle.Play();
                    }

                    // レーザー発射SE再生
                    _audioSource.clip = SoundManager.GetAudioClip(SoundManager.SE.Beam);
                    _audioSource.volume = SoundManager.MasterSEVolume * 0.05f;
                    _audioSource.loop = true;
                    _audioSource.Play();

                    // チャージフラグ停止
                    _isCharging = false;

                    // レーザー発射フラグ更新
                    IsShootingLaser = true;
                }

                // --- チャージ end
            }
            else
            {
                // --- レーザー発射 start

                // ターゲットが存在する場合は徐々にレーザーを対象へ向ける
                if (target != null)
                {
                    // 敵の方向を計算
                    Vector3 diff = target.transform.position - _transform.position;
                    Quaternion rotation = Quaternion.LookRotation(diff);
                    _transform.rotation = Quaternion.Slerp(_transform.rotation, rotation, trackingPower);
                }
                else
                {
                    // ターゲットが存在しない場合は正面に戻す
                    _transform.localRotation = Quaternion.Slerp(_transform.localRotation, Quaternion.identity, trackingPower);
                }

                // レーザーの射線上にヒットした全てのオブジェクトを調べる
                RaycastHit[] hits = Physics.SphereCastAll(
                                        _transform.position,     // レーザーの発射座標
                                        _laserRadius * 0.01f,    // レーザーの半径
                                        _transform.forward,      // レーザーの正面
                                        _laserRange);            // 射程

                // ターゲット取得
                bool exists = FilterTarget(hits, out RaycastHit hit);

                // ターゲットが存在する場合はヒット処理
                if (exists) {
                    // ヒットしたオブジェクトの距離とレーザーの長さを合わせる
                    ApplyLaserLineLength(hit.distance);

                    // ヒット間隔チェック
                    Transform t = hit.transform;
                    if (!_hitTimerMap.ContainsKey(t))
                    {
                        // ダメージ可能インターフェースが実装されている場合はダメージを与える
                        if (hit.transform.TryGetComponent(out IDamageable damageable))
                        {
                            damageable.Damage(shooter, damage);

                            // ヒット済みとしてマップに格納
                            _hitTimerMap.Add(t, 0);
                        }
                    }
                }
                else
                {
                    // ターゲットが存在しない場合はレーザーの長さを射程に戻す
                    ApplyLaserLineLength(_laserRange);
                }

                // --- レーザー発射 end
            }

            _shotHistory.CurrentValue = true;
        }

        private void Awake()
        {
            //コンポーネントキャッシュ
            _transform = transform;
            _chargeEmission = _chargeParticle.emission;
            _audioSource = GetComponent<AudioSource>();

            // 全てのParticleSystemコンポーネント取得
            _particleList = GetParticleObjects(transform);

            // プロパティ初期化
            ChargeSec = _chargeSec;
            LaserRadius = _laserRadius;
            LaserRange = _laserRange;
            HitPerSecond = _hitPerSecond;
        }

        private void Start()
        {
            // レーザー停止で初期化
            StopShot();
        }

        private void FixedUpdate()
        {
            // ヒット間隔計測
            List<Transform> keyList = _hitTimerMap.Keys.ToList();
            foreach (Transform key in keyList)
            {
                // ヒット可能になった場合はマップから削除
                if (_hitTimerMap[key] > _hitInterval)
                {
                    _hitTimerMap.Remove(key);
                    continue;
                }

                _hitTimerMap[key] += Time.deltaTime;
            }
        }

        private void LateUpdate()
        {
            // レーザー発射停止チェック
            if (!_shotHistory.CurrentValue && _shotHistory.PreviousValue)
            {
                StopShot();
            }

            // Shotメソッド呼び出し履歴更新
            _shotHistory.UpdateCurrentValue(false);
        }

        /// <summary>
        /// 指定されたオブジェクトのうちヒット可能、かつ最も近いオブジェクトを返す
        /// </summary>
        /// <param name="hits"></param>
        /// <param name="target"></param>
        /// <returns>ヒット可能オブジェクトが存在しない場合はfalse</returns>
        private bool FilterTarget(RaycastHit[] hits, out RaycastHit target)
        {
            // outパラメータ初期化
            target = new RaycastHit()
            {
                distance = float.MaxValue
            };

            bool exists = false;
            foreach (RaycastHit hit in hits)
            {
                // Transformキャッシュ
                Transform t = hit.transform;

                // タグを基にヒット対象から除外
                string tag = t.tag;
                if (tag == TagNameConst.ITEM) continue;             // アイテム除外
                if (tag == TagNameConst.BULLET) continue;           // 弾丸除外
                if (tag == TagNameConst.GIMMICK) continue;          // ギミック除外
                if (tag == TagNameConst.JAMMING_AREA) continue;     // ジャミングエリア除外
                if (tag == TagNameConst.NOT_COLLISION) continue;

                // レーザー発射元オブジェクトを非ダメージ指定されている場合はヒットしない
                if (t.TryGetComponent(out IDamageable damageable))
                {
                    if (damageable.NoDamageObject == Shooter) continue;
                }

                // 最小距離の場合は取得
                if (target.distance > hit.distance)
                {
                    target = hit;
                }

                exists = true;
            }

            return exists;
        }

        /// <summary>
        /// 指定したレーザーの長さをオブジェクトに反映させる
        /// </summary>
        /// <param name="length">レーザーの長さ</param>
        private void ApplyLaserLineLength(float length)
        {
            // レーザーの長さ反映
            Vector3 scale = _laserLineObject.localScale;
            _laserLineObject.localScale = new Vector3(length, scale.y, scale.z);

            // レーザーの末端の位置を合わせて移動
            _laserEndObject.position = _transform.position + _transform.forward * length;
        }


        /// <summary>
        /// レーザー停止
        /// </summary>
        private void StopShot()
        {
            // 全てのパーティクル停止
            foreach (ParticleSystem particle in _particleList)
            {
                particle.Stop();
            }

            // チャージエフェクト量初期化
            _chargeEmission.rateOverTime = 0;

            // レーザー音停止
            _audioSource.Stop();

            // フラグの初期化
            _isCharging = true;
            IsShootingLaser = false;
        }

        /// <summary>
        /// 指定したTransformの全ての子孫からParticleSystemコンポーネントを取得
        /// </summary>
        /// <param name="parent">子孫を取得する親</param>
        /// <returns>ParticleSystemコンポーネント</returns>
        private List<ParticleSystem> GetParticleObjects(Transform parent)
        {
            List<ParticleSystem> children = new List<ParticleSystem>();
            foreach (Transform child in parent)
            {
                // ParticleSystemコンポーネントを持っている場合は取得
                if (child.TryGetComponent(out ParticleSystem particle))
                {
                    children.Add(particle);
                }

                // 孫オブジェクト取得
                children.AddRange(GetParticleObjects(child));
            }

            return children;
        }
    }
}