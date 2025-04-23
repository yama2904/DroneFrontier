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

            // �w�b�_����
            int headerSize = TCP_HEADER_TYPE_SIZE;

            // ���O��Ԃ̃o�C�g���v�Z
            int namespaceSize = BitConverter.ToInt32(data, headerSize);
            headerSize += sizeof(int) + namespaceSize;

            // �^���̃o�C�g���v�Z
            int typeSize = BitConverter.ToInt32(data, headerSize);
            headerSize += sizeof(int) + typeSize;

            // �A�Z���u�����̃o�C�g���v�Z
            int assemblySize = BitConverter.ToInt32(data, headerSize);
            headerSize += sizeof(int) + assemblySize;

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

            int offset = TCP_HEADER_TYPE_SIZE;

            // ���O��ԃT�C�Y���o��
            int namespaceSize = BitConverter.ToInt32(data, offset);
            offset += sizeof(int);

            // ���O��Ԏ��o��
            string namespaceName = Encoding.UTF8.GetString(data, offset, namespaceSize);
            offset += namespaceSize;

            // �^���T�C�Y���o��
            int typeSize = BitConverter.ToInt32(data, offset);
            offset += sizeof(int);

            // �^�����o��
            string typeName = Encoding.UTF8.GetString(data, offset, typeSize);
            offset += typeSize;

            // �A�Z���u�����T�C�Y���o��
            int assemblySize = BitConverter.ToInt32(data, offset);
            offset += sizeof(int);

            // �A�Z���u�������o��
            string assemblyName = Encoding.UTF8.GetString(data, offset, assemblySize);
            offset += assemblySize;

            // �^����ԋp
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

            // ���O���
            byte[] namespaceByte = Encoding.UTF8.GetBytes(GetType().Namespace ?? string.Empty);

            // ���O��Ԃ̃o�C�g��
            byte[] namespaceLen = BitConverter.GetBytes(namespaceByte.Length);

            // �^��
            byte[] typeNameByte = Encoding.UTF8.GetBytes(GetType().Name);

            // �^���̃o�C�g��
            byte[] typeNameLen = BitConverter.GetBytes(typeNameByte.Length);

            // �A�Z���u����
            byte[] assemblyByte = Encoding.UTF8.GetBytes(GetType().Assembly.GetName().Name);

            // �A�Z���u�����̃o�C�g��
            byte[] assemblyLen = BitConverter.GetBytes(assemblyByte.Length);

            // [�w�b�_�[�^�C�v][���O��ԃo�C�g��][���O���][�^���o�C�g��][�^��][�A�Z���u�����o�C�g��][�A�Z���u����]�Ō������ĕԂ�
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