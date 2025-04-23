using System;
using System.Linq;
using System.Text;

namespace Network.Tcp
{
    public abstract class TcpPacket : IPacket
    {
        /// <summary>
        /// TCPパケットのヘッダータイプバイト数
        /// </summary>
        public const int TCP_HEADER_TYPE_SIZE = 1;

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

            if (data == null || data.Length < TCP_HEADER_TYPE_SIZE) return;

            // ヘッダ部長
            int headerSize = TCP_HEADER_TYPE_SIZE;

            // 名前空間のバイト長計算
            int namespaceSize = BitConverter.ToInt32(data, headerSize);
            headerSize += sizeof(int) + namespaceSize;

            // 型名のバイト長計算
            int typeSize = BitConverter.ToInt32(data, headerSize);
            headerSize += sizeof(int) + typeSize;

            // アセンブリ名のバイト長計算
            int assemblySize = BitConverter.ToInt32(data, headerSize);
            headerSize += sizeof(int) + assemblySize;

            // ヘッダ部切り出し
            header = data.Take(headerSize).ToArray();
            // ボディ部切り出し
            body = data.Skip(headerSize).ToArray();
        }

        /// <summary>
        /// TCPパケットからTcpHeaderを取り出す
        /// </summary>
        /// <param name="data">TCPパケット</param>
        /// <returns>取得したTcpHeader</returns>
        public static TcpHeader GetTcpHeader(byte[] data)
        {
            if (data == null || data.Length < TCP_HEADER_TYPE_SIZE) return TcpHeader.None;

            // ヘッダ部取得
            Split(data, out byte[] header, out _);

            // TcpHeaderへ変換
            //return (TcpHeader)BitConverter.ToInt16(header);
            return (TcpHeader)header[0];
        }

        /// <summary>
        /// TCPパケットから型を取り出す
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Type GetTcpType(byte[] data)
        {
            if (data == null) return null;

            int offset = TCP_HEADER_TYPE_SIZE;

            // 名前空間サイズ取り出し
            int namespaceSize = BitConverter.ToInt32(data, offset);
            offset += sizeof(int);

            // 名前空間取り出し
            string namespaceName = Encoding.UTF8.GetString(data, offset, namespaceSize);
            offset += namespaceSize;

            // 型名サイズ取り出し
            int typeSize = BitConverter.ToInt32(data, offset);
            offset += sizeof(int);

            // 型名取り出し
            string typeName = Encoding.UTF8.GetString(data, offset, typeSize);
            offset += typeSize;

            // アセンブリ名サイズ取り出し
            int assemblySize = BitConverter.ToInt32(data, offset);
            offset += sizeof(int);

            // アセンブリ名取り出し
            string assemblyName = Encoding.UTF8.GetString(data, offset, assemblySize);
            offset += assemblySize;

            // 型名を返却
            if (string.IsNullOrWhiteSpace(namespaceName))
            {
                return Type.GetType($"{typeName}, {assemblyName}");
            }
            else
            {
                return Type.GetType($"{namespaceName}.{typeName}, {assemblyName}");
            }
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
        /// 派生クラスのインスタンスをTCPパケットのボディ部へ変換する
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
            //byte[] header = BitConverter.GetBytes((short)Header);
            byte[] header = new byte[] { (byte)Header };

            // 名前空間
            byte[] namespaceByte = Encoding.UTF8.GetBytes(GetType().Namespace ?? string.Empty);

            // 名前空間のバイト長
            byte[] namespaceLen = BitConverter.GetBytes(namespaceByte.Length);

            // 型名
            byte[] typeNameByte = Encoding.UTF8.GetBytes(GetType().Name);

            // 型名のバイト長
            byte[] typeNameLen = BitConverter.GetBytes(typeNameByte.Length);

            // アセンブリ名
            byte[] assemblyByte = Encoding.UTF8.GetBytes(GetType().Assembly.GetName().Name);

            // アセンブリ名のバイト長
            byte[] assemblyLen = BitConverter.GetBytes(assemblyByte.Length);

            // [ヘッダータイプ][名前空間バイト長][名前空間][型名バイト長][型名][アセンブリ名バイト長][アセンブリ名]で結合して返す
            return header.Concat(namespaceLen)
                         .Concat(namespaceByte)
                         .Concat(typeNameLen)
                         .Concat(typeNameByte)
                         .Concat(assemblyLen)
                         .Concat(assemblyByte)
                         .ToArray();
        }
    }
}