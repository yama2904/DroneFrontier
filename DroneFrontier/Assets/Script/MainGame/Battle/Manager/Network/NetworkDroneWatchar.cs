using Drone.Battle.Network;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Battle.Network
{
    public class NetworkDroneWatchar : MonoBehaviour
    {
        [SerializeField, Tooltip("�h���[���X�|�[���Ǘ��I�u�W�F�N�g")]
        private NetworkDroneSpawnManager _droneSpawnManager = null;

        /// <summary>
        /// �ϐ풆�̃h���[��
        /// </summary>
        private List<NetworkBattleDrone> _watchDrones = new List<NetworkBattleDrone>();

        /// <summary>
        /// ���݃J�����Q�ƒ��̃h���[���̃C���f�b�N�X
        /// </summary>
        private int _watchingDrone = 0;

        private void Update()
        {
            if (_watchDrones.Count <= 0) return;

            // �X�y�[�X�L�[�Ŏ��̃h���[���փJ�����؂�ւ�
            if (Input.GetKeyDown(KeyCode.Space))
            {
                _watchDrones[_watchingDrone].IsWatch = false;

                // ���̃h���[��
                _watchingDrone++;
                if (_watchingDrone >= _watchDrones.Count)
                {
                    _watchingDrone = 0;
                }

                // �J�����Q�Ɛݒ�
                _watchDrones[_watchingDrone].IsWatch = true;
            }
        }

        private void OnEnable()
        {
            // �������̃h���[���擾
            _watchDrones = FindObjectsByType<NetworkBattleDrone>(FindObjectsSortMode.None).ToList();

            // �S�Ẵh���[���̃J�����Q�Ə�����
            foreach (NetworkBattleDrone drone in _watchDrones)
            {
                drone.IsWatch = false;
            }

            // �Q�Ɛ�J�����ݒ�
            _watchingDrone = 0;
            _watchDrones[_watchingDrone].IsWatch = true;

            // �h���[���j��C�x���g�ݒ�
            _droneSpawnManager.OnDroneDestroy += OnDroneDestroy;

            // AudioListener�L����
            GetComponent<AudioListener>().enabled = true;
        }


        private void OnDisable()
        {
            // �S�Ẵh���[���̃J�����Q�Ə�����
            foreach (NetworkBattleDrone drone in _watchDrones)
            {
                drone.IsWatch = false;
            }

            // �h���[���j��C�x���g�폜
            _droneSpawnManager.OnDroneDestroy -= OnDroneDestroy;

            // AudioListener������
            GetComponent<AudioListener>().enabled = false;
        }

        /// <summary>
        /// �h���[���j��C�x���g
        /// </summary>
        /// <param name="destroyDrone">�j�󂳂ꂽ�h���[��</param>
        /// <param name="respawnDrone">���X�|�[�������h���[��</param>
        private void OnDroneDestroy(NetworkBattleDrone destroyDrone, NetworkBattleDrone respawnDrone)
        {
            // �j�󂳂ꂽ�h���[�������X�|�[�������h���[���ɓ���ւ���
            int index = _watchDrones.IndexOf(destroyDrone);
            _watchDrones.RemoveAt(index);
            _watchDrones.Insert(index, respawnDrone);

            // �j�󂳂ꂽ�h���[�������݊ϐ풆�̃h���[���̏ꍇ�̓��X�|�[�������h���[��������
            if (index == _watchingDrone)
            {
                respawnDrone.IsWatch = true;
            }
        }
    }
}