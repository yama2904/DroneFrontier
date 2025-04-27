using Battle.Weapon;
using Common;
using Drone;
using Drone.Battle;
using Drone.Battle.Network;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Battle.Network
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
        /// �e�h���[���̏������
        /// </summary>
        private Dictionary<string, (WeaponType weapon, Transform pos)> _initDatas = new Dictionary<string, (WeaponType weapon, Transform pos)>();

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
            NetworkBattleDrone drone = CreateDrone(spawnPos.position, spawnPos.rotation);
            IWeapon main = WeaponCreater.CreateWeapon(WeaponType.GATLING);
            IWeapon sub = WeaponCreater.CreateWeapon(weapon);
            drone.Initialize(name, main, sub, drone.StockNum);
            drone.enabled = false;

            // �X�|�[�����_����ۑ�
            _initDatas.Add(drone.Name, (weapon, spawnPos));

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
        /// <param name="pos">�X�|�[���ʒu</param>
        /// <param name="rotate">�X�|�[������</param>
        /// <returns>���������h���[��</returns>
        private NetworkBattleDrone CreateDrone(Vector3 pos, Quaternion rotate)
        {
            NetworkBattleDrone createdDrone = Instantiate(_playerDrone, pos, rotate);
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

            // �j�󂳂ꂽ�h���[���̏������擾
            var initData = _initDatas[drone.Name];

            // ���X�|�[���������h���[��
            NetworkBattleDrone respawnDrone = null;

            if (drone.StockNum > 0)
            {
                // ���X�|�[��
                respawnDrone = CreateDrone(initData.pos.position, initData.pos.rotation);
                respawnDrone.enabled = true;
                IWeapon main = WeaponCreater.CreateWeapon(WeaponType.GATLING);
                IWeapon sub = WeaponCreater.CreateWeapon(initData.weapon);
                respawnDrone.Initialize(drone.Name, main, sub, drone.StockNum - 1);

                // ����SE�Đ�
                respawnDrone.GetComponent<DroneSoundComponent>().Play(SoundManager.SE.Respawn);
            }

            // �C�x���g����
            DroneDestroyEvent?.Invoke(drone, respawnDrone);

            // �j�󂳂ꂽ�h���[������C�x���g�̍폜
            drone.DroneDestroyEvent -= DroneDestroy;
        }
    }
}