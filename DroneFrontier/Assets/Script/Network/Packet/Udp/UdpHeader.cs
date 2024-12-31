using System;

namespace Network.Udp
{
    [Flags]
    public enum UdpHeader
    {
        None,

        /// <summary>
        /// エラー
        /// </summary>
        Error = 1 << 0,

        /// <summary>
        /// プレイヤー探索
        /// </summary>
        Discover = 1 << 1,

        /// <summary>
        /// プレイヤー探索応答
        /// </summary>
        DiscoverResponse = 1 << 2,

        /// <summary>
        /// 実行メソッド送信
        /// </summary>
        SendMethod = 1 << 3,
    }
}