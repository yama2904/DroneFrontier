using Cysharp.Threading.Tasks;
using Offline.Player;
using System;
using System.Threading;
using UnityEngine;

public class StunGrenade : MonoBehaviour
{
    /// <summary>
    /// 投擲者
    /// </summary>
    public GameObject Thrower { get; set; }

    /// <summary>
    /// 着弾時間（秒）
    /// </summary>
    public float ImpactSec { get; set; }

    /// <summary>
    /// 投擲時の重さ
    /// </summary>
    public float Weight { get; set; }

    /// <summary>
    /// スタン状態の時間（秒）
    /// </summary>
    public float StunSec { get; set; }

    [SerializeField, Tooltip("グレネードオブジェクト")]
    private GameObject _grenadeObject = null;

    [SerializeField, Tooltip("着弾用オブジェクト")]
    private GameObject _impactObject = null;

    /// <summary>
    /// 投擲中であるか
    /// </summary>
    private bool _isThrowing = false;

    /// <summary>
    /// キャンセルトークン発行クラス
    /// </summary>
    private CancellationTokenSource _cancel = new CancellationTokenSource();

    private Rigidbody _rigidbody = null;

    /// <summary>
    /// スタングレネードを投げる
    /// </summary>
    /// <param name="thrower">投擲者</param>
    /// <param name="speed">投擲速度</param>
    /// <param name="impactSec">着弾時間（秒）</param>
    /// <param name="weight">投擲時の重さ</param>
    /// <param name="stunSec">スタン状態の時間（秒）</param>
    public void ThrowGrenade(GameObject thrower, float speed, float impactSec, float weight, float stunSec)
    {
        // パラメータ受け取り
        Thrower = thrower;
        ImpactSec = impactSec;
        Weight = weight;
        StunSec = stunSec;

        // 投擲開始
        _isThrowing = true;
        _rigidbody.AddForce(transform.forward * speed, ForceMode.Impulse);

        // 時間経過で着弾
        UniTask.Void(async () =>
        {
            await UniTask.Delay(TimeSpan.FromSeconds(ImpactSec), cancellationToken: _cancel.Token);
            DoImpact().Forget();
        });

        // 投擲者とは当たり判定を行わない
        if (!Useful.IsNullOrDestroyed(thrower) && thrower.TryGetComponent(out Collider collider))
        {
            Physics.IgnoreCollision(collider, _grenadeObject.GetComponent<Collider>());
        }
    }

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (!_isThrowing) return;
        _rigidbody.AddForce(new Vector3(0, Weight * -1, 0), ForceMode.Acceleration);
    }

    /// <summary>
    /// スタングレネードの当たり判定
    /// </summary>
    /// <param name="collision"></param>
    private void OnCollisionEnter(Collision collision)
    {
        if (!_isThrowing) return;

        _cancel.Cancel();   // グレネードが当たって着弾する場合は時間経過による着弾を停止
        DoImpact().Forget();
        _isThrowing = false;
    }

    /// <summary>
    /// 着弾後の爆発当たり判定
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
        //特定のオブジェクトはすり抜け
        //if (ReferenceEquals(collision.gameObject, thrower)) return;
        //if (collision.CompareTag(TagNameConst.ITEM)) return;
        //if (collision.CompareTag(TagNameConst.GIMMICK)) return;
        //if (collision.CompareTag(TagNameConst.JAMMING)) return;
        //if (collision.CompareTag(TagNameConst.BULLET)) return;


        // 投擲者の場合は処理しない
        if (other.gameObject == Thrower) return;

        if (other.CompareTag(TagNameConst.PLAYER))
        {
            other.GetComponent<DroneStatusComponent>().AddStatus(new PlayerStunStatus(), StunSec); 
            return;
        }

        if (other.CompareTag(TagNameConst.CPU))
        {
            other.GetComponent<DroneStatusComponent>().AddStatus(new CpuStunStatus(), StunSec * 0.5f);
            return;
        }
    }

    /// <summary>
    /// 着弾させる
    /// </summary>
    private async UniTask DoImpact()
    {
        // グレネード非表示
        _grenadeObject.SetActive(false);

        // 着弾オブジェクト表示
        _impactObject.SetActive(true);

        // 移動停止させる
        _rigidbody.velocity = Vector3.zero;

        // 着弾直後にオブジェクト破棄
        await UniTask.Delay(100);
        Destroy(gameObject);
    }
}
