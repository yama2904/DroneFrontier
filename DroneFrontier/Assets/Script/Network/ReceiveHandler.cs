namespace Network
{
    /// <summary>
    /// �p�P�b�g��M�C�x���g�n���h���[
    /// </summary>
    /// <param name="client">�C�x���g�I�u�W�F�N�g</param>
    /// <param name="packet">��M�����p�P�b�g</param>
    public delegate void ReceiveHandler(PeerClient client, BasePacket packet);
}
