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
    [SerializeField] GameObject decisinButton = null;

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
            decisinButton.SetActive(true);
        }
    }

    public void SelectDecision()
    {
        //SE再生
        SoundManager.Play(SoundManager.SE.SELECT, SoundManager.BaseSEVolume);

        if (MainGameManager.Mode == MainGameManager.GameMode.BATTLE)
        {
            MatchingManager.Singleton.CreateWeaponSelectScreen();
        }
        else
        {
            MatchingManager.Singleton.RpcStartRace();
        }
    }

    public void SelectBack()
    {
        //SE再生
        SoundManager.Play(SoundManager.SE.CANCEL, SoundManager.BaseSEVolume);

        if (IsServer)
        {
            NetworkManager.singleton.StopHost();    //ホストを停止
            NewNetworkDiscovery.Singleton.StopDiscovery();  //ブロードキャストを止める
            MatchingManager.Singleton.DestroyMe();
            NonGameManager.LoadNonGameScene(BaseScreenManager.Screen.KURIBOCCHI);
        }
        else
        {
            MatchingManager.Singleton.ExitClient();
        }
    }

    public void SetPlayerList(string[] names)
    {
        int index = 0;
        for (; index < names.Length; index++)
        {
            switch (index)
            {
                case 0:
                    Text1PName.text = names[index];
                    Text1PName.color = playerTextColor;
                    break;

                case 1:
                    Text2PName.text = names[index];
                    Text2PName.color = playerTextColor;
                    break;

                case 2:
                    Text3PName.text = names[index];
                    Text3PName.color = playerTextColor;
                    break;

                case 3:
                    Text4PName.text = names[index];
                    Text4PName.color = playerTextColor;
                    break;
            }
        }

        //プレイヤーの名前がない欄は募集中にテキストを変える
        for(; index < 4; index++)
        {
            switch (index)
            {
                case 0:
                    Text1PName.text = nonPlayerText;
                    Text1PName.color = nonPlayerTextColor;
                    break;

                case 1:
                    Text2PName.text = nonPlayerText;
                    Text2PName.color = nonPlayerTextColor;
                    break;

                case 2:
                    Text3PName.text = nonPlayerText;
                    Text3PName.color = nonPlayerTextColor;
                    break;

                case 3:
                    Text4PName.text = nonPlayerText;
                    Text4PName.color = nonPlayerTextColor;
                    break;
            }
        }
    }
}
