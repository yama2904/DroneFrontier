using System;
using System.Linq;

namespace Network.Tcp
{
    public abstract class TcpPacket : Packet
    {
        /// <summary>
        /// TCPパケットのヘッダ部バイト数
        /// </summary>
        public const int TCP_HEADER_SIZE = 1;

        /// <summary>
        /// 継承先クラスのヘッダータイプ
        /// </summary>
        public abstract TcpHeader Header { get; }

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

            if (data == null || data.Length < TCP_HEADER_SIZE) return;

            // ヘッダ部切り出し
            header = data.Take(TCP_HEADER_SIZE).ToArray();
            // ボディ部切り出し
            body = data.Skip(TCP_HEADER_SIZE).ToArray();
        }

        /// <summary>
        /// TCPパケットからTcpHeaderを取り出す
        /// </summary>
        /// <param name="data">TCPパケット</param>
        /// <returns>取得したTcpHeader</returns>
        public static TcpHeader GetTcpHeader(byte[] data)
        {
            if (data == null || data.Length < TCP_HEADER_SIZE) return TcpHeader.None;

            // ヘッダ部取得
            Split(data, out byte[] header, out _);

            // TcpHeaderへ変換
            //return (TcpHeader)BitConverter.ToInt16(header);
            return (TcpHeader)header[0];
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
            return BitConverter.GetBytes((byte)Header);
        }
    }
}