using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Mirror;

public class MatchingManager : NetworkBehaviour
{
    [SerializeField] GameObject createScreen = null;
    public static List<string> playerNames = new List<string>();

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
        if (isServer) return;
        playerNames.Clear();
        playerNames = names.ToList();
    }

    void Start()
    {
        BrightnessManager.SetGameAlfa(0);
        Instantiate(createScreen);
    }
    
    void Update()
    {
        if (!isLocalPlayer) return;
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("Count: " + playerNames.Count);
            foreach(string s in playerNames)
            {
                Debug.Log(s);
            }
        }
    }
}
