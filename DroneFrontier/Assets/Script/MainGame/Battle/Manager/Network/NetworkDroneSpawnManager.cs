using Common;
using Drone;
using Drone.Battle;
using Drone.Battle.Network;
using Network.Udp;
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
            NetworkBattleDrone drone = CreateDrone(name, spawnPos.position, spawnPos.rotation);
            IWeapon main = WeaponCreater.CreateWeapon(WeaponType.GATLING);
            IWeapon sub = WeaponCreater.CreateWeapon(weapon);
            drone.Initialize(name, main, sub, drone.StockNum);

            // �X�|�[�����_����ۑ�
            _initDatas.Add(drone.Name, (weapon, spawnPos));

            // ���̃X�|�[���ʒu
            _nextSpawnIndex++;
            if (_nextSpawnIndex >= _droneSpawnPositions.Length)
            {
                _nextSpawnIndex = 0;
            }

            // �X�|�[�����M
            MyNetworkManager.Singleton.SendToAll(new DroneSpawnPacket(name, 
                                                                      weapon, 
                                                                      drone.StockNum, 
                                                                      drone.enabled, 
                                                                      spawnPos.position, 
                                                                      spawnPos.rotation));

            return drone;
        }

        private void Awake()
        {
            // �����X�|�[���ʒu�������_���ɑI��
            _nextSpawnIndex = UnityEngine.Random.Range(0, _droneSpawnPositions.Length);

            // ��M�C�x���g�ݒ�
            MyNetworkManager.Singleton.OnUdpReceiveOnMainThread += OnUdpReceive;
        }

        /// <summary>
        /// �h���[������
        /// </summary>
        /// <param name="name">�h���[���ɐݒ肷�閼�O</param>
        /// <param name="pos">�X�|�[���ʒu</param>
        /// <param name="rotate">�X�|�[������</param>
        /// <returns>���������h���[��</returns>
        private NetworkBattleDrone CreateDrone(string name, Vector3 pos, Quaternion rotate)
        {
            NetworkBattleDrone createdDrone = Instantiate(_playerDrone, pos, rotate);
            createdDrone.DroneDestroyEvent += DroneDestroy;

            return createdDrone;
        }

        /// <summary>
        /// UDP�p�P�b�g��M�C�x���g
        /// </summary>
        /// <param name="name">�v���C���[��</param>
        /// <param name="header">��M����UDP�p�P�b�g�̃w�b�_</param>
        /// <param name="packet">��M����UDP�p�P�b�g</param>
        private void OnUdpReceive(string name, UdpHeader header, UdpPacket packet)
        {
            if (packet is DroneSpawnPacket spawn)
            {
                NetworkBattleDrone drone = CreateDrone(spawn.Name, spawn.Position, spawn.Rotation);
                drone.enabled = spawn.Enabled;
                IWeapon main = WeaponCreater.CreateWeapon(WeaponType.GATLING);
                IWeapon sub = WeaponCreater.CreateWeapon(spawn.Weapon);
                drone.Initialize(drone.Name, main, sub, spawn.StockNum);
            }
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
                respawnDrone = CreateDrone(drone.Name, initData.pos.position, initData.pos.rotation);
                respawnDrone.enabled = true;
                IWeapon main = WeaponCreater.CreateWeapon(WeaponType.GATLING);
                IWeapon sub = WeaponCreater.CreateWeapon(initData.weapon);
                respawnDrone.Initialize(drone.Name, main, sub, drone.StockNum - 1);

                // ����SE�Đ�
                respawnDrone.GetComponent<DroneSoundComponent>().Play(SoundManager.SE.Respawn);

                // �X�|�[�����M
                MyNetworkManager.Singleton.SendToAll(new DroneSpawnPacket(name,
                                                                          initData.weapon,
                                                                          drone.StockNum,
                                                                          drone.enabled,
                                                                          initData.pos.position,
                                                                          initData.pos.rotation));
            }

            // �C�x���g����
            DroneDestroyEvent?.Invoke(drone, respawnDrone);

            // �j�󂳂ꂽ�h���[������C�x���g�̍폜
            drone.DroneDestroyEvent -= DroneDestroy;
        }
    }
}