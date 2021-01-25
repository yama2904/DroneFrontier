using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Mirror;

public class MatchingButtonsController : NetworkBehaviour
{
    [SerializeField] Canvas canvas = null;
    [SerializeField] Text Text1PName = null;
    [SerializeField] Text Text2PName = null;
    [SerializeField] Text Text3PName = null;
    [SerializeField] Text Text4PName = null;
    [SerializeField] Button decisinButton = null;

    Color playerTextColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    Color nonPlayerTextColor = new Color(0.32f, 0.41f, 0.72f, 1f);
    string nonPlayerText = "参加者受付中...";
    bool IsServer = false;

    void Start() { }
    void Update() { }

    public void Init(bool isServer)
    {
        IsServer = isServer;
        if (isServer)
        {
            Button b = Instantiate(decisinButton);
            b.transform.SetParent(canvas.transform);
            b.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -400);
            b.onClick.AddListener(SelectDecision);
        }
    }

    public void SelectDecision()
    {
        MatchingManager.Singleton.RpcSetWeaponSelectScreen();
    }

    public void SelectBack()
    {
        if (IsServer)
        {
            NetworkManager.singleton.StopHost();
        }
        NonGameManager.LoadNonGameScene(BaseScreenManager.Screen.KURIBOCCHI);
    }

    public void SetPlayerList(string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            switch (i)
            {
                case 0:
                    Text1PName.text = names[i];
                    Text1PName.color = playerTextColor;
                    break;

                case 1:
                    Text2PName.text = names[i];
                    Text2PName.color = playerTextColor;
                    break;

                case 2:
                    Text3PName.text = names[i];
                    Text3PName.color = playerTextColor;
                    break;

                case 3:
                    Text4PName.text = names[i];
                    Text4PName.color = playerTextColor;
                    break;
            }
        }
    }
}
