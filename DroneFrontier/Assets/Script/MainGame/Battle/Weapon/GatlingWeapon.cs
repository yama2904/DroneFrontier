using Common;
using Drone.Battle;
using System;
using UnityEngine;

public class GatlingWeapon : MonoBehaviour, IWeapon
{
    public const string ADDRESS_KEY = "GatlingWeapon";

    public GameObject Owner { get; private set; } = null;

    public event EventHandler OnBulletFull;

    public event EventHandler OnBulletEmpty;

    [SerializeField, Tooltip("弾丸")]
    private GameObject _bullet = null;

    [SerializeField, Tooltip("弾丸発射座標")]
    private Transform _shotPosition = null;

    [SerializeField, Tooltip("威力")]
    private float _damage = 1f;

    [SerializeField, Tooltip("1秒間に発射する弾数")]
    private float _shotPerSecond = 10f;

    [SerializeField, Tooltip("弾速")]
    private float _speed = 800f;

    [SerializeField, Tooltip("弾丸の生存時間（秒）")]
    private float _destroySec = 1f;

    [SerializeField, Tooltip("追従力")]
    private float _trackingPower = 3f;

    /// <summary>
    /// AudioSourceコンポーネント
    /// </summary>
    private AudioSource _audioSource = null;

    /// <summary>
    /// 発射間隔（秒）
    /// </summary>
    private float _shotIntervalSec = 0;

    /// <summary>
    /// 前回発射からの経過時間
    /// </summary>
    private float _shotTimer = 0;

    public string GetAddressKey()
    {
        return ADDRESS_KEY;
    }

    public void Initialize(GameObject owner)
    {
        Owner = owner;
    }

    public void Shot(GameObject target = null)
    {
        // 前回発射からの発射間隔チェック
        if (_shotTimer < _shotIntervalSec) return;

        // 弾丸発射
        GameObject bullet = Instantiate(_bullet, _shotPosition.position, _shotPosition.rotation);
        bullet.GetComponent<IBullet>().Shot(Owner, _damage, _speed, _trackingPower, target);

        // 一定時間後弾丸削除
        Destroy(bullet, _destroySec);

        // SE再生
        _audioSource.volume = SoundManager.MasterSEVolume;
        _audioSource.Play();

        // 前回発射時間初期化
        _shotTimer = 0;
    }

    private void Awake()
    {
        // 発射間隔計算
        _shotIntervalSec = 1.0f / _shotPerSecond;
        _shotTimer = _shotIntervalSec;

        // コンポーネント取得
        _audioSource = GetComponent<AudioSource>();
        _audioSource.clip = SoundManager.GetAudioClip(SoundManager.SE.Gatling);
    }

    private void Update()
    {
        if (_shotTimer < _shotIntervalSec)
        {
            _shotTimer += Time.deltaTime;
        }
    }
}