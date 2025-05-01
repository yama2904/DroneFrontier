namespace Network.Connect
{
    /// <summary>
    /// コネクションイベントハンドラー
    /// </summary>
    /// <param name="sender">イベントオブジェクト</param>
    /// <param name="client">接続情報</param>
    public delegate void ConnectionHandler(object sender, PeerClient client);
}
