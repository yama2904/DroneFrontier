using Offline;
using UnityEngine;

public class DroneWeaponComponent : MonoBehaviour, IDroneComponent
{
    public enum Weapon
    {
        /// <summary>
        /// ���C������
        /// </summary>
        MAIN,

        /// <summary>
        /// �T�u����
        /// </summary>
        SUB,

        NONE
    }

    /// <summary>
    /// ���C������
    /// </summary>
    public IWeapon MainWeapon { get; private set; } = null;

    /// <summary>
    /// �T�u����
    /// </summary>
    public IWeapon SubWeapon { get; private set; } = null;

    /// <summary>
    /// ���C������U�����̃X�s�[�h�ቺ��
    /// </summary>
    public float MainSpeedDownPer { get; set; } = 0;

    /// <summary>
    /// �T�u����U�����̃X�s�[�h�ቺ��
    /// </summary>
    public float SubSpeedDownPer { get; set; } = 0;

    /// <summary>
    /// ���C������U�����ł��邩
    /// </summary>
    public bool ShootingMainWeapon => _mainShotHistory.CurrentValue || _mainShotHistory.PreviousValue;

    /// <summary>
    /// �T�u����U�����ł��邩
    /// </summary>
    public bool ShootingSubWeapon => _subShotHistory.CurrentValue || _subShotHistory.PreviousValue;

    /// <summary>
    /// �e�ۃC�x���g�n���h���[
    /// </summary>
    /// <param name="component">DroneWeaponComponent</param>
    /// <param name="type">�C�x���g���΂�������̎��</param>
    /// <param name="weapon">�C�x���g���΂�������</param>
    public delegate void BulletEventHandler(DroneWeaponComponent component, Weapon type, IWeapon weapon);

    /// <summary>
    /// �S�e��[�C�x���g
    /// </summary>
    public event BulletEventHandler OnBulletFull;

    /// <summary>
    /// �c�e�����C�x���g
    /// </summary>
    public event BulletEventHandler OnBulletEmpty;

    /// <summary>
    /// �U�����̃X�s�[�h�ቺ��
    /// </summary>
    private const float SPEED_DOWN_PER = 0.5f;

    /// <summary>
    /// ���[�U�[�U�����̃X�s�[�h�ቺ��
    /// </summary>
    private const float LASER_SPEED_DOWN_PER = 0.25f;

    [SerializeField, Tooltip("���C�����푕���ʒu")]
    private Transform _mainWeaponPos = null;

    [SerializeField, Tooltip("�T�u���푕���ʒu")]
    private Transform _subWeaponPos = null;

    [SerializeField, Tooltip("���C�����픭�ˈʒu")]
    private Transform _mainShotPos = null;

    [SerializeField, Tooltip("�T�u���픭�ˈʒu")]
    private Transform _subShotPos = null;

    [SerializeField, Tooltip("�e��UI�\��Canvas")]
    private Canvas _bulletUICanvs = null;

    /// <summary>
    /// �T�u����̎��
    /// </summary>
    private WeaponType _subWeaponType = WeaponType.NONE;

    /// <summary>
    /// ���C������g�p����
    /// </summary>
    private ValueHistory<bool> _mainShotHistory = new ValueHistory<bool>();

    /// <summary>
    /// �T�u����g�p����
    /// </summary>
    private ValueHistory<bool> _subShotHistory = new ValueHistory<bool>();

    // �R���|�[�l���g�L���b�V��
    DroneMoveComponent _moveComponent = null;

    public void Initialize() 
    {
        // ���C������ǂݍ���
        GameObject mainWeapon = WeaponCreater.CreateWeapon(WeaponType.GATLING);
        mainWeapon.transform.SetParent(_mainWeaponPos, false);
        MainWeapon = mainWeapon.GetComponent<IWeapon>();
        MainWeapon.Owner = gameObject;
        MainWeapon.ShotPosition = _mainShotPos;
        MainWeapon.OnBulletFull += (o, e) =>
        {
            OnBulletFull?.Invoke(this, Weapon.MAIN, MainWeapon);
        };
        MainWeapon.OnBulletEmpty += (o, e) =>
        {
            OnBulletEmpty?.Invoke(this, Weapon.MAIN, MainWeapon);
        };

        // �T�u����ǂݍ���
        _subWeaponType = GetComponent<IBattleDrone>().SubWeapon;
        GameObject subWeapon = WeaponCreater.CreateWeapon(_subWeaponType);
        subWeapon.transform.SetParent(_subWeaponPos, false);
        SubWeapon = subWeapon.GetComponent<IWeapon>();
        SubWeapon.Owner = gameObject;
        SubWeapon.ShotPosition = _subShotPos;
        SubWeapon.BulletUICanvas = _bulletUICanvs;
        SubWeapon.OnBulletFull += (o, e) =>
        {
            OnBulletFull?.Invoke(this, Weapon.SUB, SubWeapon);
        };
        SubWeapon.OnBulletEmpty += (o, e) =>
        {
            OnBulletEmpty?.Invoke(this, Weapon.SUB, SubWeapon);
        };

        // �U�����̃X�s�[�h�ቺ���ݒ�
        MainSpeedDownPer = SPEED_DOWN_PER;
        SubSpeedDownPer = _subWeaponType == WeaponType.LASER ? LASER_SPEED_DOWN_PER : SPEED_DOWN_PER;
    }

    /// <summary>
    /// ������g�p���Ēe�۔���
    /// </summary>
    /// <param name="weapon">�g�p���镐��</param>
    /// <param name="target">�Ǐ]�Ώ�</param>
    public void Shot(Weapon weapon, GameObject target = null)
    {
        // ���C������U��
        if (weapon == Weapon.MAIN)
        {
            MainWeapon.Shot(target);

            // �U�����͑��x�ቺ
            if (!_mainShotHistory.PreviousValue)
            {
                _moveComponent.MoveSpeed *= MainSpeedDownPer;
            }

            // ���C���U���t���O�𗧂Ă�
            _mainShotHistory.CurrentValue = true;
        }

        // �T�u����U��
        if (weapon == Weapon.SUB)
        {
            SubWeapon.Shot(target);

            // �U�����͑��x�ቺ
            if (!_subShotHistory.PreviousValue)
            {
                _moveComponent.MoveSpeed *= SubSpeedDownPer;
            }

            // �T�u�U���t���O�𗧂Ă�
            _subShotHistory.CurrentValue = true;
        }
    }

    private void Awake()
    {
        _moveComponent = GetComponent<DroneMoveComponent>();
    }

    private void LateUpdate()
    {
        // ���C������̍U�����~�����ꍇ�͑��x��߂�
        if (!_mainShotHistory.CurrentValue && _mainShotHistory.PreviousValue)
        {
            _moveComponent.MoveSpeed *= 1 / MainSpeedDownPer;
        }

        // �T�u����̍U�����~�����ꍇ�͑��x��߂�
        if (!_subShotHistory.CurrentValue && _subShotHistory.PreviousValue)
        {
            _moveComponent.MoveSpeed *= 1 / SubSpeedDownPer;
        }

        // ����g�p�����X�V
        _mainShotHistory.UpdateCurrentValue(false);
        _subShotHistory.UpdateCurrentValue(false);
    }
}
