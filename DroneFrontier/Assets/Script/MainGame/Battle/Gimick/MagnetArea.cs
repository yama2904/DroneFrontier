using Common;
using Cysharp.Threading.Tasks;
using Drone.Battle;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class MagnetArea : MonoBehaviour
{
    /// <summary>
    /// �A�N�e�B�u�\�ȍő�G���A��
    /// </summary>
    private const int MAX_MAGNET_AREA_NUM = 3;

    /// <summary>
    /// ���݂̃A�N�e�B�u�G���A��
    /// </summary>
    private static int _currentAreaNum = 0;

    #region �v���p�e�B

    /// <summary>
    /// ���x�ቺ��
    /// </summary>
    public float DownPercent
    {
        get => _downPercent;
        set => _downPercent = value;
    }

    /// <summary>
    /// �X�|�[���m��
    /// </summary>
    public int SpawnPercent
    {
        get => _spawnPercent;
        set => _spawnPercent = value;
    }

    /// <summary>
    /// �����Ԋu�i�b�j
    /// </summary>
    public float SpawnInterval
    {
        get => _spawnInterval;
        set => _spawnInterval = value;
    }

    /// <summary>
    /// �������ԁi�b�j
    /// </summary>
    public float ActiveTime
    {
        get => _activeTime;
        set => _activeTime = value;
    }

    /// <summary>
    /// �ŏ��G���A�T�C�Y
    /// </summary>
    public float MinAreaSize
    {
        get => _minAreaSize;
        set => _minAreaSize = value;
    }

    /// <summary>
    /// �ő�G���A�T�C�Y
    /// </summary>
    public float MaxAreaSize
    {
        get => _maxAreaSize;
        set => _maxAreaSize = value;
    }

    /// <summary>
    /// ���݃G���A�T�C�Y
    /// </summary>
    public float CurrentAreaSize { get; private set; } = 0;

    #endregion

    /// <summary>
    /// ���C�G���A�����C�x���g
    /// </summary>
    public event EventHandler OnSpawn;

    /// <summary>
    /// ���C�G���A���ŃC�x���g
    /// </summary>
    public event EventHandler OnDespawn;

    [SerializeField, Range(0, 1f), Tooltip("���x�ቺ��")]
    private float _downPercent = 0.7f;

    [SerializeField, Range(0, 100), Tooltip("�X�|�[���m��")]
    private int _spawnPercent = 50;

    [SerializeField, Tooltip("�����Ԋu�i�b�j")]
    private float _spawnInterval = 30f;

    [SerializeField, Tooltip("�������ԁi�b�j")]
    private float _activeTime = 20f;

    [SerializeField, Tooltip("�ŏ��G���A�T�C�Y")]
    private float _minAreaSize = 1f;

    [SerializeField, Tooltip("�ő�G���A�T�C�Y")]
    private float _maxAreaSize = 3f;

    [SerializeField]
    private ParticleSystem _particle1 = null;

    [SerializeField]
    private ParticleSystem _particle2 = null;

    /// <summary>
    /// �e�I�u�W�F�N�g�ɕt�^�����X�s�[�h�_�E���X�e�[�^�X
    /// </summary>
    private Dictionary<GameObject, SpeedDownStatus> _speedDowns = new Dictionary<GameObject, SpeedDownStatus>();

    private CancellationTokenSource _cancel = new CancellationTokenSource();

    // �R���|�[�l���g�L���b�V��
    private Transform _transform = null;

    private void Start()
    {
        _transform = transform;
        SetEnabledArea(false);

        UniTask.Void(async () =>
        {
            while (true)
            {
                // �����^�C�}�[
                await UniTask.Delay(TimeSpan.FromSeconds(_spawnInterval), cancellationToken: _cancel.Token);

                // ���������邩�����_���Ɍ���
                if (UnityEngine.Random.Range(0, 100) >= _spawnPercent) continue;

                // ���ɍő吔�������Ă�����X�L�b�v
                if (_currentAreaNum >= MAX_MAGNET_AREA_NUM) continue;

                // �����J�n
                _currentAreaNum++;
                CurrentAreaSize = UnityEngine.Random.Range(_minAreaSize, _maxAreaSize);
                ChangeAreaSize(CurrentAreaSize);
                SetEnabledArea(true);

                // �����C�x���g����
                OnSpawn?.Invoke(this, EventArgs.Empty);

                // ��~�^�C�}�[
                await UniTask.Delay(TimeSpan.FromSeconds(_activeTime), cancellationToken: _cancel.Token);

                // ��~
                SetEnabledArea(false);
                ClearStatus();
                _currentAreaNum--;

                // ���ŃC�x���g����
                OnDespawn?.Invoke(this, EventArgs.Empty);
            }
        });
    }

    private void OnDestroy()
    {
        ClearStatus();
        _cancel.Cancel();
    }

    private void OnTriggerEnter(Collider other)
    {
        // ���ɃX�s�[�h�_�E���t�^�ς݂̏ꍇ�͏������Ȃ�
        if (_speedDowns.ContainsKey(other.gameObject)) return;

        // �v���C���[��CPU�̂ݏ���
        if (!other.CompareTag(TagNameConst.PLAYER)
            && !other.CompareTag(TagNameConst.CPU))
        {
            return;
        }

        // �X�s�[�h�_�E���X�e�[�^�X�t�^
        SpeedDownStatus status = new SpeedDownStatus();
        other.GetComponent<DroneStatusComponent>().AddStatus(status, 9999, _downPercent);
        _speedDowns.Add(other.gameObject, status);

        Debug.Log($"�X�s�[�h�_�E���F{other.gameObject.name}");
    }

    private void OnTriggerExit(Collider other)
    {
        // �W���~���O����
        if (_speedDowns.ContainsKey(other.gameObject))
        {
            _speedDowns[other.gameObject].EndSpeedDown();
            _speedDowns.Remove(other.gameObject);
        }
    }

    /// <summary>
    /// ���C�G���A�̔���/��~��ݒ�
    /// </summary>
    /// <param name="enable">����������ꍇ��true</param>
    private void SetEnabledArea(bool enable)
    {
        if (enable)
        {
            _particle1.Play();
            _particle2.Play();
            gameObject.SetActive(true);
        }
        else
        {
            _particle1.Stop();
            _particle2.Stop();
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// �G���A�T�C�Y�ύX
    /// </summary>
    /// <param name="size"></param>
    private void ChangeAreaSize(float size)
    {
        _transform.localScale = new Vector3(size, size, size);
        _particle1.transform.localScale = new Vector3(size * 5, size * 5, size * 5);
        _particle2.transform.localScale = new Vector3(size * 5, size * 5, size * 5);
    }

    /// <summary>
    /// �S�ẴX�s�[�h�_�E��������
    /// </summary>
    private void ClearStatus()
    {
        foreach (SpeedDownStatus status in _speedDowns.Values)
        {
            status.EndSpeedDown();
        }
        _speedDowns.Clear();
    }
}
