using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Mirror;

namespace Online
{
    public class MatchingManager : NetworkBehaviour
    {
        //シングルトン
        static MatchingManager singleton;
        public static MatchingManager Singleton { get { return singleton; } }

        //生成する画像用
        [SerializeField] MatchingScreenManager matchingScreen = null;
        [SerializeField] WeaponSelectManager weaponSelectScreen = null;
        static MatchingScreenManager createMatchingScreen = null;

        //接続中のプレイヤー情報
        public class PlayerData
        {
            public string name;
            public NetworkConnection conn;
            public BaseWeapon.Weapon weapon;
            public bool isReady;
        }
        public static List<PlayerData> playerDatas = new List<PlayerData>();

        //プレイヤーの数
        public static int PlayerNum { get { return playerDatas.Count; } }


        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            //シングルトンの作成
            singleton = this;

            //明るさの調整と画面の生成
            BrightnessManager.SetGameAlfa(0);
            MatchingScreenManager mc = Instantiate(matchingScreen);
            mc.Init(isServer);
            createMatchingScreen = mc;

            //サーバ以外は最初から準備完了フラグを立てる
            if (!isServer)
            {
                GetComponent<NetworkRoomPlayer>().CmdChangeReadyState(true);
            }

            CmdAddPlayer(SoloMultiSelectManager.playerName);
        }

        [ServerCallback]
        void Update()
        {
            if (MainGameManager.IsMainGaming) return;
            if (!isLocalPlayer) return;

            //準備完了フラグのセット
            int readyCount = 0;
            foreach (PlayerData pd in playerDatas)
            {
                if (pd.isReady)
                {
                    readyCount++;
                }
            }

            //すべてのクライアントの準備が完了したらBGMを止めてシーン移動
            if (readyCount == PlayerNum)
            {
                RpcStartGame();
            }
        }

        [ClientRpc]
        void RpcStopBGM()
        {
            SoundManager.StopBGM();
        }


        //新規接続してきたプレイヤーの情報を受信
        [Command]
        public void CmdAddPlayer(string name)
        {
            playerDatas.Add(new PlayerData
            {
                name = name,
                conn = connectionToClient
            });

            //更新したプレイヤー情報をリストに表示
            RpcSetPlayerList(playerDatas.Select(pd => pd.name).ToArray());
        }

        [ClientRpc]
        void RpcSetPlayerList(string[] names)
        {
            if (createMatchingScreen == null) return;
            createMatchingScreen.SetPlayerList(names);
        }

        public void Init()
        {
            playerDatas.Clear();
            createMatchingScreen = null;
        }

        //クライアントの退出
        public void ExitClient()
        {
            NetworkManager.singleton.StopClient();  //クライアントを停止
            Init();
            Mirror.Discovery.CustomNetworkDiscoveryHUD.Singleton.Init();
            HomeSceneManager.LoadHomeScene(BaseScreenManager.Screen.SOLO_MULTI_SELECT);
        }


        //武器選択画面の生成
        [Server]
        public void CreateWeaponSelectScreen()
        {
            RpcDesroyMatchingScreen();
            WeaponSelectManager ws = Instantiate(weaponSelectScreen);
            NetworkServer.Spawn(ws.gameObject, connectionToClient);
            ws.DisplayItemSelect();
        }

        //全てのクライアントからマッチング画面を削除
        [ClientRpc]
        void RpcDesroyMatchingScreen()
        {
            Destroy(createMatchingScreen.gameObject);
        }

        //自分が装備する武器を設定
        [Command]
        public void CmdSetWeapon(int weapon)
        {
            //バグ防止
            if (weapon >= (int)BaseWeapon.Weapon.NONE) return;
            if (weapon < 0) return;

            int index = playerDatas.FindIndex(pd => ReferenceEquals(pd.conn, connectionToClient));
            if (index >= 0)
            {
                playerDatas[index].weapon = (BaseWeapon.Weapon)weapon;
                playerDatas[index].isReady = true;
            }
        }

        //BGMを止めてゲームを開始する
        [ClientRpc]
        public void RpcStartGame()
        {
            SoundManager.StopBGM();
            if (isServer)
            {
                GetComponent<NetworkRoomPlayer>().CmdChangeReadyState(true);
            }
        }

        //接続が切れた時呼ぶ
        [Server]
        public void DisconnectPlayer(NetworkConnection conn)
        {
            int index = playerDatas.FindIndex(pd => ReferenceEquals(pd.conn, conn));
            if (index < 0) return;

            playerDatas.RemoveAt(index);
            if (!MainGameManager.IsMainGaming)
            {
                RpcSetPlayerList(playerDatas.Select(pd => pd.name).ToArray()); ;
            }
        }
    }
}
