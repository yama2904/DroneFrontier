using System;
using System.Collections.Generic;
using UnityEngine;

namespace Network
{
    public class NetworkDroneSpawnManager : MonoBehaviour
    {
        /// <summary>
        /// �h���[���j��C�x���g
        /// </summary>
        /// <param name="destroyDrone">�j�󂳂ꂽ�h���[��</param>
        /// <param name="respawnDrone">���X�|�[�������h���[���i�c�@�������Ȃ����ꍇ��null�j</param>
        public delegate void DroneDestroyHandler(NetworkBattleDrone destroyDrone, NetworkBattleDrone respawnDrone);

        /// <summary>
        /// �h���[���j��C�x���g
        /// </summary>
        public event DroneDestroyHandler DroneDestroyEvent;

        [SerializeField, Tooltip("�v���C���[�h���[��")]
        private NetworkBattleDrone _playerDrone = null;

        [SerializeField, Tooltip("�h���[���X�|�[���ʒu")]
        private Transform[] _droneSpawnPositions = null;

        /// <summary>
        /// �e�h���[���̏����ʒu
        /// </summary>
        private Dictionary<string, Transform> _initPositions = new Dictionary<string, Transform>();

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
        public NetworkBattleDrone SpawnDrone(string name, WeaponType weapon)
        {
            // �X�|�[���ʒu�擾
            Transform spawnPos = _droneSpawnPositions[_nextSpawnIndex];

            // �h���[������
            NetworkBattleDrone drone = CreateDrone(name, weapon, spawnPos);

            // �X�|�[���ʒu��ۑ�
            _initPositions.Add(drone.Name, spawnPos);

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

        /// <summary>
        /// �h���[������
        /// </summary>
        /// <param name="weapon">�h���[���ɐݒ肷�閼�O</param>
        /// <param name="weapon">�ݒ肷�镐��</param>
        /// <param name="spawnPosition">�X�|�[���ʒu</param>
        /// <returns>���������h���[��</returns>
        private NetworkBattleDrone CreateDrone(string name, WeaponType weapon, Transform spawnPosition)
        {
            NetworkBattleDrone createdDrone = Instantiate(_playerDrone, spawnPosition.position, spawnPosition.rotation);
            createdDrone.Name = name;
            createdDrone.SubWeapon = weapon;
            createdDrone.DroneDestroyEvent += DroneDestroy;

            return createdDrone;
        }

        /// <summary>
        /// �h���[���j��C�x���g
        /// </summary>
        /// <param name="sender">�C�x���g�I�u�W�F�N�g</param>
        /// <param name="e">�C�x���g����</param>
        private void DroneDestroy(object sender, EventArgs e)
        {
            NetworkBattleDrone drone = sender as NetworkBattleDrone;

            // �j�󂳂ꂽ�h���[���̏����ʒu�擾
            Transform initPos = _initPositions[drone.Name];

            // ���X�|�[���������h���[��
            NetworkBattleDrone respawnDrone = null;

            if (drone.StockNum > 0)
            {
                // ���X�|�[��
                respawnDrone = CreateDrone(drone.Name, drone.SubWeapon, initPos);

                // ����SE�Đ�
                respawnDrone.GetComponent<DroneSoundComponent>().Play(SoundManager.SE.Respawn);

                // �X�g�b�N���X�V
                respawnDrone.StockNum = drone.StockNum - 1;
            }

            // �C�x���g����
            DroneDestroyEvent?.Invoke(drone, respawnDrone);

            // �j�󂳂ꂽ�h���[������C�x���g�̍폜
            drone.DroneDestroyEvent -= DroneDestroy;
        }
    }
}