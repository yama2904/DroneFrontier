using Battle.Status;
using Common;
using Cysharp.Threading.Tasks;
using Drone.Battle;
using System;
using System.Threading;
using UnityEngine;

namespace Battle.Gimmick
{
    public class BarrierWeakLaser : MonoBehaviour
    {
        /// <summary>
        /// ���[�U�[����������x�ɒZ�����锭���Ԋu�i�b�j
        /// </summary>
        private const float INTERVAL_SHORT_SEC = 3;

        /// <summary>
        /// ���[�U�[�̍ő唼�a
        /// </summary>
        private const float MAX_LASER_WIDTH = 100f;

        #region �v���p�e�B

        /// <summary>
        /// �o���A��̉����ԁi�b�j
        /// </summary>
        public float WeakTime
        {
            get => _weakTime;
            set => _weakTime = value;
        }

        /// <summary>
        /// ���[�U�[�˒�
        /// </summary>
        public float LazerRange
        {
            get => _lazerRange;
            set => _lazerRange = value;
        }

        /// <summary>
        /// ���[�U�[�̓����蔻��̔��a
        /// </summary>
        public float LazerRadius
        {
            get => _lazerRadius;
            set => _lazerRadius = value;
        }

        /// <summary>
        /// ���[�U�[�̃����_�������Ԋu�̍ŏ��l�i�b�j
        /// </summary>
        public float MinInterval
        {
            get => _minInterval;
            set => _minInterval = value;
        }

        /// <summary>
        /// ���[�U�[�̃����_�������Ԋu�̍ő�l�i�b�j
        /// </summary>
        public float MaxInterval
        {
            get => _maxInterval;
            set => _maxInterval = value;
        }

        /// <summary>
        /// ���[�U�[�̔������ԁi�b�j
        /// </summary>
        public float LaserTime
        {
            get => _laserTime;
            set => _laserTime = value;
        }

        /// <summary>
        /// ���[�U�[��Y�������_����]���x(/s)�̍ŏ��l
        /// </summary>
        public float MinRotateSpeed
        {
            get => _minRotateSpeed;
            set => _minRotateSpeed = value;
        }

        /// <summary>
        /// ���[�U�[��Y�������_����]���x(/s)�̍ő�l
        /// </summary>
        public float MaxRotateSpeed
        {
            get => _maxRotateSpeed;
            set => _maxRotateSpeed = value;
        }

        /// <summary>
        /// ���[�U�[��Y�������_����]���x(/s)�̌��ݒl
        /// </summary>
        public float CurrentRotateSpeed { get; private set; } = 0;

        /// <summary>
        /// ���[�U�[��������X�������_���p�x�̍ŏ��l
        /// </summary>
        public float MinAngle
        {
            get => _minAngle;
            set => _minAngle = value;
        }

        /// <summary>
        /// ���[�U�[��������X�������_���p�x�̍ő�l
        /// </summary>
        public float MaxAngle
        {
            get => _maxAngle;
            set => _maxAngle = value;
        }

        #endregion

        /// <summary>
        /// ���[�U�[�����C�x���g
        /// </summary>
        public event EventHandler OnSpawn;

        /// <summary>
        /// ���[�U�[���ŃC�x���g
        /// </summary>
        public event EventHandler OnDespawn;

        [SerializeField, Tooltip("�o���A��̉����ԁi�b�j")]
        private float _weakTime = 15f;

        [SerializeField, Tooltip("���[�U�[�˒�")]
        private float _lazerRange = 3000f;

        [SerializeField, Tooltip("���[�U�[�̓����蔻��̔��a")]
        private float _lazerRadius = 10f;

        [SerializeField, Tooltip("���[�U�[�̃����_�������Ԋu�̍ŏ��l�i�b�j")]
        private float _minInterval = 30f;

        [SerializeField, Tooltip("���[�U�[�̃����_�������Ԋu�̍ő�l�i�b�j")]
        private float _maxInterval = 60f;

        [SerializeField, Tooltip("���[�U�[�̔������ԁi�b�j")]
        private float _laserTime = 20f;

