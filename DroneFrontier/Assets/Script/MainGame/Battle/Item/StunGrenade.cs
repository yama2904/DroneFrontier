using Battle.Status;
using Common;
using Cysharp.Threading.Tasks;
using Drone.Battle;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Battle.Item
{
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
        /// �q�b�g�ς݃I�u�W�F�N�g���X�g
        /// </summary>
        private List<GameObject> _hitteds = new List<GameObject>();

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
                lock (_hitteds)
                {
                    if (_hitteds.Contains(other.gameObject)) return;
                    _hitteds.Add(other.gameObject);
                }
                other.GetComponent<DroneStatusComponent>().AddStatus(new StunStatus(), StunSec);
            }

            if (other.CompareTag(TagNameConst.CPU))
            {
                lock (_hitteds)
                {
                    if (_hitteds.Contains(other.gameObject)) return;
                    _hitteds.Add(other.gameObject);
                }
                other.GetComponent<DroneStatusComponent>().AddStatus(new StunStatus(), StunSec * 0.5f);
            }
        }

        /// <summary>
        /// ���e������
        /// </summary>
        private async UniTask DoImpact()
        {
            _isThrowing = false;

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
}