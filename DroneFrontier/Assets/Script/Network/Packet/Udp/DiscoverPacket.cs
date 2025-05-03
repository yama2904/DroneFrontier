using System;
using System.Linq;
using System.Text;

namespace Network.Udp
{
    internal class DiscoverPacket : BasePacket
    {
        /// <summary>
        /// プレイヤー名
        /// </summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// ゲームモード
        /// </summary>
        public string GameMode { get; private set; } = string.Empty;

        /// <summary>
        /// TCPリッスンポート番号
        /// </summary>
        public int ListenPort { get; private set; } = 0;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public DiscoverPacket() { }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="name">プレイヤー名</param>
        /// <param name="gameMode">ゲームモード</param>
        /// <param name="listenPort">TCPリッスンポート番号</param>
        public DiscoverPacket(string name, string gameMode, int listenPort)
        {
            Name = name;
            GameMode = gameMode;
            ListenPort = listenPort;
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

            // ゲームモード長
            int modeLen = BitConverter.ToInt32(body, offset);
            offset += sizeof(int);

            // ゲームモード
            string mode = Encoding.UTF8.GetString(body, offset, modeLen);
            offset += modeLen;

            // ポート
            int port = BitConverter.ToInt32(body, offset);
            offset += sizeof(int);

            // インスタンスを作成して返す
            return new DiscoverPacket(name, mode, port);
        }

        protected override byte[] ConvertToPacketBody()
        {
            // プレイヤー名
            byte[] name = Encoding.UTF8.GetBytes(Name);
            byte[] nameLen = BitConverter.GetBytes(name.Length);

            // ゲームモード
            byte[] mode = Encoding.UTF8.GetBytes(GameMode);
            byte[] modeLen = BitConverter.GetBytes(mode.Length);

            // ポート
            byte[] port = BitConverter.GetBytes(ListenPort);

            return nameLen.Concat(name)
                          .Concat(modeLen)
                          .Concat(mode)
                          .Concat(port)
                          .ToArray();
        }
    }
}