        [SerializeField, Tooltip("���[�U�[��Y�������_����]���x(/s)�̍ŏ��l")]
        private float _minRotateSpeed = 70f;

        [SerializeField, Tooltip("���[�U�[��Y�������_����]���x(/s)�̍ő�l")]
        private float _maxRotateSpeed = 120f;

        [SerializeField, Tooltip("���[�U�[��������X�������_���p�x�̍ŏ��l")]
        private float _minAngle = 20f;

        [SerializeField, Tooltip("���[�U�[��������X�������_���p�x�̍ő�l")]
        private float _maxAngle = 50f;

        /// <summary>
        /// ���݂̃��[�U�[���a
        /// </summary>
        private float _lazerWidth = 0;

        private CancellationTokenSource _cancel = new CancellationTokenSource();

        private bool _enabledLaser = false;

        // �R���|�[�l���g�L���b�V��
        private Transform _transform = null;
        private LineRenderer _renderer = null;

        private void Start()
        {
            // �R���|�[�l���g�L���b�V��
            _transform = transform;
            _renderer = GetComponent<LineRenderer>();

            // ���[�U�[�̎n�_������
            _transform.position = _renderer.GetPosition(0);

            // ���[�U�[��\��
            _renderer.startWidth = 0;
            _renderer.endWidth = 0;

            // ���[�U�[����/��~�^�C�}�[
            UniTask.Void(async () =>
            {
                while (true)
                {
                    // ���[�U�[�����^�C�}�[
                    TimeSpan interval = TimeSpan.FromSeconds(UnityEngine.Random.Range(_minInterval, _maxInterval));
                    await UniTask.Delay(interval, cancellationToken: _cancel.Token);

                    // ���Ƀ��[�U�[�������̏ꍇ�̓X�L�b�v
                    if (_enabledLaser) continue;
                    Debug.Log("�o���A��̉����[�U�[����");

                    // �������邽�тɊԊu��Z������
                    _minInterval -= INTERVAL_SHORT_SEC;
                    _minInterval = _minInterval < 0 ? 0 : _minInterval;
                    _maxInterval -= INTERVAL_SHORT_SEC;
                    _maxInterval = _maxInterval < 0 ? 0 : _maxInterval;

                    // ���[�U�[�̊p�x�������_���ɐݒ�
                    Vector3 angle = _transform.localEulerAngles;
                    angle.x = UnityEngine.Random.Range(_minAngle, _maxAngle);
                    _transform.localEulerAngles = angle;

                    // ��]���x�������_���ɐݒ�
                    CurrentRotateSpeed = UnityEngine.Random.Range(_minRotateSpeed, _maxRotateSpeed);

                    // ���[�U�[����
                    _enabledLaser = true;

                    // �����C�x���g����
                    OnSpawn?.Invoke(this, EventArgs.Empty);

                    // ���[�U�[��~�^�C�}�[�i���[�U�[��~���̏��X�ɍׂ����鎞�Ԃ��l���j
                    await UniTask.Delay(TimeSpan.FromSeconds(_laserTime - 1), cancellationToken: _cancel.Token);
                    _enabledLaser = false;
                    await UniTask.Delay(1000, cancellationToken: _cancel.Token);
                    Debug.Log("�o���A��̉����[�U�[��~");

                    // ���ŃC�x���g����
                    OnDespawn?.Invoke(this, EventArgs.Empty);
                }
            });
        }

        private void Update()
        {
            // ���[�U�[�T�C�Y�X�V
            UpdateLazerWidth(_enabledLaser);
        }

