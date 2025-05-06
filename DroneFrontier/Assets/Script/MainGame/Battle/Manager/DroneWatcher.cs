using Battle.Drone;
using Drone.Battle;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Battle
{
    public class DroneWatcher : MonoBehaviour
    {
        [SerializeField, Tooltip("�h���[���X�|�[���Ǘ��I�u�W�F�N�g")]
        private DroneSpawnManager _droneSpawnManager = null;

        /// <summary>
        /// �ϐ풆�̃h���[��
        /// </summary>
        private static List<CpuBattleDrone> _watchDrones = new List<CpuBattleDrone>();

        /// <summary>
        /// ���݃J�����Q�ƒ��̃h���[���̃C���f�b�N�X
        /// </summary>
        private static int _watchingDrone = 0;

        private static bool _isRunning = false;

        public static void Run()
        {
            if (_isRunning) return;
            _isRunning = true;

            // ��������CPU�擾
            _watchDrones = FindObjectsByType<CpuBattleDrone>(FindObjectsSortMode.None).ToList();

            // �S�Ẵh���[���̃J�����Q�Ə�����
            foreach (CpuBattleDrone drone in _watchDrones)
            {
                drone.IsWatch = false;
            }

            // �Q�Ɛ�J�����ݒ�
            _watchingDrone = 0;
            _watchDrones[_watchingDrone].IsWatch = true;
        }

        private void Awake()
        {
            // �C�x���g�ݒ�
            _droneSpawnManager.OnDroneDestroy += OnDroneDestroy;
        }

        private void Update()
        {
            if (_watchDrones.Count <= 0) return;

            // �X�y�[�X�L�[�Ŏ���CPU�փJ�����؂�ւ�
            if (Input.GetKeyDown(KeyCode.Space))
            {
                WatchNextDrone();
            }
        }

        private void OnDestroy()
        {
            _isRunning = false;

            // �C�x���g�폜
            _droneSpawnManager.OnDroneDestroy -= OnDroneDestroy;
        }

        /// <summary>
        /// �h���[���j��C�x���g
        /// </summary>
        /// <param name="destroyDrone">�j�󂳂ꂽ�h���[��</param>
        /// <param name="respawnDrone">���X�|�[�������h���[��</param>
        private void OnDroneDestroy(IBattleDrone destroyDrone, IBattleDrone respawnDrone)
        {
            if (!_isRunning) return;

            // �j�󂳂ꂽ�h���[�������X�g����폜
            int droneIndex = _watchDrones.IndexOf(destroyDrone as CpuBattleDrone);
            _watchDrones.RemoveAt(droneIndex);

            // ���X�|�[���h���[���擾
            var drone = respawnDrone as CpuBattleDrone;

            // ���X�|�[�����ꂽ�ꍇ�͍ēx�ϐ�Ώۂɒǉ�
            if (respawnDrone != null)
            {
                _watchDrones.Insert(droneIndex, drone);
            }

            // �j�󂳂ꂽ�h���[�������݊ϐ풆��CPU�̏ꍇ
            if (droneIndex == _watchingDrone)
            {
                // �c�@0�̏ꍇ�͎���CPU�֐؂�ւ�
                if (respawnDrone == null)
                {
                    WatchNextDrone();
                }
                else
                {
                    // �c�@���c���Ă��ă��X�|�[�������ꍇ�̓��X�|�[���h���[���֐؂�ւ�
                    drone.IsWatch = true;
                }
            }
        }

        /// <summary>
        /// ���̃h���[���փJ������؂�ւ���
        /// </summary>
        private void WatchNextDrone()
        {
            _watchDrones[_watchingDrone].IsWatch = false;

            // ����CPU
            _watchingDrone++;
            if (_watchingDrone >= _watchDrones.Count)
            {
                _watchingDrone = 0;
            }

            // �J�����Q�Ɛݒ�
            _watchDrones[_watchingDrone].IsWatch = true;
        }
    }
}