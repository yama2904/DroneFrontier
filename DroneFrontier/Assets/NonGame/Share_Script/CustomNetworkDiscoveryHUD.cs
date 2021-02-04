using System.Collections.Generic;
using UnityEngine;

namespace Mirror.Discovery
{
    [DisallowMultipleComponent]
    [HelpURL("https://mirror-networking.com/docs/Articles/Components/NetworkDiscovery.html")]
    public class CustomNetworkDiscoveryHUD : MonoBehaviour
    {
        readonly Dictionary<long, ServerResponse> discoveredServers = new Dictionary<long, ServerResponse>();
        Vector2 scrollViewPos = Vector2.zero;

        public NewNetworkDiscovery networkDiscovery;
        long serverId = -1;
        bool isStartClient = false;

        //シングルトン
        static CustomNetworkDiscoveryHUD singleton;
        public static CustomNetworkDiscoveryHUD Singleton { get { return singleton; } }
        void Awake()
        {
            singleton = this;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (networkDiscovery == null)
            {
                networkDiscovery = GetComponent<NewNetworkDiscovery>();
                UnityEditor.Events.UnityEventTools.AddPersistentListener(networkDiscovery.OnServerFound, OnDiscoveredServer);
                UnityEditor.Undo.RecordObjects(new Object[] { this, networkDiscovery }, "Set NetworkDiscovery");
            }
        }
#endif


        private void Update()
        {
            if (serverId == -1) return;
            if (isStartClient) return;

            Connect(discoveredServers[serverId]);  //ヒットしたサーバに接続
            networkDiscovery.StopDiscovery();   //サーバ検索を停止
            isStartClient = true;
        }

        void Connect(ServerResponse info)
        {
            NetworkManager.singleton.StartClient(info.uri);
            Debug.Log("Connect");
        }

        public void OnDiscoveredServer(ServerResponse info)
        {
            // Note that you can check the versioning to decide if you can connect to the server or not using this method
            discoveredServers[info.serverId] = info;
            serverId = info.serverId;
            Debug.Log("OnDiscoveredServer");
        }

        public void StartHost()
        {
            if (NetworkManager.singleton == null)
                return;

            if (NetworkServer.active || NetworkClient.active)
                return;

            if (!NetworkClient.isConnected && !NetworkServer.active && !NetworkClient.active)
            {
                discoveredServers.Clear();
                NetworkManager.singleton.StartHost();
                networkDiscovery.AdvertiseServer();
            }
        }

        public void StartClient()
        {
            if (NetworkManager.singleton == null) return;

            if (NetworkServer.active || NetworkClient.active) return;

            if (NetworkClient.isConnected && NetworkServer.active && NetworkClient.active) return;

            discoveredServers.Clear();
            networkDiscovery.StartDiscovery();
        }

        private void OnDestroy()
        {
            networkDiscovery.StopDiscovery();
        }

        public void Init()
        {
            isStartClient = false;
            serverId = -1;
        }
    }
}