using Battle.Packet;
using Network;
using Network.Udp;
using System;
using UnityEngine;

namespace Battle.Gimmick.Network
{
    public class NetworkBarrierWeakLaserSpawner : MonoBehaviour
    {
        [SerializeField]
        private BarrierWeakLaser _lazerPrefab = null;

        [SerializeField]
        private BarrierWeakLaser[] _lazersOnScene = null;

        private void Start()
        {
            // ��M�C�x���g�ݒ�
            NetworkManager.Singleton.OnUdpReceiveOnMainThread += OnReceive;

            // �V�[����̃��[�U�[������
            foreach (BarrierWeakLaser lazer in _lazersOnScene)
            {
                // �z�X�g�̏ꍇ�͔����C�x���g�ݒ�
                if (NetworkManager.Singleton.IsHost)
                {
                    lazer.OnSpawn += OnSpawn;
                }
                else
                {
                    // �N���C�A���g�̏ꍇ�͍폜
                    Destroy(lazer.gameObject);
                }
            }
        }

        private void OnDestroy()
        {
            // ��M�C�x���g�폜
            NetworkManager.Singleton.OnUdpReceiveOnMainThread -= OnReceive;

            // �z�X�g�̏ꍇ�̓V�[����̃��[�U�[����C�x���g�폜
            if (NetworkManager.Singleton.IsHost)
            {
                foreach (BarrierWeakLaser lazer in _lazersOnScene)
                {
                    lazer.OnSpawn -= OnSpawn;
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
            if (packet is BarrierWeakLaserPacket lazerPacket)
            {
                // ��M����������Ƀ��[�U�[����
                BarrierWeakLaser lazer = Instantiate(_lazerPrefab, lazerPacket.Position, lazerPacket.Rotation);
                lazer.WeakTime = lazerPacket.WeakTime;
                lazer.LazerRange = lazerPacket.LazerRange;
                lazer.LazerRadius = lazerPacket.LazerRadius;
                lazer.LaserTime = lazerPacket.LaserTime;
                lazer.MinRotateSpeed = lazerPacket.RotateSpeed;
                lazer.MaxRotateSpeed = lazerPacket.RotateSpeed;
                lazer.MinAngle = lazer.transform.localEulerAngles.x;
                lazer.MaxAngle = lazer.transform.localEulerAngles.x;
                lazer.MinInterval = 0;
                lazer.MaxInterval = 0;

                // �C�x���g�ݒ�
                lazer.OnDespawn += OnDespawn;
            }
        }

        /// <summary>
        /// ���[�U�[�����C�x���g
        /// </summary>
        /// <param name="sender">�C�x���g�I�u�W�F�N�g</param>
        /// <param name="e">�C�x���g����</param>
        private void OnSpawn(object sender, EventArgs e)
        {
            // �z�X�g�̂ݏ���
            if (NetworkManager.Singleton.IsClient) return;

            // �����������[�U�[�����N���C�A���g�֑��M
            BarrierWeakLaser lazer = sender as BarrierWeakLaser;
            BarrierWeakLaserPacket packet = new BarrierWeakLaserPacket(lazer.WeakTime,
                                                                       lazer.LazerRange,
                                                                       lazer.LazerRadius,
                                                                       lazer.LaserTime,
                                                                       lazer.CurrentRotateSpeed,
                                                                       lazer.gameObject.transform.position,
                                                                       lazer.gameObject.transform.rotation);
            NetworkManager.Singleton.SendToAll(packet);
        }

        /// <summary>
        /// ���[�U�[���ŃC�x���g
        /// </summary>
        /// <param name="sender">�C�x���g�I�u�W�F�N�g</param>
        /// <param name="e">�C�x���g����</param>
        private void OnDespawn(object sender, EventArgs e)
        {
            // �N���C�A���g�̂ݏ���
            if (NetworkManager.Singleton.IsHost) return;

            // ���ł������[�U�[�폜
            BarrierWeakLaser lazer = sender as BarrierWeakLaser;
            lazer.OnDespawn -= OnDespawn;
            Destroy(lazer.gameObject);
        }
    }
}