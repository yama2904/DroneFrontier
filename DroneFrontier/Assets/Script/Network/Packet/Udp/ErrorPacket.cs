using System;

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

        protected override IPacket ParseBody(byte[] body)
        {
            ErrorCode code = (ErrorCode)BitConverter.ToInt32(body);
            return new ErrorPacket(code);
        }

        protected override byte[] ConvertToPacketBody()
        {
            return BitConverter.GetBytes((int)ErrorCode);
        }
    }
}