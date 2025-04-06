using System;
using UnityEngine;
using UnityEngine.UI;

namespace Offline
{
    public class ShotgunWeapon : MonoBehaviour, IWeapon
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

                // 残弾UI作成
                _bulletUIs = new Image[_maxBulletNum];
                for (int i = 0; i < _maxBulletNum; i++)
                {
                    // UIの配置位置計算
                    float x = UI_WIDTH * i + UI_WIDTH * 0.5f;
                    float y = UI_HEIGHT * 0.5f;

                    // 背景UIの生成
                    RectTransform back = Instantiate(_bulletBackUI).GetComponent<RectTransform>();
                    back.SetParent(_bulletUICanvas.transform);
                    back.localPosition = new Vector3(x, y, 0);
                    back.localRotation = Quaternion.identity;

                    // 前面UIの生成
                    RectTransform front = Instantiate(_bulletFrontUI).GetComponent<RectTransform>();
                    front.SetParent(_bulletUICanvas.transform);
                    front.localPosition = new Vector3(x, y, 0);
                    front.localRotation = Quaternion.identity;

                    // 残弾UIに追加
                    _bulletUIs[i] = front.GetComponent<Image>();
                    _bulletUIs[i].fillAmount = 1f;
                }
            }
        }
        private Canvas _bulletUICanvas = null;

        public event EventHandler OnBulletFull;

        public event EventHandler OnBulletEmpty;

        /// <summary>
        /// 各残弾UIの縦幅
        /// </summary>
        private const int UI_HEIGHT = 100;

        /// <summary>
        /// 各残弾UIの横幅
        /// </summary>
        private const int UI_WIDTH = 50;

        [SerializeField, Tooltip("弾丸")]
        private GameObject _bullet = null;

        [SerializeField, Tooltip("残弾UI（前面）")]
        private Image _bulletFrontUI = null;

        [SerializeField, Tooltip("残弾UI（背面）")]
        private Image _bulletBackUI = null;

        [SerializeField, Tooltip("弾丸発射座標")]
        private Transform _shotPosition = null;

        [SerializeField, Tooltip("拡散力")]
        private float _angle = 3f;

        [SerializeField, Tooltip("拡散のブレ幅")]
        private float _angleDiff = 2f;

        [SerializeField, Tooltip("威力")] 
        private float _damage = 5.5f;

        [SerializeField, Tooltip("1秒間に発射する弾数")] 
        private float _shotPerSecond = 2f;

        [SerializeField, Tooltip("弾速")]
        private float _speed = 900f;

        [SerializeField, Tooltip("弾丸の生存時間（秒）")]
        private float _destroySec = 0.6f;

        [SerializeField, Tooltip("リキャスト時間（秒）")]
        private float _recastSec = 2f;

        [SerializeField, Tooltip("ストック可能な弾数")]
        private int _maxBulletNum = 5;

        /// <summary>
        /// 発射間隔（秒）
        /// </summary>
        private float _shotIntervalSec = 0;

        /// <summary>
        /// 前回発射からの経過時間
        /// </summary>
        private float _shotTimer = 0;

        /// <summary>
        /// リキャスト計測
        /// </summary>
        private float _recastTimer = 0;

        /// <summary>
        /// 残弾数
        /// </summary>
        private int _hasBulletNum = 0;

        /// <summary>
        /// 各残弾UI
        /// </summary>
        private Image[] _bulletUIs = null;

        private AudioSource _audioSource = null;

        public void Shot(GameObject target = null)
        {
            // 発射間隔チェック
            if (_shotTimer < _shotIntervalSec) return;

            // 残弾0の場合は撃たない
            if (_hasBulletNum <= 0) return;

            // 敵の位置に応じて発射角度を修正
            Quaternion rotation = ShotPosition.rotation;
            if (target != null)
            {
                // 追従対象への角度を計算
                Vector3 diff = target.transform.position - ShotPosition.position;
                rotation = Quaternion.LookRotation(diff);
            }

            // 弾丸発射
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    // 弾丸生成
                    GameObject bullet = Instantiate(_bullet, ShotPosition.position, rotation);
                    bullet.GetComponent<IBullet>().Shot(Owner, _damage, _speed);

                    // ブレ幅設定
                    Transform t = bullet.transform;
                    float diffX = _angle * x + UnityEngine.Random.Range(_angleDiff * -1, _angleDiff);  // 左右の角度
                    t.RotateAround(t.position, t.up, diffX);
                    float diffY = _angle * y + UnityEngine.Random.Range(_angleDiff * -1, _angleDiff);  // 上下の角度
                    t.RotateAround(t.position, t.right, diffY);

                    // 一定時間後弾丸削除
                    Destroy(bullet, _destroySec);
                }
            }

            // 弾丸発射SE再生
            _audioSource.Play();

            // 残弾UI更新
            if (_bulletUIs != null)
            {
                for (int i = _hasBulletNum - 1; i < _maxBulletNum; i++)
                {
                    _bulletUIs[i].fillAmount = 0;
                }
            }

            // 残弾-1
            _hasBulletNum--;

            // 前回発射時間リセット
            _shotTimer = 0;

            // 残弾が無くなった場合はイベント発火
            if (_hasBulletNum < 0)
            {
                OnBulletEmpty?.Invoke(this, EventArgs.Empty);
            }
        }

        private void Awake()
        {
            // 発射間隔計算
            _shotIntervalSec = 1.0f / _shotPerSecond;
            _shotTimer = _shotIntervalSec;
            _hasBulletNum = _maxBulletNum;

            // コンポーネント取得
            _audioSource = GetComponent<AudioSource>();
            _audioSource.clip = SoundManager.GetAudioClip(SoundManager.SE.SHOTGUN);
            _audioSource.volume = SoundManager.SEVolume;
        }

        private void Update()
        {
            // リキャスト時間経過したら弾数を1個補充
            if (_hasBulletNum < _maxBulletNum && _recastTimer >= _recastSec)
            {
                if (_bulletUIs != null)
                {
                    _bulletUIs[_hasBulletNum].fillAmount = 1f;
                }

                // 弾丸1個補充
                _hasBulletNum++;

                // リキャスト時間リセット
                _recastTimer = 0;

                // 全弾補充イベント
                if (_hasBulletNum >= _maxBulletNum)
                {
                    OnBulletFull?.Invoke(this, EventArgs.Empty);
                }
            }

            // UIにリキャスト反映
            if (_bulletUIs != null && _hasBulletNum < _maxBulletNum)
            {
                _bulletUIs[_hasBulletNum].fillAmount = _recastTimer / _recastSec;
            }

            // リキャストと発射間隔のカウント
            if (_recastTimer < _recastSec && _hasBulletNum < _maxBulletNum)
            {
                _recastTimer += Time.deltaTime;
            }
            if (_shotTimer < _shotIntervalSec)
            {
                _shotTimer += Time.deltaTime;
            }
        }
    }
}