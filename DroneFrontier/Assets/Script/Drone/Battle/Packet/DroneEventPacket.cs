using Common;
using Network;
using System.Linq;
using System.Text;

namespace Drone.Battle.Network
{
    public class DroneEventPacket : BasePacket
    {
        /// <summary>
        /// ドローン名
        /// </summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// バリア破壊
        /// </summary>
        public bool BarrierBreak { get; private set; } = false;

        /// <summary>
        /// バリア復活
        /// </summary>
        public bool BarrierResurrect { get; private set; } = false;

        /// <summary>
        /// ドローン死亡
        /// </summary>
        public bool Destroy { get; private set; } = false;

        /// <summary>
        /// コンストラクタ
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
            
            // ビットフラグ取得
            byte data = body[byteOffset];
            byteOffset += sizeof(byte);

            // ドローン名取得
            int nameLen = body.Length - byteOffset;
            string name = Encoding.UTF8.GetString(body, byteOffset, nameLen);
            byteOffset += nameLen;

            // ビットフラグから各フラグ取得
            int bitOffset = 0;
            bool barrierBreak = BitFlagUtil.CheckFlag(data, bitOffset++);
            bool resurrectBarrier = BitFlagUtil.CheckFlag(data, bitOffset++);
            bool destroy = BitFlagUtil.CheckFlag(data, bitOffset++);

            // インスタンスを作成して返す
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