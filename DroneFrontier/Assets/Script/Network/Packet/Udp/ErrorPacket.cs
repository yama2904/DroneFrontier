using System;
using System.Linq;

namespace Network.Udp
{
    public class ErrorPacket : UdpPacket
    {
        public override UdpHeader Header => UdpHeader.Error;

        /// <summary>
        /// エラーコード
        /// </summary>
        public ErrorCode ErrorCode { get; private set; } = ErrorCode.NoError;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ErrorPacket() { }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="code">エラーコード</param>
        public ErrorPacket (ErrorCode code)
        {
            ErrorCode = code;
        }

        public override Packet Parse(byte[] data)
        {
            // ボディ部取得
            Split(data, out _, out byte[] body);

            ErrorCode code = (ErrorCode)BitConverter.ToInt32(body);
            return new ErrorPacket(code);
        }

        public override byte[] ConvertToPacket()
        {
            byte[] code = BitConverter.GetBytes((int)ErrorCode);
            return GetHeaderBytes().Concat(code).ToArray();
        }
    }
}