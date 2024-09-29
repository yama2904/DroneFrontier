using Mirror.Discovery;
using Offline;
using UnityEngine;
using UnityEngine.UI;

public class SoloMultiSelectManager : MonoBehaviour
{
    /// <summary>
    /// ボタン種類
    /// </summary>
    public enum ButtonType
    {
        /// <summary>
        /// ソロモード
        /// </summary>
        SoloMode,

        /// <summary>
        /// 募集
        /// </summary>
        Host,

        /// <summary>
        /// 参加
        /// </summary>
        Client,

        /// <summary>
        /// 戻る
        /// </summary>
        Back
    }

    /// <summary>
    /// ボタンクリックイベントハンドラ
    /// </summary>
    /// <param name="type">クリックされたボタン</param>
    public delegate void ButtonClickHandler(ButtonType type);

    /// <summary>
    /// ボタンクリックイベント
    /// </summary>
    public event ButtonClickHandler ButtonClick;

    [SerializeField] GameObject inputNameObject = null;
    [SerializeField] GameObject screenMask = null;  //名前入力中に後ろのボタンを押せないようにするため
    [SerializeField] InputField inputField = null;
    [SerializeField] GameObject soloButton = null;

    public static string playerName = "";

    void Start()
    {
        inputNameObject.SetActive(false);
        screenMask.SetActive(false);
        inputField.characterLimit = 10;
    }

    void Update()
    {
        if(GameModeSelectManager.Mode == GameModeSelectManager.GameMode.RACE)
        {
            soloButton.SetActive(false);
        }
        else if (GameModeSelectManager.Mode == GameModeSelectManager.GameMode.BATTLE)
        {
            soloButton.SetActive(true);
        }
    }

    //ソロ
    public void ClickSolo()
    {
        SoundManager.Play(SoundManager.SE.SELECT, SoundManager.SEVolume);
        ButtonClick(ButtonType.SoloMode);
    }

    //マルチ
    public void ClickMulti()
    {
        //SE再生
        SoundManager.Play(SoundManager.SE.SELECT, SoundManager.SEVolume);

        inputNameObject.SetActive(true);  //名前入力の表示
        screenMask.SetActive(true);       //後ろのボタンを押せなくする
        BrightnessManager.SetGameAlfa(0.7f);  //後ろを暗くする
    }

    //戻る
    public void ClickBack()
    {
        SoundManager.Play(SoundManager.SE.CANCEL);
        ButtonClick(ButtonType.Back);
    }


    //募集ボタン
    public void ClickHost()
    {
        if (inputField.text != "")
        {
            //SE再生
            SoundManager.Play(SoundManager.SE.SELECT, SoundManager.SEVolume);

            playerName = inputField.text;

            CustomNetworkDiscoveryHUD.Singleton.StartHost();
        }
    }

    //参加ボタン
    public void ClickClient()
    {
        //名前を入力していなかったら処理しない
        if (inputField.text == "") return;

        //SE再生
        SoundManager.Play(SoundManager.SE.SELECT, SoundManager.SEVolume);

        CustomNetworkDiscoveryHUD.Singleton.StartClient();  //ホストを探す
        playerName = inputField.text;
    }


    //名前入力中の戻る
    public void ClickBack_InputName()
    {
        //SE再生
        SoundManager.Play(SoundManager.SE.CANCEL, SoundManager.SEVolume);

        inputNameObject.SetActive(false);    //名前入力の非表示
        screenMask.SetActive(false);    //後ろのボタンを押せるようにする
        BrightnessManager.SetGameAlfa(0);   //明るさを元に戻す

        //検索を止める
        NewNetworkDiscovery.Singleton.StopDiscovery();
        CustomNetworkDiscoveryHUD.Singleton.Init();
    }
}
