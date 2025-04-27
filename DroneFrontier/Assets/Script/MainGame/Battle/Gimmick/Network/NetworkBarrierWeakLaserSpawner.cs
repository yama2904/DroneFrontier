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
            // 受信イベント設定
            NetworkManager.Singleton.OnUdpReceiveOnMainThread += OnReceive;

            // シーン上のレーザー初期化
            foreach (BarrierWeakLaser lazer in _lazersOnScene)
            {
                // ホストの場合は発生イベント設定
                if (NetworkManager.Singleton.IsHost)
                {
                    lazer.OnSpawn += OnSpawn;
                }
                else
                {
                    // クライアントの場合は削除
                    Destroy(lazer.gameObject);
                }
            }
        }

        private void OnDestroy()
        {
            // 受信イベント削除
            NetworkManager.Singleton.OnUdpReceiveOnMainThread -= OnReceive;

            // ホストの場合はシーン上のレーザーからイベント削除
            if (NetworkManager.Singleton.IsHost)
            {
                foreach (BarrierWeakLaser lazer in _lazersOnScene)
                {
                    lazer.OnSpawn -= OnSpawn;
                }
            }
        }

        /// <summary>
        /// UDPパケット受信イベント
        /// </summary>
        /// <param name="name">プレイヤー名</param>
        /// <param name="header">受信したUDPパケットのヘッダ</param>
        /// <param name="packet">受信したUDPパケット</param>
        private void OnReceive(string name, UdpHeader header, UdpPacket packet)
        {
            if (packet is BarrierWeakLaserPacket lazerPacket)
            {
                // 受信した情報を基にレーザー生成
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

                // イベント設定
                lazer.OnDespawn += OnDespawn;
            }
        }

        /// <summary>
        /// レーザー発生イベント
        /// </summary>
        /// <param name="sender">イベントオブジェクト</param>
        /// <param name="e">イベント引数</param>
        private void OnSpawn(object sender, EventArgs e)
        {
            // ホストのみ処理
            if (NetworkManager.Singleton.IsClient) return;

            // 発生したレーザー情報をクライアントへ送信
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
        /// レーザー消滅イベント
        /// </summary>
        /// <param name="sender">イベントオブジェクト</param>
        /// <param name="e">イベント引数</param>
        private void OnDespawn(object sender, EventArgs e)
        {
            // クライアントのみ処理
            if (NetworkManager.Singleton.IsHost) return;

            // 消滅したレーザー削除
            BarrierWeakLaser lazer = sender as BarrierWeakLaser;
            lazer.OnDespawn -= OnDespawn;
            Destroy(lazer.gameObject);
        }
    }
}