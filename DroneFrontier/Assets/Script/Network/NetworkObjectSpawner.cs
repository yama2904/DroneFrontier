using Network.Udp;
using UnityEngine;

namespace Network
{
    /// <summary>
    /// 全ての通信相手とのゲームオブジェクトの生成・削除を管理するクラス
    /// </summary>
    public class NetworkObjectSpawner : MonoBehaviour
    {
        public static NetworkObjectSpawner Instance { get; private set; }

        /// <summary>
        /// オブジェクト共有ID採番値
        /// </summary>
        private long _numberingId = 1;

        ///// <summary>
        ///// 指定されたオブジェクトを全プレイヤーに生成させる
        ///// </summary>
        ///// <returns>採番した共有オブジェクトID</returns>
        //public void Spawn(MyNetworkBehaviour obj)
        //{
        //    // ID設定
        //    long id = _numberingId++;
        //    obj.ObjectId = id;

        //    // パケット送信
        //    IPacket packet = new SpawnPacket(obj);
        //    MyNetworkManager.Singleton.SendAsync(packet);
        //}

        //private void Awake()
        //{
        //    Instance = this;

        //    // 受信イベント設定
        //    MyNetworkManager.Singleton.OnUdpReceive += OnUdpReceive;
        //}

        //private void OnDestroy()
        //{
        //    // 受信イベント削除
        //    MyNetworkManager.Singleton.OnUdpReceive -= OnUdpReceive;
        //}

        ///// <summary>
        ///// UDPパケット受信イベント
        ///// </summary>
        ///// <param name="name">プレイヤー名</param>
        ///// <param name="header">受信したUDPパケットのヘッダ</param>
        ///// <param name="packet">受信したUDPパケット</param>
        //private void OnUdpReceive(string name, UdpHeader header, UdpPacket packet)
        //{
        //    // オブジェクト生成パケット以外は無視
        //    if (header != UdpHeader.Spawn) return;

        //    // オブジェクト生成
        //    MyNetworkBehaviour obj = (packet as SpawnPacket).SpawnObject;
        //    Instantiate(obj);
        //}
    }
}
