using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Mirror;

public class MatchingManager : NetworkBehaviour
{
    //シングルトン
    static MatchingManager singleton;
    public static MatchingManager Singleton { get { return singleton; } }

    public bool IsLocalPlayer { get { return isLocalPlayer; } }

    //生成する画像用
    [SerializeField] MatchingButtonsController matchingScreen = null;
    [SerializeField] NetworkWeaponSelectController weaponSelectScreen = null;
    static GameObject createMatchingScreen = null;

    //ルームに入ったプレイヤーの名前
    public static List<string> playerNames = new List<string>();

    //準備ができたか
    public static bool isReady = false;

    public class PlayerData
    {
        public string name;
        public NetworkConnection conn;
        public BaseWeapon.Weapon weapon;
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
        MatchingButtonsController mc = Instantiate(matchingScreen);
        mc.Init(isServer);
        createMatchingScreen = mc.gameObject;

        CmdAddPlayerData();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (isServer)
        {
            RpcSetPlayerList(playerNames.ToArray());
        }
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        //準備完了フラグのセット
        if (isReady)
        {
            isReady = false;
            GetComponent<NetworkRoomPlayer>().CmdChangeReadyState(true);
        }
    }


    //playerDatasリストの更新
    [Command]
    void CmdAddPlayerData()
    {
        playerDatas.Add(new PlayerData
        {
            name = playerNames[playerDatas.Count],
            conn = connectionToClient,
            weapon = BaseWeapon.Weapon.NONE
        });
    }

    //各クライアントのマッチングリストを更新する
    [ClientRpc]
    void RpcSetPlayerList(string[] names)
    {
        playerNames.Clear();
        playerNames = names.ToList();
        createMatchingScreen.GetComponent<MatchingButtonsController>().SetPlayerList(names);
    }

    public void Init()
    {
        playerNames.Clear();
        playerDatas.Clear();
        isReady = false;
    }

    //クライアントの退出
    public void ExitClient()
    {
        NetworkManager.singleton.StopClient();  //クライアントを停止
        Init();
        Mirror.Discovery.CustomNetworkDiscoveryHUD.Singleton.Init();
        NonGameManager.LoadNonGameScene(BaseScreenManager.Screen.KURIBOCCHI);
    }

    //切断したプレイヤーをリストから削除
    [ServerCallback]
    public void RemovePlayer(int index)
    {
        playerNames.RemoveAt(index);
        playerDatas.RemoveAt(index);
        RpcSetPlayerList(playerNames.ToArray());
    }


    //武器選択画面の生成
    public void CreateWeaponSelectScreen()
    {
        RpcDesroyMatchingScreen();
        NetworkWeaponSelectController ws = Instantiate(weaponSelectScreen);
        NetworkServer.Spawn(ws.gameObject, connectionToClient);
        ws.DisplayItemSelect();
    }

    //全てのクライアントからマッチング画面を削除
    [ClientRpc]
    void RpcDesroyMatchingScreen()
    {
        Destroy(createMatchingScreen);
    }

    //自分が装備する武器を設定
    [Command]
    public void CmdSetWeapon(int weapon)
    {
        //バグ防止
        if (weapon >= (int)BaseWeapon.Weapon.NONE) return;
        if (weapon < 0) return;

        int index = playerDatas.FindIndex(pd => ReferenceEquals(pd.conn, connectionToClient));
        if(index >= 0)
        {
            playerDatas[index].weapon = (BaseWeapon.Weapon)weapon;
        }
    }

    //レースモード用
    //すべてのクライアントの準備を完了させてゲームを開始する
    [ClientRpc]
    public void RpcStartRace()
    {
        isReady = true;
    }
}
