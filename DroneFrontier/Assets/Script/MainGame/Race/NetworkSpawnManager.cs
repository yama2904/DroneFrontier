using Drone.Race.Network;
using UnityEngine;

namespace Race.Network
{
    public class NetworkSpawnManager : MonoBehaviour
    {
        [SerializeField, Tooltip("�v���C���[�h���[��")]
        private NetworkRaceDrone _playerDrone = null;

        [SerializeField, Tooltip("�h���[���X�|�[���ʒu")]
        private Transform[] _droneSpawnPositions = null;

        /// <summary>
        /// ���̃X�|�[�����Ɏg�p����z��C���f�b�N�X
        /// </summary>
        private int _nextSpawnIndex = -1;

        /// <summary>
        /// �h���[�����X�|�[��������
        /// </summary>
        /// <param name="name">�X�|�[��������h���[���̖��O</param>
        /// <param name="weapon">�X�|�[��������h���[���̃T�u����</param>
        /// <returns>�X�|�[���������h���[��</returns>
        public NetworkRaceDrone SpawnDrone(string name)
        {
            // �X�|�[���ʒu�擾
            Transform spawnPos = _droneSpawnPositions[_nextSpawnIndex];

            // �h���[������
            NetworkRaceDrone drone = Instantiate(_playerDrone, spawnPos.position, spawnPos.rotation);
            drone.Initialize(name);
            drone.enabled = false;

            // ���̃X�|�[���ʒu
            _nextSpawnIndex++;
            if (_nextSpawnIndex >= _droneSpawnPositions.Length)
            {
                _nextSpawnIndex = 0;
            }

            return drone;
        }

        private void Awake()
        {
            // �����X�|�[���ʒu�������_���ɑI��
            _nextSpawnIndex = UnityEngine.Random.Range(0, _droneSpawnPositions.Length);
        }
    }
}