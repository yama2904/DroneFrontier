using System.Collections.Generic;
using UnityEngine;

namespace Mirror.Discovery
{
    [DisallowMultipleComponent]
    //[AddComponentMenu("Network/NetworkDiscoveryHUD")]
    [HelpURL("https://mirror-networking.com/docs/Articles/Components/NetworkDiscovery.html")]
    //[RequireComponent(typeof(NetworkDiscovery))]
    public class CustomNetworkDiscoveryHUD : MonoBehaviour
    {
        readonly Dictionary<long, ServerResponse> discoveredServers = new Dictionary<long, ServerResponse>();
        Vector2 scrollViewPos = Vector2.zero;

        public NewNetworkDiscovery networkDiscovery;
        long serverId = -1;
        bool isStartClient = false;

        static CustomNetworkDiscoveryHUD instance;
        public static CustomNetworkDiscoveryHUD Instance { get { return instance; } }
        void Awake()
        {
            instance = this;
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

            Debug.Log("Update");
            Connect(discoveredServers[serverId]);
            networkDiscovery.StopDiscovery();
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
    }
}
