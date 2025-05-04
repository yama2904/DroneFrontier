using Network;
using System;
using System.Linq;
using System.Text;

namespace Battle.Packet
{
    public class DroneDestroyPacket : BasePacket
    {
        /// <summary>
        /// プレイヤー名
        /// </summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// リスポーンドローンのオブジェクトID
        /// </summary>
        public string RespawnDroneId { get; private set; } = string.Empty;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public DroneDestroyPacket() { }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="name">プレイヤー名</param>
        /// <param name="newId">リスポーンドローンのオブジェクトID</param>
        public DroneDestroyPacket(string name, string newId)
        {
            Name = name;
            RespawnDroneId = string.IsNullOrEmpty(newId) ? string.Empty : newId;
        }

        protected override BasePacket ParseBody(byte[] body)
        {
            int offset = 0;

            // プレイヤー名長
            int nameLen = BitConverter.ToInt32(body, offset);
            offset += sizeof(int);

            // プレイヤー名
            string name = Encoding.UTF8.GetString(body, offset, nameLen);
            offset += nameLen;

            // ドローンID長
            int idLen = BitConverter.ToInt32(body, offset);
            offset += sizeof(int);

            // ドローンID
            string id = Encoding.UTF8.GetString(body, offset, idLen);
            offset += idLen;

            // インスタンスを作成して返す
            return new DroneDestroyPacket(name, id);
        }

        protected override byte[] ConvertToPacketBody()
        {
            // プレイヤー名
            byte[] name = Encoding.UTF8.GetBytes(Name);
            byte[] nameLen = BitConverter.GetBytes(name.Length);

            // ドローンID
            byte[] id = Encoding.UTF8.GetBytes(RespawnDroneId);
            byte[] idLen = BitConverter.GetBytes(id.Length);

            return nameLen.Concat(name)
                          .Concat(idLen)
                          .Concat(id)
                          .ToArray();
        }
    }
}
