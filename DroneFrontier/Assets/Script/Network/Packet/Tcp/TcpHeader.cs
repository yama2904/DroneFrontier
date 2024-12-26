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
        PeerConnect
    }
}