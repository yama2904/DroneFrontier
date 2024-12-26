using System;
using System.Linq;

namespace Network.Tcp
{
    public abstract class TcpPacket : Packet
    {
        /// <summary>
        /// TCP�p�P�b�g�̃w�b�_���o�C�g��
        /// </summary>
        public const int TCP_HEADER_SIZE = 1;

        /// <summary>
        /// �p����N���X�̃w�b�_�[�^�C�v
        /// </summary>
        public abstract TcpHeader Header { get; }

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

            if (data == null || data.Length < TCP_HEADER_SIZE) return;

            // �w�b�_���؂�o��
            header = data.Take(TCP_HEADER_SIZE).ToArray();
            // �{�f�B���؂�o��
            body = data.Skip(TCP_HEADER_SIZE).ToArray();
        }

        /// <summary>
        /// TCP�p�P�b�g����TcpHeader�����o��
        /// </summary>
        /// <param name="data">TCP�p�P�b�g</param>
        /// <returns>�擾����TcpHeader</returns>
        public static TcpHeader GetTcpHeader(byte[] data)
        {
            if (data == null || data.Length < TCP_HEADER_SIZE) return TcpHeader.None;

            // �w�b�_���擾
            Split(data, out byte[] header, out _);

            // TcpHeader�֕ϊ�
            //return (TcpHeader)BitConverter.ToInt16(header);
            return (TcpHeader)header[0];
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
            return BitConverter.GetBytes((byte)Header);
        }
    }
}