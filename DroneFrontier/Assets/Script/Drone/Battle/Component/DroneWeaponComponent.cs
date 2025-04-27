using Common;
using UnityEngine;

namespace Drone.Battle
{
    public class DroneWeaponComponent : MonoBehaviour, IDroneComponent
    {
        /// <summary>
        /// �U�����̃X�s�[�h�ቺ��
        /// </summary>
        private const float SPEED_DOWN_PER = 0.5f;

        public enum Weapon
        {
            /// <summary>
            /// ���C������
            /// </summary>
            Main,

            /// <summary>
            /// �T�u����
            /// </summary>
            Sub,

            None
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
        public float MainSpeedDownPer { get; set; } = SPEED_DOWN_PER;

        /// <summary>
        /// �T�u����U�����̃X�s�[�h�ቺ��
        /// </summary>
        public float SubSpeedDownPer { get; set; } = SPEED_DOWN_PER;

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

        [SerializeField, Tooltip("���C�����푕���ʒu")]
        private Transform _mainWeaponPos = null;

        [SerializeField, Tooltip("�T�u���푕���ʒu")]
        private Transform _subWeaponPos = null;

        /// <summary>
        /// ���C������g�p����
        /// </summary>
        private ValueHistory<bool> _mainShotHistory = new ValueHistory<bool>();

        /// <summary>
        /// �T�u����g�p����
        /// </summary>
        private ValueHistory<bool> _subShotHistory = new ValueHistory<bool>();

        /// <summary>
        /// �U�����ɔ��s���ꂽ�ړ����x�ύXID
        /// </summary>
        private int _changeSpeedId = -1;

        // �R���|�[�l���g�L���b�V��
        DroneMoveComponent _moveComponent = null;

        public void Initialize()
        {
            IBattleDrone drone = GetComponent<IBattleDrone>();

            // ���C������ǂݍ���
            MainWeapon = drone.MainWeapon;
            (MainWeapon as MonoBehaviour).transform.SetParent(_mainWeaponPos, false);
            MainWeapon.OnBulletFull += (o, e) =>
            {
                OnBulletFull?.Invoke(this, Weapon.Main, MainWeapon);
            };
            MainWeapon.OnBulletEmpty += (o, e) =>
            {
                OnBulletEmpty?.Invoke(this, Weapon.Main, MainWeapon);
            };

            // �T�u����ǂݍ���
            SubWeapon = drone.SubWeapon;
            (SubWeapon as MonoBehaviour).transform.SetParent(_subWeaponPos, false);
            SubWeapon.OnBulletFull += (o, e) =>
            {
                OnBulletFull?.Invoke(this, Weapon.Sub, SubWeapon);
            };
            SubWeapon.OnBulletEmpty += (o, e) =>
            {
                OnBulletEmpty?.Invoke(this, Weapon.Sub, SubWeapon);
            };
        }

        /// <summary>
        /// ������g�p���Ēe�۔���
        /// </summary>
        /// <param name="weapon">�g�p���镐��</param>
        /// <param name="target">�Ǐ]�Ώ�</param>
        public void Shot(Weapon weapon, GameObject target = null)
        {
            // ���C������U��
            if (weapon == Weapon.Main)
            {
                MainWeapon.Shot(target);

                // �U�����͑��x�ቺ
                if (!_mainShotHistory.PreviousValue)
                {
                    _changeSpeedId = _moveComponent.ChangeMoveSpeedPercent(MainSpeedDownPer);
                }

                // ���C���U���t���O�𗧂Ă�
                _mainShotHistory.CurrentValue = true;
            }

            // �T�u����U��
            if (weapon == Weapon.Sub)
            {
                SubWeapon.Shot(target);

                // �U�����͑��x�ቺ
                if (!_subShotHistory.PreviousValue)
                {
                    _changeSpeedId = _moveComponent.ChangeMoveSpeedPercent(SubSpeedDownPer);
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
                _moveComponent.ResetMoveSpeed(_changeSpeedId);
            }

            // �T�u����̍U�����~�����ꍇ�͑��x��߂�
            if (!_subShotHistory.CurrentValue && _subShotHistory.PreviousValue)
            {
                _moveComponent.ResetMoveSpeed(_changeSpeedId);
            }

            // ����g�p�����X�V
            _mainShotHistory.UpdateCurrentValue(false);
            _subShotHistory.UpdateCurrentValue(false);
        }
    }
}