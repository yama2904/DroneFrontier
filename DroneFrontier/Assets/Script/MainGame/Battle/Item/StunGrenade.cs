using Common;
using Cysharp.Threading.Tasks;
using Drone.Battle;
using Drone.Battle.Network;
using Network;
using System;
using System.Threading;
using UnityEngine;

public class StunGrenade : MonoBehaviour
{
    /// <summary>
    /// ������
    /// </summary>
    public GameObject Thrower { get; set; }

    /// <summary>
    /// �����p�x
    /// </summary>
    public Transform ThrowRotate
    {
        get => _throwRotate;
        set => _throwRotate = value;
    }

    /// <summary>
    /// ���e���ԁi�b�j
    /// </summary>
    public float ImpactSec { get; set; }

    /// <summary>
    /// �������̏d��
    /// </summary>
    public float Weight { get; set; }

    /// <summary>
    /// �X�^����Ԃ̎��ԁi�b�j
    /// </summary>
    public float StunSec { get; set; }

    [SerializeField, Tooltip("�O���l�[�h�I�u�W�F�N�g")]
    private GameObject _grenadeObject = null;

    [SerializeField, Tooltip("�����p�x")]
    private Transform _throwRotate = null;

    [SerializeField, Tooltip("���e�p�I�u�W�F�N�g")]
    private GameObject _impactObject = null;

    /// <summary>
    /// �������ł��邩
    /// </summary>
    private bool _isThrowing = false;

    /// <summary>
    /// �L�����Z���g�[�N�����s�N���X
    /// </summary>
    private CancellationTokenSource _cancel = new CancellationTokenSource();

    private Rigidbody _rigidbody = null;

    /// <summary>
    /// �X�^���O���l�[�h�𓊂���
    /// </summary>
    /// <param name="thrower">������</param>
    /// <param name="speed">�������x</param>
    /// <param name="impactSec">���e���ԁi�b�j</param>
    /// <param name="weight">�������̏d��</param>
    /// <param name="stunSec">�X�^����Ԃ̎��ԁi�b�j</param>
    public void ThrowGrenade(GameObject thrower, float speed, float impactSec, float weight, float stunSec)
    {
        // �p�����[�^�󂯎��
        Thrower = thrower;
        ImpactSec = impactSec;
        Weight = weight;
        StunSec = stunSec;

        // �����J�n
        _isThrowing = true;
        transform.rotation = thrower.transform.rotation * _throwRotate.localRotation;
        _rigidbody.AddForce(transform.forward * speed, ForceMode.Impulse);

        // ���Ԍo�߂Œ��e
        UniTask.Void(async () =>
        {
            await UniTask.Delay(TimeSpan.FromSeconds(ImpactSec), cancellationToken: _cancel.Token, ignoreTimeScale: true);
            DoImpact().Forget();
        });

        // �����҂Ƃ͓����蔻����s��Ȃ�
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
    /// �X�^���O���l�[�h�̓����蔻��
    /// </summary>
    /// <param name="collision"></param>
    private void OnCollisionEnter(Collision collision)
    {
        if (!_isThrowing) return;

        _cancel.Cancel();   // �O���l�[�h���������Ē��e����ꍇ�͎��Ԍo�߂ɂ�钅�e���~
        DoImpact().Forget();
        _isThrowing = false;
    }

    /// <summary>
    /// ���e��̔��������蔻��
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
        // �����҂̏ꍇ�͏������Ȃ�
        if (other.gameObject == Thrower) return;

        if (other.CompareTag(TagNameConst.PLAYER))
        {
            if (other.TryGetComponent<BattleDrone>(out var component))
            {
                other.GetComponent<DroneStatusComponent>().AddStatus(new StunStatus(), StunSec, true);
            }
            else
            {
                if (other.GetComponent<NetworkBattleDrone>().IsControl)
                {
                    other.GetComponent<DroneStatusComponent>().AddStatus(new StunStatus(), StunSec, true);
                }
            }
        }

        if (other.CompareTag(TagNameConst.CPU))
        {
            other.GetComponent<DroneStatusComponent>().AddStatus(new StunStatus(), StunSec * 0.5f, false);
        }
    }

    /// <summary>
    /// ���e������
    /// </summary>
    private async UniTask DoImpact()
    {
        // �O���l�[�h��\��
        _grenadeObject.SetActive(false);

        // ���e�I�u�W�F�N�g�\��
        _impactObject.SetActive(true);

        // �ړ���~������
        _rigidbody.velocity = Vector3.zero;

        // ���e����ɃI�u�W�F�N�g�j��
        await UniTask.Delay(100);
        Destroy(gameObject);
    }
}
