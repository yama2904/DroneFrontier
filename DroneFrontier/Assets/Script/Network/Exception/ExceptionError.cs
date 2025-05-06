namespace Network
{
    /// <summary>
    /// NetworkExceptionクラスで扱うエラー一覧
    /// </summary>
    public enum ExceptionError
    {
        /// <summary>
        /// エラーなし
        /// </summary>
        NoError,

        /// <summary>
        /// 既に同じプレイヤー名が存在する
        /// </summary>
        ExistsName,

        /// <summary>
        /// 想定外のエラー
        /// </summary>
        UnexpectedError
    }
}
