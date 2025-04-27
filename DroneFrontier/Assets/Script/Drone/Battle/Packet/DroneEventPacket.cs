using Common;
using Network;
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
            int byteOffset = 0;
            
            // �r�b�g�t���O�擾
            byte data = body[byteOffset];
            byteOffset += sizeof(byte);

            // �h���[�����擾
            int nameLen = body.Length - byteOffset;
            string name = Encoding.UTF8.GetString(body, byteOffset, nameLen);
            byteOffset += nameLen;

            // �r�b�g�t���O����e�t���O�擾
            int bitOffset = 0;
            bool barrierBreak = BitFlagUtil.CheckFlag(data, bitOffset++);
            bool resurrectBarrier = BitFlagUtil.CheckFlag(data, bitOffset++);
            bool destroy = BitFlagUtil.CheckFlag(data, bitOffset++);

            // �C���X�^���X���쐬���ĕԂ�
            return new DroneEventPacket(name, barrierBreak, resurrectBarrier, destroy);
        }

        protected override byte[] ConvertToPacketBody()
        {
            byte bitFlag = 0;
            int offset = 0;
            bitFlag = BitFlagUtil.UpdateFlag(bitFlag, offset++, BarrierBreak);
            bitFlag = BitFlagUtil.UpdateFlag(bitFlag, offset++, BarrierResurrect);
            bitFlag = BitFlagUtil.UpdateFlag(bitFlag, offset++, Destroy);
            return new byte[] { bitFlag }
                    .Concat(Encoding.UTF8.GetBytes(Name))
                    .ToArray();
        }
    }
}