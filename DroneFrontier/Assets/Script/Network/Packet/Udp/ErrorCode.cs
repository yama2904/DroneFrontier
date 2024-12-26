namespace Network.Udp
{
    /// <summary>
    /// ErrorPacketクラスで扱うエラー一覧
    /// </summary>
    public enum ErrorCode
    {
        /// <summary>
        /// エラーなし
        /// </summary>
        NoError,

        /// <summary>
        /// 既に同じプレイヤー名が存在する
        /// </summary>
        ExistsName,
    }
}