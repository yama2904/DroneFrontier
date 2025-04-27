using Battle.Packet;
using Network;
using Network.Udp;
using System;
using UnityEngine;

namespace Battle.Gimmick.Network
{
    public class NetworkMagnetAreaSpawner : MonoBehaviour
    {
        [SerializeField]
        private MagnetArea _magnetAreaPrefab = null;

        [SerializeField]
        private MagnetArea[] _magnetAreasOnScene = null;

        private void Start()
        {
            // ��M�C�x���g�ݒ�
            NetworkManager.Singleton.OnUdpReceiveOnMainThread += OnReceive;

            // �V�[����̎��C�G���A������
            foreach (MagnetArea area in _magnetAreasOnScene)
            {
                // �z�X�g�̏ꍇ�͔����C�x���g�ݒ�
                if (NetworkManager.Singleton.IsHost)
                {
                    area.OnSpawn += OnSpawn;
                }
                else
                {
                    // �N���C�A���g�̏ꍇ�͍폜
                    Destroy(area.gameObject);
                }
            }
        }

        private void OnDestroy()
        {
            // ��M�C�x���g�폜
            NetworkManager.Singleton.OnUdpReceiveOnMainThread -= OnReceive;

            // �z�X�g�̏ꍇ�̓V�[����̎��C�G���A����C�x���g�폜
            if (NetworkManager.Singleton.IsHost)
            {
                foreach (MagnetArea area in _magnetAreasOnScene)
                {
                    area.OnSpawn -= OnSpawn;
                }
            }
        }

        /// <summary>
        /// UDP�p�P�b�g��M�C�x���g
        /// </summary>
        /// <param name="name">�v���C���[��</param>
        /// <param name="header">��M����UDP�p�P�b�g�̃w�b�_</param>
        /// <param name="packet">��M����UDP�p�P�b�g</param>
        private void OnReceive(string name, UdpHeader header, UdpPacket packet)
        {
            if (packet is MagnetSpawnPacket magnetPacket)
            {
                // ��M����������Ɏ��C�G���A����
                MagnetArea area = Instantiate(_magnetAreaPrefab, magnetPacket.Position, magnetPacket.Rotation);
                area.DownPercent = magnetPacket.DownPercent;
                area.ActiveTime = magnetPacket.ActiveTime;
                area.MinAreaSize = magnetPacket.AreaSize;
                area.MaxAreaSize = magnetPacket.AreaSize;
                area.SpawnPercent = 100;
                area.SpawnInterval = 0;

                // �C�x���g�ݒ�
                area.OnDespawn += OnDespawn;
            }
        }

        /// <summary>
        /// ���C�G���A�����C�x���g
        /// </summary>
        /// <param name="sender">�C�x���g�I�u�W�F�N�g</param>
        /// <param name="e">�C�x���g����</param>
        private void OnSpawn(object sender, EventArgs e)
        {
            // �z�X�g�̂ݏ���
            if (NetworkManager.Singleton.IsClient) return;

            // ���������G���A�����N���C�A���g�֑��M
            MagnetArea area = sender as MagnetArea;
            MagnetSpawnPacket packet = new MagnetSpawnPacket(area.DownPercent, 
                                                             area.ActiveTime, 
                                                             area.CurrentAreaSize, 
                                                             area.gameObject.transform.position, 
                                                             area.gameObject.transform.rotation);
            NetworkManager.Singleton.SendToAll(packet);
        }

        /// <summary>
        /// ���C�G���A���ŃC�x���g
        /// </summary>
        /// <param name="sender">�C�x���g�I�u�W�F�N�g</param>
        /// <param name="e">�C�x���g����</param>
        private void OnDespawn(object sender, EventArgs e)
        {
            // �N���C�A���g�̂ݏ���
            if (NetworkManager.Singleton.IsHost) return;

            // ���ł������C�G���A�폜
            MagnetArea area = sender as MagnetArea;
            area.OnDespawn -= OnDespawn;
            Destroy(area.gameObject);
        }
    }
}