using System;

namespace Network.Udp
{
    public class ErrorPacket : BasePacket
    {
        /// <summary>
        /// �G���[�R�[�h
        /// </summary>
        public ErrorCode ErrorCode { get; private set; } = ErrorCode.NoError;

        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        public ErrorPacket() { }

        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        /// <param name="code">�G���[�R�[�h</param>
        public ErrorPacket (ErrorCode code)
        {
            ErrorCode = code;
        }

        protected override BasePacket ParseBody(byte[] body)
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