using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror.Discovery;

public class KuribocchiButtonsController : MonoBehaviour
{
    [SerializeField] GameObject inputNameObject = null;
    [SerializeField] GameObject screenMask = null;  //名前入力中に後ろのボタンを押せないようにするため
    [SerializeField] InputField inputField = null;

    string playerName = "";

    void Start()
    {
        inputNameObject.SetActive(false);
        screenMask.SetActive(false);
        inputField.characterLimit = 10;
    }

    //ソロ
    public void SelectBocchi()
    {
        BaseScreenManager.SetScreen(BaseScreenManager.Screen.CPU_SELECT);
    }

    //マルチ
    public void SelectRiajuu()
    {
        inputNameObject.SetActive(true);  //名前入力の表示
        screenMask.SetActive(true);       //後ろのボタンを押せなくする
        BrightnessManager.SetGameAlfa(0.7f);    //後ろを暗くする
    }

    //戻る
    public void SelectBack()
    {
        BaseScreenManager.SetScreen(BaseScreenManager.Screen.GAME_MODE_SELECT);
    }


    //マッチング
    public void SelectHost()
    {
        if (inputField.text != "")
        {
            playerName = inputField.text;
            inputNameObject.SetActive(false);    //名前入力の非表示
            screenMask.SetActive(false);         //後ろのボタンを押せるようにする
            BrightnessManager.SetGameAlfa(0);    //明るさを元に戻す
            MainGameManager.IsMulti = true;      //マルチモードに設定

            CustomNetworkDiscoveryHUD.Instance.StartHost();
            //BaseScreenManager.SetScreen(BaseScreenManager.Screen.MATCHING);
        }
    }

    public void SelectClient()
    {
        if (inputField.text == "") return;
        if (!CustomNetworkDiscoveryHUD.Instance.StartClient()) return;

        playerName = inputField.text;
        inputNameObject.SetActive(false);    //名前入力の非表示
        screenMask.SetActive(false);         //後ろのボタンを押せるようにする
        BrightnessManager.SetGameAlfa(0);    //明るさを元に戻す
        MainGameManager.IsMulti = true;      //マルチモードに設定

        //BaseScreenManager.SetScreen(BaseScreenManager.Screen.MATCHING);
    }


    //名前入力中の戻る
    public void SelectBack_InputName()
    {
        inputNameObject.SetActive(false);    //名前入力の非表示
        screenMask.SetActive(false);    //後ろのボタンを押せるようにする
        BrightnessManager.SetGameAlfa(0);   //明るさを元に戻す
    }
}
