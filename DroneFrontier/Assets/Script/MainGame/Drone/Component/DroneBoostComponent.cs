using Offline;
using System.Drawing.Text;
using UnityEngine;
using UnityEngine.UI;

public class DroneBoostComponent : MonoBehaviour, IDroneComponent
{
    /// <summary>
    /// �u�[�X�g�̉����x
    /// </summary>
    public float BoostAccele
    {
        get { return _boostAccele; }
        set { _boostAccele = value; }
    }

    /// <summary>
    /// �ő�u�[�X�g�\����
    /// </summary>
    public float MaxBoostTime
    {
        get { return _maxBoostTime; }
        set 
        {
            _maxBoostTime = value;
            _useGaugePerSec = 1 / _maxBoostTime;
        }
    }

    /// <summary>
    /// �u�[�X�g�̍ő僊�L���X�g����
    /// </summary>
    public float MaxBoostRecastTime
    {
        get { return _maxBoostRecastTime; }
        set 
        {
            _maxBoostRecastTime = value;
            _addGaugePerSec = 1 / _maxBoostRecastTime;
        }
    }

    /// <summary>
    /// �Q�[�WUI���\���ɂ��邩
    /// </summary>
    public bool HideGaugeUI
    {
        get { return _hideGaugeUI; }
        set
        {
            if (_boostGaugeUI != null)
            {
                _boostGaugeUI.enabled = !value;
            }
            _hideGaugeUI = value;
        }
    }
    private bool _hideGaugeUI = false;

    /// <summary>
    /// �u�[�X�g�\�ȍŒ�Q�[�W��
    /// </summary>
    private const float BOOSTABLE_MIN_GAUGE = 0.2f;

    /// <summary>
    /// �u�[�X�g�Q�[�WUI
    /// </summary>
    [SerializeField, Tooltip("�u�[�X�g�Q�[�WUI")]
    private Image _boostGaugeUI = null;

    [SerializeField, Tooltip("�u�[�X�g�̉����x")]
    private float _boostAccele = 2.1f;

    [SerializeField, Tooltip("�ő�u�[�X�g�\����")]
    private float _maxBoostTime = 6.0f;

    [SerializeField, Tooltip("�u�[�X�g�̍ő僊�L���X�g����")]
    private float _maxBoostRecastTime = 8.0f;

    /// <summary>
    /// ���݂̃u�[�X�g�Q�[�W��
    /// </summary>
    private float _gaugeValue = 1f;

    /// <summary>
    /// 1�b���Ƃɏ����Q�[�W��
    /// </summary>
    private float _useGaugePerSec = 0;

    /// <summary>
    /// 1�b���Ƃɉ񕜂���Q�[�W��
    /// </summary>
    private float _addGaugePerSec = 0;

    /// <summary>
    /// �u�[�X�gSE�Đ����ɔ��s���ꂽSE�ԍ�
    /// </summary>
    private int _boostSEId = -1;

    /// <summary>
    /// �u�[�X�g���ł��邩
    /// </summary>
    private bool _isBoost = false;

    // �R���|�[�l���g�L���b�V��
    private DroneMoveComponent _moveComponent = null;
    private DroneSoundComponent _soundComponent;

    public void Initialize() { }

    /// <summary>
    /// �u�[�X�g�J�n
    /// </summary>
    public void StartBoost()
    {
        // ���Ƀu�[�X�g���̏ꍇ�͉������Ȃ�
        if (_isBoost) return;

        // �u�[�X�g�ɕK�v�ȍŒ���̃Q�[�W���Ȃ��ƃu�[�X�g�J�n�ł��Ȃ�
        if (_gaugeValue < BOOSTABLE_MIN_GAUGE) return;

        // �ړ����x�㏸
        _moveComponent.MoveSpeed *= _boostAccele;
        
        // �u�[�X�gSE�Đ�
        _boostSEId = _soundComponent.PlayLoopSE(SoundManager.SE.BOOST, SoundManager.SEVolume * 0.15f);

        // �u�[�X�g�t���OON
        _isBoost = true;
    }

    /// <summary>
    /// �u�[�X�g��~
    /// </summary>
    public void StopBoost()
    {
        // �u�[�X�g���łȂ��ꍇ�͉������Ȃ�
        if (!_isBoost) return;

        // �ړ����x��߂�
        _moveComponent.MoveSpeed *= 1 / _boostAccele;

        // �u�[�X�gSE��~
        _soundComponent.StopLoopSE(_boostSEId);

        // �u�[�X�g�t���OOFF
        _isBoost = false;
    }

    private void Awake()
    {
        // �R���|�[�l���g�L���b�V��
        _moveComponent = GetComponent<DroneMoveComponent>();
        _soundComponent = GetComponent<DroneSoundComponent>();

        // �v���p�e�B������
        MaxBoostTime = _maxBoostTime;
        MaxBoostRecastTime = _maxBoostRecastTime;
    }

    private void LateUpdate()
    {
        // �u�[�X�g���̓Q�[�W�����炷
        if (_isBoost)
        {
            _gaugeValue -= _useGaugePerSec * Time.deltaTime;

            // �Q�[�W�������Ȃ����ꍇ�̓u�[�X�g��~
            if (_gaugeValue <= 0)
            {
                _gaugeValue = 0;
                StopBoost();
            }
        }
        else
        {
            // �u�[�X�g���łȂ��ꍇ�̓Q�[�W��
            if (_gaugeValue < 1.0f)
            {
                // �Q�[�W����
                _gaugeValue += _addGaugePerSec * Time.deltaTime;
                if (_gaugeValue > 1f)
                {
                    _gaugeValue = 1f;
                }
            }
        }

        // UI�ɔ��f
        if (!_hideGaugeUI && _boostGaugeUI != null)
        {
            _boostGaugeUI.fillAmount = _gaugeValue;
        }
    }
}
