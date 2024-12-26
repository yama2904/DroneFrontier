using System.Linq;
using System;

namespace Network.Udp
{
    public abstract class UdpPacket : Packet
    {
        /// <summary>
        /// UDPパケットのヘッダ部バイト数
        /// </summary>
        public const int UDP_HEADER_SIZE = 2;

        /// <summary>
        /// 継承先クラスのヘッダータイプ
        /// </summary>
        public abstract UdpHeader Header { get; }

        /// <summary>
        /// パケットをヘッダ部とボディ部へ分割
        /// </summary>
        /// <param name="data">分割元パケット</param>
        /// <param name="header">分割したヘッダ部</param>
        /// <param name="body">分割したボディ部</param>
        public static void Split(byte[] data, out byte[] header, out byte[] body)
        {
            header = new byte[0];
            body = new byte[0];

            if (data == null || data.Length < UDP_HEADER_SIZE) return;

            // ヘッダ部切り出し
            header = data.Take(UDP_HEADER_SIZE).ToArray();
            // ボディ部切り出し
            body = data.Skip(UDP_HEADER_SIZE).ToArray();
        }

        /// <summary>
        /// UDPパケットからUdpHeaderを取り出す
        /// </summary>
        /// <param name="data">UDPパケット</param>
        /// <returns>取得したUdpHeader</returns>
        public static UdpHeader GetUdpHeader(byte[] data)
        {
            if (data == null || data.Length < UDP_HEADER_SIZE) return UdpHeader.None;

            // ヘッダ部取得
            Split(data, out byte[] header, out _);

            // UdpHeaderへ変換
            return (UdpHeader)BitConverter.ToInt16(header);
        }

        /// <summary>
        /// パケットを解析して派生クラスのインスタンスを作成する
        /// </summary>
        /// <param name="data">解析元パケット</param>
        /// <returns>生成したインスタンス</returns>
        public abstract Packet Parse(byte[] data);

        /// <summary>
        /// 派生クラスのインスタンスから送信用パケットへ変換する
        /// </summary>
        /// <returns>変換したパケット</returns>
        public abstract byte[] ConvertToPacket();

        /// <summary>
        /// ヘッダ部のバイト配列を取得
        /// </summary>
        /// <returns></returns>
        protected byte[] GetHeaderBytes()
        {
            return BitConverter.GetBytes((short)Header);
        }
    }
}