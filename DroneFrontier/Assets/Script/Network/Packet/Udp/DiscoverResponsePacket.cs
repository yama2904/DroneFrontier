using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Network.Udp
{
    internal class DiscoverResponsePacket : BasePacket
    {
        /// <summary>
        /// �z�X�g�̃v���C���[��
        /// </summary>
        public string HostName { get; private set; } = string.Empty;

        /// <summary>
        /// �e�N���C�A���g��TCP�A�h���X
        /// </summary>
        public List<string> ClientAddresses = new List<string>();

        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        public DiscoverResponsePacket() { }

        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        /// <param name="hostName">�z�X�g�̃v���C���[��</param>
        /// <param name="clientAdrs">�e�N���C�A���g��TCP�A�h���X</param>
        public DiscoverResponsePacket(string hostName, List<string> clientAdrs)
        {
            HostName = hostName;
            ClientAddresses = clientAdrs;
        }

        protected override BasePacket ParseBody(byte[] body)
        {
            int offset = 0;

            // �z�X�g�v���C���[���̃o�C�g���擾
            int hostLen = BitConverter.ToInt32(body, offset);
            offset += sizeof(int);

            // �z�X�g�v���C���[���擾
            string hostName = Encoding.UTF8.GetString(body, offset, hostLen);
            offset += hostLen;

            // �v���C���[���擾
            int num = BitConverter.ToInt32(body, offset);
            offset += sizeof(int);

            // �e�v���C���[��TCP�A�h���X�擾
            List<string> addresses = new List<string>();
            for (int i = 0; i < num; i++)
            {
                // �A�h���X�̃o�C�g���擾
                int addressLen = BitConverter.ToInt32(body, offset);
                offset += sizeof(int);

                // �A�h���X�擾
                string address = Encoding.UTF8.GetString(body, offset, addressLen);
                offset += addressLen;

                addresses.Add(address);
            }

            // �C���X�^���X���쐬���ĕԂ�
            return new DiscoverResponsePacket(hostName, addresses);
        }

        protected override byte[] ConvertToPacketBody()
        {
            // �߂�l
            byte[] body;

            // �z�X�g�̃v���C���[�����o�C�g�ϊ�
            byte[] hostByte = Encoding.UTF8.GetBytes(HostName);

            // �z�X�g�v���C���[���̃o�C�g�����擾
            byte[] hostLen = BitConverter.GetBytes(hostByte.Length);

            // �v���C���[�����p�P�b�g�Ɍ���
            byte[] num = BitConverter.GetBytes(ClientAddresses.Count);

            // [�z�X�g�v���C���[���o�C�g��] [�z�X�g�v���C���[��] [�v���C���[��] �̏��Ɍ���
            body = hostLen.Concat(hostByte).Concat(num).ToArray();

            // TCP�A�h���X���p�P�b�g�Ɍ�������
            foreach (string address in ClientAddresses)
            {
                // �A�h���X���o�C�g�ϊ�
                byte[] addressByte = Encoding.UTF8.GetBytes(address);

                // �A�h���X�̃o�C�g�����擾
                byte[] addressLen = BitConverter.GetBytes(addressByte.Length);

                // [�A�h���X�o�C�g��] [�A�h���X] �̏��Ɍ���
                body = body.Concat(addressLen).Concat(addressByte).ToArray();
            }

            return body;
        }
    }
}