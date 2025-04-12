using System;

namespace Network.Tcp
{
    [Flags]
    public enum TcpHeader
    {
        None,

        /// <summary>
        /// クライアント同士の接続
        /// </summary>
        PeerConnect = 1 << 0,

        /// <summary>
        /// プレイヤー探索完了
        /// </summary>
        DiscoveryComplete = 1 << 1
    }
}