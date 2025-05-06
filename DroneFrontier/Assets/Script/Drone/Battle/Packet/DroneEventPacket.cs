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
            int bodyOffset = 0;
            
            // ビットフラグ取得
            byte bitFlag = body[bodyOffset];
            bodyOffset += sizeof(byte);

            // ドローン名取得
            int nameLen = BitConverter.ToInt32(body, bodyOffset);
            bodyOffset += sizeof(int);
            string name = Encoding.UTF8.GetString(body, bodyOffset, nameLen);
            bodyOffset += nameLen;

            // ビットフラグから各フラグ取得
            int bitOffset = 0;
            bool barrierBreak = BitFlagUtil.CheckFlag(bitFlag, bitOffset++);
            bool resurrectBarrier = BitFlagUtil.CheckFlag(bitFlag, bitOffset++);
            bool destroy = BitFlagUtil.CheckFlag(bitFlag, bitOffset++);

            // インスタンスを作成して返す
            return new DroneEventPacket(name, barrierBreak, resurrectBarrier, destroy);
        }

        protected override byte[] ConvertToPacketBody()
        {
            // ビットフラグ
            byte bitFlag = 0;
            int offset = 0;
            bitFlag = BitFlagUtil.UpdateFlag(bitFlag, offset++, BarrierBreak);
            bitFlag = BitFlagUtil.UpdateFlag(bitFlag, offset++, BarrierResurrect);
            bitFlag = BitFlagUtil.UpdateFlag(bitFlag, offset++, Destroy);

            // ドローン名
            byte[] name = Encoding.UTF8.GetBytes(Name);
            byte[] nameLen = BitConverter.GetBytes(name.Length);

            return new byte[] { bitFlag }
                    .Concat(nameLen)
                    .Concat(name)
                    .ToArray();
        }
    }
}