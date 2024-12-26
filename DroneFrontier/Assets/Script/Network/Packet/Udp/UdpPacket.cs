using System.Linq;
using System;

namespace Network.Udp
{
    public abstract class UdpPacket : Packet
    {
        /// <summary>
        /// UDP�p�P�b�g�̃w�b�_���o�C�g��
        /// </summary>
        public const int UDP_HEADER_SIZE = 2;

        /// <summary>
        /// �p����N���X�̃w�b�_�[�^�C�v
        /// </summary>
        public abstract UdpHeader Header { get; }

        /// <summary>
        /// �p�P�b�g���w�b�_���ƃ{�f�B���֕���
        /// </summary>
        /// <param name="data">�������p�P�b�g</param>
        /// <param name="header">���������w�b�_��</param>
        /// <param name="body">���������{�f�B��</param>
        public static void Split(byte[] data, out byte[] header, out byte[] body)
        {
            header = new byte[0];
            body = new byte[0];

            if (data == null || data.Length < UDP_HEADER_SIZE) return;

            // �w�b�_���؂�o��
            header = data.Take(UDP_HEADER_SIZE).ToArray();
            // �{�f�B���؂�o��
            body = data.Skip(UDP_HEADER_SIZE).ToArray();
        }

        /// <summary>
        /// UDP�p�P�b�g����UdpHeader�����o��
        /// </summary>
        /// <param name="data">UDP�p�P�b�g</param>
        /// <returns>�擾����UdpHeader</returns>
        public static UdpHeader GetUdpHeader(byte[] data)
        {
            if (data == null || data.Length < UDP_HEADER_SIZE) return UdpHeader.None;

            // �w�b�_���擾
            Split(data, out byte[] header, out _);

            // UdpHeader�֕ϊ�
            return (UdpHeader)BitConverter.ToInt16(header);
        }

        /// <summary>
        /// �p�P�b�g����͂��Ĕh���N���X�̃C���X�^���X���쐬����
        /// </summary>
        /// <param name="data">��͌��p�P�b�g</param>
        /// <returns>���������C���X�^���X</returns>
        public abstract Packet Parse(byte[] data);

        /// <summary>
        /// �h���N���X�̃C���X�^���X���瑗�M�p�p�P�b�g�֕ϊ�����
        /// </summary>
        /// <returns>�ϊ������p�P�b�g</returns>
        public abstract byte[] ConvertToPacket();

        /// <summary>
        /// �w�b�_���̃o�C�g�z����擾
        /// </summary>
        /// <returns></returns>
        protected byte[] GetHeaderBytes()
        {
            return BitConverter.GetBytes((short)Header);
        }
    }
}