        private void FixedUpdate()
        {
            if (!_enabledLaser) return;

            // ���[�U�[�̔������͏�ɉ�]
            Vector3 angle = _transform.localEulerAngles;
            angle.y += CurrentRotateSpeed * Time.deltaTime;
            _transform.localEulerAngles = angle;

            // ���[�U�[�̃q�b�g�I�u�W�F�N�g�����ׂĎ擾
            RaycastHit[] hits = Physics.SphereCastAll(
                                            _transform.position,
                                            _lazerRadius,
                                            _transform.forward,
                                            _lazerRange);
            // �q�b�g�I�u�W�F�N�g���i�荞��
            bool exists = FilterTarget(hits, out RaycastHit target);

            // �q�b�g�����̏ꍇ�̓I�u�W�F�N�g�̕\�����ő�˒��ɂ��ďI��
            if (!exists)
            {
                ApplyLaserLength(_lazerRange);
                return;
            }

            // �h���[���Ƀq�b�g�����ꍇ�̓o���A��̉��t�^
            if (target.transform.CompareTag(TagNameConst.PLAYER))
            {
                // �o���A��̉�
                target.transform.GetComponent<DroneStatusComponent>().AddStatus(new BarrierWeakStatus(), _weakTime);
            }

            // �q�b�g�����I�u�W�F�N�g�Ń��[�U�[���~�߂�
            ApplyLaserLength(target.distance);
        }

        private void OnDestroy()
        {
            _cancel.Cancel();
        }

        /// <summary>
        /// �w�肳�ꂽ�I�u�W�F�N�g�̂����ł��������߂��q�b�g�\�I�u�W�F�N�g��Ԃ�
        /// </summary>
        /// <param name="hits"></param>
        /// <param name="target"></param>
        /// <returns>�q�b�g�\�I�u�W�F�N�g�����݂��Ȃ��ꍇ��false</returns>
        private bool FilterTarget(RaycastHit[] hits, out RaycastHit target)
        {
            // out�p�����[�^������
            target = new RaycastHit();

            // �I�u�W�F�N�g�Ƃ̍ŏ�����
            float minDistance = float.MaxValue;

            bool exists = false;
            foreach (RaycastHit hit in hits)
            {
                // �����蔻����s��Ȃ��I�u�W�F�N�g�̓X�L�b�v
                Transform t = hit.transform;
                if (t.CompareTag(TagNameConst.ITEM)) continue;
                if (t.CompareTag(TagNameConst.BULLET)) continue;
                if (t.CompareTag(TagNameConst.GIMMICK)) continue;
                if (t.CompareTag(TagNameConst.JAMMING_AREA)) continue;
                if (t.CompareTag(TagNameConst.TOWER)) continue;
                if (t.CompareTag(TagNameConst.NOT_COLLISION)) continue;

                // �������ŏ���������X�V
                if (hit.distance < minDistance)
                {
                    minDistance = hit.distance;
                    target = hit;
                }
                exists = true;
            }

            return exists;
        }

        /// <summary>
        /// ���[�U�[�I�u�W�F�N�g�̒����𔽉f������
        /// </summary>
        /// <param name="length">���[�U�[�̒���</param>
        private void ApplyLaserLength(float length)
        {
            // transform�ɓK�p
            Vector3 scale = _transform.localScale;
            _transform.localScale = new Vector3(scale.x, scale.y, length);

            // ���[�U�[�̌����ڂɓK�p
            _renderer.SetPosition(1, _transform.position + (_transform.forward * length));
        }

        /// <summary>
        /// ���[�U�[�̔��a���X�V
        /// </summary>
        /// <param name="enableLazer">���[�U�[�������ł��邩</param>
        private void UpdateLazerWidth(bool enableLazer)
        {
            // ���[�U�[���������̏ꍇ�͏��X�ɑ�������
            if (enableLazer)
            {
                if (_lazerWidth >= MAX_LASER_WIDTH) return;

                _lazerWidth += MAX_LASER_WIDTH * Time.deltaTime;
                if (_lazerWidth > MAX_LASER_WIDTH)
                {
                    _lazerWidth = MAX_LASER_WIDTH;
                }
                _renderer.startWidth = _lazerWidth;
                _renderer.endWidth = _lazerWidth;
            }
            else
            {
                // ���[�U�[���������ȊO�̏ꍇ�͏��X�ɍׂ�����

                if (_lazerWidth <= 0) return;

                _lazerWidth -= MAX_LASER_WIDTH * Time.deltaTime;
                if (_lazerWidth <= 0)
                {
                    _lazerWidth = 0;
                }
                _renderer.startWidth = _lazerWidth;
                _renderer.endWidth = _lazerWidth;
            }
        }
    }
}