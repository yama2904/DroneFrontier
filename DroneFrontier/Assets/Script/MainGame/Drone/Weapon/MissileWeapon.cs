using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace Offline
{
    public class MissileWeapon : MonoBehaviour, IWeapon
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

                // 非同期で弾丸UI読み込み
                if (_bulletUICanvas != null)
                {
                    LoadBulletUIAsync(_bulletUICanvas).Forget();
                }
            }
        }
        private Canvas _bulletUICanvas = null;

        public event EventHandler OnBulletFull;

        public event EventHandler OnBulletEmpty;

        /// <summary>
        /// 弾丸オブジェクトのAddressKey
        /// </summary>
        private const string BULLET_ADDRESS_KEY = "MissileBullet";

        /// <summary>
        /// 残弾背景UIのAddressKey
        /// </summary>
        private const string BACK_UI_ADDRESS_KEY = "MissileBulletBackUI";

        /// <summary>
        /// 残弾前面UIのAddressKey
        /// </summary>
        private const string FRONT_UI_ADDRESS_KEY = "MissileBulletFrontUI";

        /// <summary>
        /// 各残弾UIの縦幅
        /// </summary>
        private const int UI_HEIGHT = 100;

        /// <summary>
        /// 各残弾UIの横幅
        /// </summary>
        private const int UI_WIDTH = 50;

        [SerializeField, Tooltip("表示用の装備ミサイル")]
        private GameObject _displayMissile = null;

        [SerializeField, Tooltip("弾丸発射座標")]
        private Transform _shotPosition = null;

        [SerializeField, Tooltip("威力")]
        private float _damage = 40f;

        [SerializeField, Tooltip("1秒間に発射する弾数")]
        private float _shotPerSecond = 0.2f;

        [SerializeField, Tooltip("弾速")]
        private float _speed = 500f;

        [SerializeField, Tooltip("着弾時間（秒）")]
        private float _explosionSec = 2f;

        [SerializeField, Tooltip("リキャスト時間（秒）")]
        private float _recastSec = 10f;

        [SerializeField, Tooltip("追従力")]
        private float _trackingPower = 2.5f;

        [SerializeField, Tooltip("ストック可能な弾数")]
        private int _maxBulletNum = 3;

        /// <summary>
        /// 弾丸プレハブ
        /// </summary>
        private static GameObject _bulletPrefab = null;

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

        public void Shot(GameObject target = null)
        {
            // 発射間隔チェック
            if (_shotTimer < _shotIntervalSec) return;

            // 残弾0の場合は撃たない
            if (_hasBulletNum <= 0) return;

            // 弾丸生成
            IBullet bullet = Instantiate(_bulletPrefab, ShotPosition.position, ShotPosition.rotation).GetComponent<IBullet>();
            bullet.Shot(Owner, _damage, _speed, _trackingPower, target);
            (bullet as MissileBullet).ExplosionSec = _explosionSec; // ※要検討

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

            // 表示用ミサイルを非表示
            _displayMissile.SetActive(false);

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

            // 弾丸オブジェクト読み込み
            if (_bulletPrefab == null)
            {
                Addressables.LoadAssetAsync<GameObject>(BULLET_ADDRESS_KEY).Completed += handle =>
                {
                    _bulletPrefab = handle.Result;
                    Addressables.Release(handle);
                };
            }
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

            // 発射可能になったらミサイル表示
            if (_shotTimer >= _shotIntervalSec && !_displayMissile.activeSelf)
            {
                _displayMissile.SetActive(true);
            }

            // リキャストと発射間隔のカウント
            if (_recastTimer < _recastSec)
            {
                _recastTimer += Time.deltaTime;
            }
            if (_shotTimer < _shotIntervalSec)
            {
                _shotTimer += Time.deltaTime;
            }
        }

        /// <summary>
        /// 非同期で弾丸UIを読み込んで表示
        /// </summary>
        /// <param name="canvas">表示する弾丸UIの親Canvas</param>
        private async UniTask LoadBulletUIAsync(Canvas canvas)
        {
            // 残弾UI読み込み
            var handleBack = Addressables.LoadAssetAsync<GameObject>(BACK_UI_ADDRESS_KEY);
            var handleFront = Addressables.LoadAssetAsync<GameObject>(FRONT_UI_ADDRESS_KEY);
            await UniTask.WhenAll(handleBack.ToUniTask(), handleFront.ToUniTask());

            // 残弾UI取り出し
            RectTransform bulletUIBack = handleBack.Result.GetComponent<RectTransform>();
            RectTransform bulletUIFront = handleFront.Result.GetComponent<RectTransform>();

            // リソース解放
            Addressables.Release(handleBack);
            Addressables.Release(handleFront);

            // 残弾UI作成
            _bulletUIs = new Image[_maxBulletNum];
            for (int i = 0; i < _maxBulletNum; i++)
            {
                // UIの配置位置計算
                float x = UI_WIDTH * i + UI_WIDTH * 0.5f;
                float y = UI_HEIGHT * 0.5f;

                // 背景UIの生成
                RectTransform back = Instantiate(bulletUIBack).GetComponent<RectTransform>();
                back.SetParent(canvas.transform);
                back.localPosition = new Vector3(x, y, 0);
                back.localRotation = Quaternion.identity;

                // 前面UIの生成
                RectTransform front = Instantiate(bulletUIFront).GetComponent<RectTransform>();
                front.SetParent(canvas.transform);
                front.localPosition = new Vector3(x, y, 0);
                front.localRotation = Quaternion.identity;

                // 残弾UIに追加
                _bulletUIs[i] = front.GetComponent<Image>();
                _bulletUIs[i].fillAmount = 1f;
            }
        }
    }
}