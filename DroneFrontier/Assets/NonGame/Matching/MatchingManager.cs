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
    static bool isReady = false;

    public class PlayerData
    {
        public string name;
        public NetworkConnection conn;
        public BaseWeapon.Weapon weapon;
    }
    public static List<PlayerData> playerDatas = new List<PlayerData>();

    static bool isStarted = false;


    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        //シングルトンの作成
        singleton = this;

        CmdAddPlayerData();
    }

    [ServerCallback]
    public override void OnStartClient()
    {
        base.OnStartClient();

        string[] names = new string[playerNames.Count];
        names = playerNames.ToArray();
        RpcAddPlayer(names);
    }

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

    [ClientRpc]
    void RpcAddPlayer(string[] names)
    {
        if (!isStarted)
        {
            BrightnessManager.SetGameAlfa(0);
            MatchingButtonsController mc = Instantiate(matchingScreen);
            mc.Init(isServer);
            createMatchingScreen = mc.gameObject;

            isStarted = true;
        }
        playerNames.Clear();
        playerNames = names.ToList();
        createMatchingScreen.GetComponent<MatchingButtonsController>().SetPlayerList(names);
    }

    void Update()
    {
        if (!isLocalPlayer) return;
        if (isReady)
        {
            GetComponent<NetworkRoomPlayer>().CmdChangeReadyState(true);
        }
    }

    public void CreateWeaponSelectScreen()
    {
        RpcDesroyMatchingScreen();
        NetworkWeaponSelectController ws = Instantiate(weaponSelectScreen);
        NetworkServer.Spawn(ws.gameObject, connectionToClient);
        ws.DisplayItemSelect();
    }

    [ClientRpc]
    void RpcDesroyMatchingScreen()
    {
        Destroy(createMatchingScreen);
    }

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
        isReady = true;
    }

    //レースモード用
    //すべてのクライアントの準備を完了させてゲームを開始する
    [ClientRpc]
    public void RpcStartRace()
    {
        isReady = true;
    }
}
