using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Mirror;

public class MatchingManager : NetworkBehaviour
{
    static MatchingManager singleton;
    public static MatchingManager Singleton { get { return singleton; } }
    void Awake()
    {
        singleton = this;
    }

    [SerializeField] MatchingButtonsController matchingScreen = null;
    [SerializeField] NetworkWeaponSelectController weaponSelectScreen = null;
    static GameObject createScreen = null;
    public static List<string> playerNames = new List<string>();
    static bool isStarted = false;

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
            if (isServer)
            {
                GetComponent<NetworkRoomPlayer>().readyToBegin = false;
            }
            BrightnessManager.SetGameAlfa(0);
            MatchingButtonsController mc = Instantiate(matchingScreen);
            mc.Init(isServer);
            createScreen = mc.gameObject;

            isStarted = true;
        }
        playerNames.Clear();
        playerNames = names.ToList();
        createScreen.GetComponent<MatchingButtonsController>().SetPlayerList(names);
    }

    void Update() { }

    [ClientRpc]
    public void RpcSetWeaponSelectScreen()
    {
        Destroy(createScreen);
        Instantiate(weaponSelectScreen);
    }
}
