namespace Network
{
    /// <summary>
    /// パケット取扱用抽象クラス
    /// </summary>
    public interface Packet
    {
        /// <summary>
        /// パケットを解析して派生クラスのインスタンスを作成する
        /// </summary>
        /// <param name="data">解析元パケット</param>
        /// <returns>生成したインスタンス</returns>
        public Packet Parse(byte[] data);

        /// <summary>
        /// 派生クラスのインスタンスから送信用パケットへ変換する
        /// </summary>
        /// <returns>変換したパケット</returns>
        public byte[] ConvertToPacket();
    }
}