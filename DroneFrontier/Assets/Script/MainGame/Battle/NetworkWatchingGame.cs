using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Network
{
    public class NetworkWatchingGame : MonoBehaviour
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
                // �J�����[�x������
                _watchDrones[_watchingDrone].Camera.depth = 0;

                // ���̃h���[��
                _watchingDrone++;
                if (_watchingDrone >= _watchDrones.Count)
                {
                    _watchingDrone = 0;
                }

                // �J�����Q�Ɛݒ�
                _watchDrones[_watchingDrone].Camera.depth = 5;
            }
        }

        private void OnEnable()
        {
            // �������̃h���[���擾
            _watchDrones = FindObjectsByType<NetworkBattleDrone>(FindObjectsSortMode.None).ToList();

            // �S�Ẵh���[���̃J�����[�x������
            foreach (NetworkBattleDrone drone in _watchDrones)
            {
                drone.Camera.depth = 0;
            }

            // �Q�Ɛ�J�����ݒ�
            _watchingDrone = 0;
            _watchDrones[_watchingDrone].Camera.depth = 5;

            // �h���[���j��C�x���g�ݒ�
            _droneSpawnManager.DroneDestroyEvent += DroneDestroy;

            // AudioListener�L����
            GetComponent<AudioListener>().enabled = true;
        }


        private void OnDisable()
        {
            // �S�Ẵh���[���̃J�����[�x������
            foreach (NetworkBattleDrone drone in _watchDrones)
            {
                drone.Camera.depth = 0;
            }

            // �h���[���j��C�x���g�폜
            _droneSpawnManager.DroneDestroyEvent -= DroneDestroy;

            // AudioListener������
            GetComponent<AudioListener>().enabled = false;
        }

        /// <summary>
        /// �h���[���j��C�x���g
        /// </summary>
        /// <param name="destroyDrone">�j�󂳂ꂽ�h���[��</param>
        /// <param name="respawnDrone">���X�|�[�������h���[��</param>
        private void DroneDestroy(NetworkBattleDrone destroyDrone, NetworkBattleDrone respawnDrone)
        {
            // �j�󂳂ꂽ�h���[�������X�|�[�������h���[���ɓ���ւ���
            int index = _watchDrones.IndexOf(destroyDrone);
            _watchDrones.RemoveAt(index);
            _watchDrones.Insert(index, respawnDrone);

            // �j�󂳂ꂽ�h���[�������݊ϐ풆�̃h���[���̏ꍇ�̓J�����[�x����
            if (index == _watchingDrone)
            {
                respawnDrone.Camera.depth = 5;
            }
            else
            {
                // �ϐ풆�h���[���łȂ��ꍇ�̓J�����[�x������
                respawnDrone.Camera.depth = 0;
            }
        }
    }
}