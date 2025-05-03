using System;
using System.Linq;
using System.Text;

namespace Network.Udp
{
    internal class DiscoverPacket : BasePacket
    {
        /// <summary>
        /// �v���C���[��
        /// </summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// �Q�[�����[�h
        /// </summary>
        public string GameMode { get; private set; } = string.Empty;

        /// <summary>
        /// TCP���b�X���|�[�g�ԍ�
        /// </summary>
        public int ListenPort { get; private set; } = 0;

        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        public DiscoverPacket() { }

        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        /// <param name="name">�v���C���[��</param>
        /// <param name="gameMode">�Q�[�����[�h</param>
        /// <param name="listenPort">TCP���b�X���|�[�g�ԍ�</param>
        public DiscoverPacket(string name, string gameMode, int listenPort)
        {
            Name = name;
            GameMode = gameMode;
            ListenPort = listenPort;
        }

        protected override BasePacket ParseBody(byte[] body)
        {
            int offset = 0;

            // �v���C���[����
            int nameLen = BitConverter.ToInt32(body, offset);
            offset += sizeof(int);

            // �v���C���[��
            string name = Encoding.UTF8.GetString(body, offset, nameLen);
            offset += nameLen;

            // �Q�[�����[�h��
            int modeLen = BitConverter.ToInt32(body, offset);
            offset += sizeof(int);

            // �Q�[�����[�h
            string mode = Encoding.UTF8.GetString(body, offset, modeLen);
            offset += modeLen;

            // �|�[�g
            int port = BitConverter.ToInt32(body, offset);
            offset += sizeof(int);

            // �C���X�^���X���쐬���ĕԂ�
            return new DiscoverPacket(name, mode, port);
        }

        protected override byte[] ConvertToPacketBody()
        {
            // �v���C���[��
            byte[] name = Encoding.UTF8.GetBytes(Name);
            byte[] nameLen = BitConverter.GetBytes(name.Length);

            // �Q�[�����[�h
            byte[] mode = Encoding.UTF8.GetBytes(GameMode);
            byte[] modeLen = BitConverter.GetBytes(mode.Length);

            // �|�[�g
            byte[] port = BitConverter.GetBytes(ListenPort);

            return nameLen.Concat(name)
                          .Concat(modeLen)
                          .Concat(mode)
                          .Concat(port)
                          .ToArray();
        }
    }
}