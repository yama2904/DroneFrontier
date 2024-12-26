using System;

namespace Network.Udp
{
    [Flags]
    public enum UdpHeader
    {
        None,

        /// <summary>
        /// プレイヤー探索
        /// </summary>
        Discover = 1 << 0,

        /// <summary>
        /// プレイヤー探索応答
        /// </summary>
        DiscoverResponse = 1 << 1,

        /// <summary>
        /// エラー
        /// </summary>
        Error = 1 << 2,
    }
}