namespace Network
{
    /// <summary>
    /// パケット受信イベントハンドラー
    /// </summary>
    /// <param name="client">イベントオブジェクト</param>
    /// <param name="packet">受信したパケット</param>
    public delegate void ReceiveHandler(PeerClient client, BasePacket packet);
}
