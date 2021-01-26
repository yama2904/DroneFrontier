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

    //選択した武器
    BaseWeapon.Weapon selectWeapon = BaseWeapon.Weapon.NONE;
    [SyncVar] bool isReady = false;

    static bool isStarted = false;


    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        //シングルトンの作成
        singleton = this;
    }
    
    [ServerCallback]
    public override void OnStartClient()
    {
        base.OnStartClient();
        string[] names = new string[playerNames.Count];
        names = playerNames.ToArray();
        RpcAddPlayer(names);
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
        if(isReady)
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

        selectWeapon = (BaseWeapon.Weapon)weapon;
        isReady = true;
    }
}
