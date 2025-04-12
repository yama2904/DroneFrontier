using System;
using System.Linq;
using System.Text;

namespace Network.Tcp
{
    public abstract class TcpPacket : IPacket
    {
        /// <summary>
        /// TCP�p�P�b�g�̃w�b�_�[�^�C�v�o�C�g��
        /// </summary>
        public const int TCP_HEADER_TYPE_SIZE = 1;

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

            if (data == null || data.Length < TCP_HEADER_TYPE_SIZE) return;

            // �^���̃o�C�g���擾
            int typeSize = BitConverter.ToInt32(data, TCP_HEADER_TYPE_SIZE);

            // �w�b�_����
            int headerSize = TCP_HEADER_TYPE_SIZE + sizeof(int) + typeSize;

            // �w�b�_���؂�o��
            header = data.Take(headerSize).ToArray();
            // �{�f�B���؂�o��
            body = data.Skip(headerSize).ToArray();
        }

        /// <summary>
        /// TCP�p�P�b�g����TcpHeader�����o��
        /// </summary>
        /// <param name="data">TCP�p�P�b�g</param>
        /// <returns>�擾����TcpHeader</returns>
        public static TcpHeader GetTcpHeader(byte[] data)
        {
            if (data == null || data.Length < TCP_HEADER_TYPE_SIZE) return TcpHeader.None;

            // �w�b�_���擾
            Split(data, out byte[] header, out _);

            // TcpHeader�֕ϊ�
            //return (TcpHeader)BitConverter.ToInt16(header);
            return (TcpHeader)header[0];
        }

        /// <summary>
        /// TCP�p�P�b�g����^�����o��
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Type GetTcpType(byte[] data)
        {
            if (data == null) return null;

            // �w�b�_���擾
            Split(data, out byte[] header, out _);

            // �w�b�_������^��������؂�o��
            byte[] typeData = header.Skip(TCP_HEADER_TYPE_SIZE + sizeof(int)).ToArray();

            // �^��Ԃ�
            string typeName = Encoding.UTF8.GetString(typeData);
            return Type.GetType($"Network.Tcp.{typeName}, Assembly-CSharp");
        }

        /// <summary>
        /// �p�P�b�g����͂��Ĕh���N���X�̃C���X�^���X���쐬����
        /// </summary>
        /// <param name="data">��͌��p�P�b�g</param>
        /// <returns>���������C���X�^���X</returns>
        public IPacket Parse(byte[] data)
        {
            Split(data, out _, out byte[] body);
            return ParseBody(body);
        }

        /// <summary>
        /// �h���N���X�̃C���X�^���X���瑗�M�p�p�P�b�g�֕ϊ�����
        /// </summary>
        /// <returns>�ϊ������p�P�b�g</returns>
        public byte[] ConvertToPacket()
        {
            // �w�b�_���ƃ{�f�B�����������ĕԂ�
            return GetHeaderBytes()
                  .Concat(ConvertToPacketBody())
                  .ToArray();
        }

        /// <summary>
        /// �{�f�B������͂��Ĕh���N���X�̃C���X�^���X���쐬����
        /// </summary>
        /// <param name="body">�{�f�B��</param>
        /// <returns></returns>
        protected abstract IPacket ParseBody(byte[] body);

        /// <summary>
        /// �h���N���X�̃C���X�^���X��TCP�p�P�b�g�̃{�f�B���֕ϊ�����
        /// </summary>
        /// <returns>�ϊ������p�P�b�g</returns>
        protected abstract byte[] ConvertToPacketBody();

        /// <summary>
        /// �w�b�_���̃o�C�g�z����擾
        /// </summary>
        /// <returns></returns>
        private byte[] GetHeaderBytes()
        {
            // �w�b�_�[�^�C�v
            //byte[] header = BitConverter.GetBytes((short)Header);
            byte[] header = new byte[] { (byte)Header };

            // �^��
            byte[] typeNameByte = Encoding.UTF8.GetBytes(GetType().Name);

            // �^���̃o�C�g��
            byte[] typeNameLen = BitConverter.GetBytes(typeNameByte.Length);

            // [�w�b�_�[�^�C�v][�^���o�C�g��][�^��]�Ō������ĕԂ�
            return header.Concat(typeNameLen).Concat(typeNameByte).ToArray();
        }
    }
}