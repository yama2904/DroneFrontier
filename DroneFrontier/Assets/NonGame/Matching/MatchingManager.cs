using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Mirror;

public class MatchingManager : NetworkBehaviour
{
    [SerializeField] MatchingButtonsController createScreen = null;
    static MatchingButtonsController createdScreen = null;
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
        //if (isServer) return;
        if (!isStarted)
        {
            BrightnessManager.SetGameAlfa(0);
            createdScreen = Instantiate(createScreen);
            createdScreen.Init(isServer);

            isStarted = true;
        }
        playerNames.Clear();
        playerNames = names.ToList();
        createdScreen.SetPlayerList(names);
    }

    void Update() { }
}
