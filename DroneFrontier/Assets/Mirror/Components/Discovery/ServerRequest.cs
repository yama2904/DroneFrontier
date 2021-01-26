namespace Mirror.Discovery
{
    public struct ServerRequest : NetworkMessage
    {
        public string name;
        public bool isBattle;
    }
}
