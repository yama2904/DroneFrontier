using System;
using System.Linq;
using System.Text;

namespace Network.Udp
{
    public abstract class UdpPacket : IPacket
    {
        /// <summary>
        /// UDPパケットのヘッダータイプバイト数
        /// </summary>
        public const int UDP_HEADER_TYPE_SIZE = 2;

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

            if (data == null || data.Length < UDP_HEADER_TYPE_SIZE) return;

            // 型名のバイト長取得
            int typeSize = BitConverter.ToInt32(data, UDP_HEADER_TYPE_SIZE);

            // ヘッダ部長
            int headerSize = UDP_HEADER_TYPE_SIZE + sizeof(int) + typeSize;

            // ヘッダ部切り出し
            header = data.Take(headerSize).ToArray();
            // ボディ部切り出し
            body = data.Skip(headerSize).ToArray();
        }

        /// <summary>
        /// UDPパケットからUdpHeaderを取り出す
        /// </summary>
        /// <param name="data">UDPパケット</param>
        /// <returns>取得したUdpHeader</returns>
        public static UdpHeader GetUdpHeader(byte[] data)
        {
            if (data == null || data.Length < UDP_HEADER_TYPE_SIZE) return UdpHeader.None;

            // ヘッダ部取得
            Split(data, out byte[] header, out _);

            // UdpHeaderへ変換
            return (UdpHeader)BitConverter.ToInt16(header);
        }

        /// <summary>
        /// UDPパケットから型を取り出す
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Type GetUdpType(byte[] data)
        {
            if (data == null) return null;

            // ヘッダ部取得
            Split(data, out byte[] header, out _);

            // ヘッダ部から型名部分を切り出し
            byte[] typeData = header.Skip(UDP_HEADER_TYPE_SIZE + sizeof(int)).ToArray();

            // 型を返す
            string typeName = Encoding.UTF8.GetString(typeData);
            return Type.GetType($"Network.Udp.{typeName}, Assembly-CSharp");
        }

        /// <summary>
        /// パケットを解析して派生クラスのインスタンスを作成する
        /// </summary>
        /// <param name="data">解析元パケット</param>
        /// <returns>生成したインスタンス</returns>
        public IPacket Parse(byte[] data)
        {
            Split(data, out _, out byte[] body);
            return ParseBody(body);
        }

        /// <summary>
        /// 派生クラスのインスタンスから送信用パケットへ変換する
        /// </summary>
        /// <returns>変換したパケット</returns>
        public byte[] ConvertToPacket()
        {
            // ヘッダ部とボディ部を結合して返す
            return GetHeaderBytes()
                  .Concat(ConvertToPacketBody())
                  .ToArray();
        }

        /// <summary>
        /// ボディ部を解析して派生クラスのインスタンスを作成する
        /// </summary>
        /// <param name="body">ボディ部</param>
        /// <returns></returns>
        protected abstract IPacket ParseBody(byte[] body);

        /// <summary>
        /// 派生クラスのインスタンスをUDPパケットのボディ部へ変換する
        /// </summary>
        /// <returns>変換したパケット</returns>
        protected abstract byte[] ConvertToPacketBody();

        /// <summary>
        /// ヘッダ部のバイト配列を取得
        /// </summary>
        /// <returns></returns>
        private byte[] GetHeaderBytes()
        {
            // ヘッダータイプ
            byte[] header = BitConverter.GetBytes((short)Header);

            // 型名
            byte[] typeNameByte = Encoding.UTF8.GetBytes(GetType().Name);

            // 型名のバイト長
            byte[] typeNameLen = BitConverter.GetBytes(typeNameByte.Length);

            // [ヘッダータイプ][型名バイト長][型名]で結合して返す
            return header.Concat(typeNameLen).Concat(typeNameByte).ToArray();
        }
    }
}