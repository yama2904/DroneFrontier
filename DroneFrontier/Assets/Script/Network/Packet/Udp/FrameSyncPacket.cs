using System;

namespace Network.Udp
{
    public class FrameSyncPacket : BasePacket
    {
        public float TotalSeconds { get; private set; } = 0;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public FrameSyncPacket() { }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="totalSeconds">経過時間</param>
        public FrameSyncPacket(float totalSeconds)
        {
            TotalSeconds = totalSeconds;
        }

        protected override BasePacket ParseBody(byte[] body)
        {
            // インスタンスを作成して返す
            return new FrameSyncPacket(BitConverter.ToSingle(body));
        }

        protected override byte[] ConvertToPacketBody()
        {
            return BitConverter.GetBytes(TotalSeconds);
        }
    }
}