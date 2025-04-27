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
            // 受信イベント設定
            NetworkManager.Singleton.OnUdpReceiveOnMainThread += OnReceive;

            // シーン上の磁気エリア初期化
            foreach (MagnetArea area in _magnetAreasOnScene)
            {
                // ホストの場合は発生イベント設定
                if (NetworkManager.Singleton.IsHost)
                {
                    area.OnSpawn += OnSpawn;
                }
                else
                {
                    // クライアントの場合は削除
                    Destroy(area.gameObject);
                }
            }
        }

        private void OnDestroy()
        {
            // 受信イベント削除
            NetworkManager.Singleton.OnUdpReceiveOnMainThread -= OnReceive;

            // ホストの場合はシーン上の磁気エリアからイベント削除
            if (NetworkManager.Singleton.IsHost)
            {
                foreach (MagnetArea area in _magnetAreasOnScene)
                {
                    area.OnSpawn -= OnSpawn;
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
            if (packet is MagnetSpawnPacket magnetPacket)
            {
                // 受信した情報を基に磁気エリア生成
                MagnetArea area = Instantiate(_magnetAreaPrefab, magnetPacket.Position, magnetPacket.Rotation);
                area.DownPercent = magnetPacket.DownPercent;
                area.ActiveTime = magnetPacket.ActiveTime;
                area.MinAreaSize = magnetPacket.AreaSize;
                area.MaxAreaSize = magnetPacket.AreaSize;
                area.SpawnPercent = 100;
                area.SpawnInterval = 0;

                // イベント設定
                area.OnDespawn += OnDespawn;
            }
        }

        /// <summary>
        /// 磁気エリア発生イベント
        /// </summary>
        /// <param name="sender">イベントオブジェクト</param>
        /// <param name="e">イベント引数</param>
        private void OnSpawn(object sender, EventArgs e)
        {
            // ホストのみ処理
            if (NetworkManager.Singleton.IsClient) return;

            // 発生したエリア情報をクライアントへ送信
            MagnetArea area = sender as MagnetArea;
            MagnetSpawnPacket packet = new MagnetSpawnPacket(area.DownPercent, 
                                                             area.ActiveTime, 
                                                             area.CurrentAreaSize, 
                                                             area.gameObject.transform.position, 
                                                             area.gameObject.transform.rotation);
            NetworkManager.Singleton.SendToAll(packet);
        }

        /// <summary>
        /// 磁気エリア消滅イベント
        /// </summary>
        /// <param name="sender">イベントオブジェクト</param>
        /// <param name="e">イベント引数</param>
        private void OnDespawn(object sender, EventArgs e)
        {
            // クライアントのみ処理
            if (NetworkManager.Singleton.IsHost) return;

            // 消滅した磁気エリア削除
            MagnetArea area = sender as MagnetArea;
            area.OnDespawn -= OnDespawn;
            Destroy(area.gameObject);
        }
    }
}