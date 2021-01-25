using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class MatchingButtonsController : NetworkBehaviour
{
    [SerializeField] Text Text1PName = null;
    [SerializeField] Text Text2PName = null;
    [SerializeField] Text Text3PName = null;
    [SerializeField] Text Text4PName = null;

    Color playerTextColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    Color nonPlayerTextColor = new Color(0.32f, 0.41f, 0.72f, 1f);
    string nonPlayerText = "参加者受付中...";
    [SerializeField] Button decisinButton = null;

    void Start()
    {
        if (isServer)
        {
            Instantiate(decisinButton);
        }
    }
    void Update() { }

    public void SelectDecision()
    {

    }

    public void SelectBack()
    {
        if (isServer)
        {
            NetworkManager.singleton.StopHost();
        }
        NonGameManager.LoadNonGameScene(BaseScreenManager.Screen.KURIBOCCHI);
    }
}
