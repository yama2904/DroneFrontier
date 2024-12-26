using System;
using System.Linq;

namespace Network.Udp
{
    public class ErrorPacket : UdpPacket
    {
        public override UdpHeader Header => UdpHeader.Error;

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

        public override Packet Parse(byte[] data)
        {
            // �{�f�B���擾
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