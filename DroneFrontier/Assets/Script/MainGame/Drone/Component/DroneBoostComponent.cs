using Offline;
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
    /// Boost���\�b�h�Ăяo������<br/>
    /// [0]:���݂̃t���[��<br/>
    /// [1]:1�t���[���O
    /// </summary>
    private bool[] _isBoosted = new bool[2];

    // �R���|�[�l���g�L���b�V��
    private DroneMoveComponent _moveComponent = null;
    private DroneSoundComponent _soundComponent;

    public void Initialize() { }

    public void Boost()
    {
        // �u�[�X�g�ɕK�v�ȍŒ���̃Q�[�W���Ȃ��ƃu�[�X�g�J�n�ł��Ȃ�
        if (!_isBoosted[1])
        {
            if (_gaugeValue < BOOSTABLE_MIN_GAUGE)
            {
                return;
            }
        }

        // �u�[�X�g�K�p
        if (!_isBoosted[1])
        {
            _moveComponent.MoveSpeed *= _boostAccele;
            _boostSEId = _soundComponent.PlayLoopSE(SoundManager.SE.BOOST, SoundManager.SEVolume * 0.15f);
        }
        _isBoosted[0] = true;

        // �u�[�X�g���̓Q�[�W�����炷
        _gaugeValue -= _useGaugePerSec * Time.deltaTime;

        // �Q�[�W�������Ȃ����ꍇ�̓��[�U�[��~
        if (_gaugeValue <= 0)
        {
            _gaugeValue = 0;
            _isBoosted[0] = false;
        }

        // UI�ɔ��f
        if (!_hideGaugeUI && _boostGaugeUI != null)
        {
            _boostGaugeUI.fillAmount = _gaugeValue;
        }
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
        // �u�[�X�g����߂��ꍇ�͑��x��߂�
        if (!_isBoosted[0] && _isBoosted[1])
        {
            _moveComponent.MoveSpeed *= 1 / _boostAccele;
            _soundComponent.StopLoopSE(_boostSEId);
        }

        // �u�[�X�g���łȂ��ꍇ�̓Q�[�W��
        if (!_isBoosted[0])
        {
            if (_gaugeValue < 1.0f)
            {
                // �Q�[�W����
                _gaugeValue += _addGaugePerSec * Time.deltaTime;
                if (_gaugeValue > 1f)
                {
                    _gaugeValue = 1f;
                }

                // UI�ɔ��f
                if (!_hideGaugeUI && _boostGaugeUI != null)
                {
                    _boostGaugeUI.fillAmount = _gaugeValue;
                }
            }
        }

        // Boost���\�b�h�Ăяo�������X�V
        _isBoosted[1] = _isBoosted[0];
        _isBoosted[0] = false;
    }
}
