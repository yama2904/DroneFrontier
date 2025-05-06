namespace Network.Tcp
{
    /// <summary>
    /// クライアント同士の接続完了通知パケット
    /// </summary>
    internal class ConnectedClientsPacket : BasePacket
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ConnectedClientsPacket() { }

        protected override BasePacket ParseBody(byte[] body)
        {
            return new ConnectedClientsPacket();
        }

        protected override byte[] ConvertToPacketBody()
        {
            return new byte[0];
        }
    }
}
