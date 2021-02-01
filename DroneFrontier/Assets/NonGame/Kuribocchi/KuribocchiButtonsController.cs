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

    public static string playerName = "";

    void Start()
    {
        inputNameObject.SetActive(false);
        screenMask.SetActive(false);
        inputField.characterLimit = 10;
    }

    //ソロ
    public void SelectBocchi()
    {
        //SE再生
        SoundManager.Play(SoundManager.SE.SELECT, SoundManager.BaseSEVolume);

        BaseScreenManager.SetScreen(BaseScreenManager.Screen.CPU_SELECT);
    }

    //マルチ
    public void SelectRiajuu()
    {
        //SE再生
        SoundManager.Play(SoundManager.SE.SELECT, SoundManager.BaseSEVolume);

        inputNameObject.SetActive(true);  //名前入力の表示
        screenMask.SetActive(true);       //後ろのボタンを押せなくする
        BrightnessManager.SetGameAlfa(0.7f);  //後ろを暗くする
    }

    //戻る
    public void SelectBack()
    {
        //SE再生
        SoundManager.Play(SoundManager.SE.CANCEL, SoundManager.BaseSEVolume);

        BaseScreenManager.SetScreen(BaseScreenManager.Screen.GAME_MODE_SELECT);
    }


    //募集ボタン
    public void SelectHost()
    {
        if (inputField.text != "")
        {
            //SE再生
            SoundManager.Play(SoundManager.SE.SELECT, SoundManager.BaseSEVolume);

            MainGameManager.IsMulti = true;  //マルチモードに設定
            playerName = inputField.text;

            CustomNetworkDiscoveryHUD.Singleton.StartHost();
        }
    }

    //参加ボタン
    public void SelectClient()
    {
        //名前を入力していなかったら処理しない
        if (inputField.text == "") return;

        //SE再生
        SoundManager.Play(SoundManager.SE.SELECT, SoundManager.BaseSEVolume);

        CustomNetworkDiscoveryHUD.Singleton.StartClient();  //ホストを探す
        playerName = inputField.text;
    }


    //名前入力中の戻る
    public void SelectBack_InputName()
    {
        //SE再生
        SoundManager.Play(SoundManager.SE.CANCEL, SoundManager.BaseSEVolume);

        inputNameObject.SetActive(false);    //名前入力の非表示
        screenMask.SetActive(false);    //後ろのボタンを押せるようにする
        BrightnessManager.SetGameAlfa(0);   //明るさを元に戻す

        //検索を止める
        NewNetworkDiscovery.Singleton.StopDiscovery();
        CustomNetworkDiscoveryHUD.Singleton.Init();
    }
}
