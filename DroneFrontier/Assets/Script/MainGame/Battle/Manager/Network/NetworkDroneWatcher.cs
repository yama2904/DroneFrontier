using Battle.Packet;
using Common;
using Cysharp.Threading.Tasks;
using Drone.Battle;
using Drone.Battle.Network;
using Network;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace Battle.Network
{
    public class NetworkDroneWatcher : MonoBehaviour
    {
        [SerializeField, Tooltip("�h���[���X�|�[���Ǘ��I�u�W�F�N�g")]
        private NetworkDroneSpawnManager _droneSpawnManager = null;

        /// <summary>
        /// �ϐ풆�̃h���[��
        /// </summary>
        private static List<(string name, NetworkBattleDrone drone)> _watchDrones = new List<(string name, NetworkBattleDrone drone)>();

        /// <summary>
        /// ���݃J�����Q�ƒ��̃h���[���̃C���f�b�N�X
        /// </summary>
        private static int _watchingDrone = 0;

        private static CancellationTokenSource _cancel = new CancellationTokenSource();
        private static bool _isRunning = false;

        public static void Run()
        {
            if (_isRunning) return;
            _isRunning = true;
            _cancel = new CancellationTokenSource();

            // �������̃v���C���[�擾
            _watchDrones = GameObject.FindGameObjectsWithTag(TagNameConst.PLAYER)
                                     .Where(x => !Useful.IsNullOrDestroyed(x))
                                     .Select(x =>
                                     {
                                         var drone = x.GetComponent<NetworkBattleDrone>();
                                         return (drone.Name, drone);
                                     })
                                     .ToList();

            // �S�Ẵh���[���̃J�����Q�Ə�����
            foreach (var drone in _watchDrones)
            {
                drone.drone.IsWatch = false;
            }

            // �Q�Ɛ�J�����ݒ�
            _watchingDrone = 0;
            _watchDrones[_watchingDrone].drone.IsWatch = true;
        }

        private void Awake()
        {
            // �C�x���g�ݒ�
            NetworkManager.OnTcpReceived += OnTcpReceived;
            NetworkManager.OnUdpReceivedOnMainThread += OnUdpReceived;
            _droneSpawnManager.OnDroneDestroy += OnDroneDestroy;
        }

        private void Update()
        {
            if (_watchDrones.Count <= 0) return;

            // �X�y�[�X�L�[�Ŏ��̃v���C���[�փJ�����؂�ւ�
            if (Input.GetKeyDown(KeyCode.Space))
            {
                WatchNextDrone();
            }
        }

        private void OnDestroy()
        {
            _isRunning = false;
            _cancel.Cancel();

            // �C�x���g�폜
            NetworkManager.OnTcpReceived -= OnTcpReceived;
            NetworkManager.OnUdpReceivedOnMainThread -= OnUdpReceived;
            _droneSpawnManager.OnDroneDestroy -= OnDroneDestroy;
        }

        private void OnTcpReceived(string name, BasePacket packet)
        {
            if (packet is DroneWatchPacket)
            {
                Run();
            }
        }

        /// <summary>
        /// �h���[���j��C�x���g
        /// </summary>
        /// <param name="destroyDrone">�j�󂳂ꂽ�h���[��</param>
        /// <param name="respawnDrone">���X�|�[�������h���[��</param>
        private void OnDroneDestroy(IBattleDrone destroyDrone, IBattleDrone respawnDrone)
        {
            UpdateWatchDrones(destroyDrone.Name, respawnDrone);
        }

        /// <summary>
        /// UDP��M�C�x���g
        /// </summary>
        /// <param name="player">���M���v���C���[</param>
        /// <param name="packet">��M����UDP�p�P�b�g</param>
        protected virtual async void OnUdpReceived(string player, BasePacket packet)
        {
            if (packet is DroneDestroyPacket destroy)
            {
                string newId = destroy.RespawnDroneId;

                IBattleDrone respawnDrone = null;
                if (!string.IsNullOrEmpty(newId))
                {
                    while (true)
                    {
                        if (NetworkObjectSpawner.SpawnedObjects.ContainsKey(newId))
                        {
                            respawnDrone = NetworkObjectSpawner.SpawnedObjects[newId] as NetworkBattleDrone;
                            break;
                        }
                        await UniTask.Delay(1, cancellationToken: _cancel.Token);
                    }
                }

                UpdateWatchDrones(destroy.Name, respawnDrone);
            }
        }

        /// <summary>
        /// �ϐ풆�h���[�����X�g�X�V
        /// </summary>
        /// <param name="player">�X�V����h���[���̃v���C���[��</param>
        /// <param name="respawnDrone">���X�|�[���h���[��</param>
        private void UpdateWatchDrones(string player, IBattleDrone respawnDrone)
        {
            if (!_isRunning) return;

            // �j�󂳂ꂽ�h���[�������X�g����폜
            int droneIndex = _watchDrones.FindIndex(x => x.name == player);
            if (droneIndex >= 0)
            {
                _watchDrones.RemoveAt(droneIndex);
            }

            // ���X�|�[���h���[���擾
            var drone = respawnDrone as NetworkBattleDrone;

            // ���X�|�[�����ꂽ�ꍇ�͍ēx�ϐ�Ώۂɒǉ�
            if (respawnDrone != null)
            {
                if (droneIndex >= 0)
                {
                    _watchDrones.Insert(droneIndex, (drone.Name, drone));
                }
                else
                {
                    _watchDrones.Add((drone.Name, drone));
                }
            }

            // �j�󂳂ꂽ�h���[�������݊ϐ풆�̃v���C���[�̏ꍇ
            if (droneIndex == _watchingDrone)
            {
                // �c�@0�̏ꍇ�͎��̃v���C���[�֐؂�ւ�
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
            if (_watchingDrone < _watchDrones.Count
                && _watchDrones[_watchingDrone].drone != null)
            {
                _watchDrones[_watchingDrone].drone.IsWatch = false;
            }

            // ���̃v���C���[
            _watchingDrone++;
            if (_watchingDrone >= _watchDrones.Count)
            {
                _watchingDrone = 0;
            }

            // �J�����Q�Ɛݒ�i�Ώۂ��j�󂳂�Ă���ꍇ�͕s�������N���Ă��邽�ߍ폜���Ď��̃J�����֐؂�ւ���j
            if (_watchDrones[_watchingDrone].drone == null)
            {
                _watchDrones.RemoveAt(_watchingDrone);
                WatchNextDrone();
            }
            else
            {
                _watchDrones[_watchingDrone].drone.IsWatch = true;
            }
        }
    }
}