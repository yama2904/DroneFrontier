using Common;
using Network;
using System;
using System.Linq;
using System.Text;

namespace Drone.Battle.Network
{
    public class DroneEventPacket : BasePacket
    {
        /// <summary>
        /// �h���[����
        /// </summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// �o���A�j��
        /// </summary>
        public bool BarrierBreak { get; private set; } = false;

        /// <summary>
        /// �o���A����
        /// </summary>
        public bool BarrierResurrect { get; private set; } = false;

        /// <summary>
        /// �h���[�����S
        /// </summary>
        public bool Destroy { get; private set; } = false;

        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        public DroneEventPacket() { }

        public DroneEventPacket(string name, bool barrierBreak, bool resurrectBarrier, bool destroy)
        {
            Name = name;
            BarrierBreak = barrierBreak;
            BarrierResurrect = resurrectBarrier;
            Destroy = destroy;
        }

        protected override BasePacket ParseBody(byte[] body)
        {
            int bodyOffset = 0;
            
            // �r�b�g�t���O�擾
            byte bitFlag = body[bodyOffset];
            bodyOffset += sizeof(byte);

            // �h���[�����擾
            int nameLen = BitConverter.ToInt32(body, bodyOffset);
            bodyOffset += sizeof(int);
            string name = Encoding.UTF8.GetString(body, bodyOffset, nameLen);
            bodyOffset += nameLen;

            // �r�b�g�t���O����e�t���O�擾
            int bitOffset = 0;
            bool barrierBreak = BitFlagUtil.CheckFlag(bitFlag, bitOffset++);
            bool resurrectBarrier = BitFlagUtil.CheckFlag(bitFlag, bitOffset++);
            bool destroy = BitFlagUtil.CheckFlag(bitFlag, bitOffset++);

            // �C���X�^���X���쐬���ĕԂ�
            return new DroneEventPacket(name, barrierBreak, resurrectBarrier, destroy);
        }

        protected override byte[] ConvertToPacketBody()
        {
            // �r�b�g�t���O
            byte bitFlag = 0;
            int offset = 0;
            bitFlag = BitFlagUtil.UpdateFlag(bitFlag, offset++, BarrierBreak);
            bitFlag = BitFlagUtil.UpdateFlag(bitFlag, offset++, BarrierResurrect);
            bitFlag = BitFlagUtil.UpdateFlag(bitFlag, offset++, Destroy);

            // �h���[����
            byte[] name = Encoding.UTF8.GetBytes(Name);
            byte[] nameLen = BitConverter.GetBytes(name.Length);

            return new byte[] { bitFlag }
                    .Concat(nameLen)
                    .Concat(name)
                    .ToArray();
        }
    }
}