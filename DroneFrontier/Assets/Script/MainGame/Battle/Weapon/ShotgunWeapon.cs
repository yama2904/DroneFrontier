using Common;
using Drone;
using Drone.Battle;
using Drone.Battle.Network;
using System;
using UnityEngine;
using UnityEngine.UI;

public class ShotgunWeapon : MonoBehaviour, IWeapon
{
    public const string ADDRESS_KEY = "ShotgunWeapon";

    public GameObject Owner { get; private set; } = null;

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
    /// 武器所有者Canvas
    /// </summary>
    private Canvas _bulletUICanvas = null;

    /// <summary>
    /// 各残弾UI
    /// </summary>s
    private Image[] _bulletUIs = null;

    private AudioSource _audioSource = null;

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
            // 残弾UI作成
            if (drone.Canvas != null)
            {
                _bulletUICanvas = drone.Canvas;
                _bulletUIs = new Image[_maxBulletNum];
                for (int i = 0; i < _maxBulletNum; i++)
                {
                    // UIの配置位置計算
                    float x = UI_WIDTH * i + UI_WIDTH * 0.5f;
                    float y = UI_HEIGHT * 0.5f;

                    // 背景UIの生成
                    Image back = Instantiate(_bulletBackUI);
                    back.transform.SetParent(_bulletUICanvas.transform);
                    back.transform.localPosition = new Vector3(x, y, 0);
                    back.transform.localRotation = Quaternion.identity;

                    // 前面UIの生成
                    Image front = Instantiate(_bulletFrontUI);
                    front.transform.SetParent(_bulletUICanvas.transform);
                    front.transform.localPosition = new Vector3(x, y, 0);
                    front.transform.localRotation = Quaternion.identity;

                    // 残弾UIに追加
                    _bulletUIs[i] = front.GetComponent<Image>();
                    _bulletUIs[i].fillAmount = 1f;
                }
            }

            // ブーストを多少強化
            DroneBoostComponent boost = owner.GetComponent<DroneBoostComponent>();
            boost.BoostAccele *= 1.2f;
            boost.MaxBoostTime *= 1.2f;
            boost.MaxBoostRecastTime *= 0.8f;
        }
    }

    public void Shot(GameObject target = null)
    {
        // 発射間隔チェック
        if (_shotTimer < _shotIntervalSec) return;

        // 残弾0の場合は撃たない
        if (_hasBulletNum <= 0) return;

        // 敵の位置に応じて発射角度を修正
        Quaternion rotation = _shotPosition.rotation;
        if (target != null)
        {
            // 追従対象への角度を計算
            Vector3 diff = target.transform.position - _shotPosition.position;
            rotation = Quaternion.LookRotation(diff);
        }

        // 弾丸発射
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                // 弾丸生成
                GameObject bullet = Instantiate(_bullet, _shotPosition.position, rotation);
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
        _audioSource.volume = SoundManager.MasterSEVolume;
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
        _audioSource.clip = SoundManager.GetAudioClip(SoundManager.SE.Shotgun);
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