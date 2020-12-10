using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KuribocchiButtonsController : MonoBehaviour
{
    [SerializeField] GameObject inputNameObject = null;
    [SerializeField] GameObject screenMask = null;  //名前入力中に後ろのボタンを押せないようにするため
    InputField inputField;

    string name = "";

    void Start()
    {
        inputNameObject.SetActive(false);
        screenMask.SetActive(false);

        inputField = inputNameObject.transform.Find("InputField").GetComponent<InputField>();
        inputField.characterLimit = 10;
    }

    //ソロ
    public void SelectBocchi()
    {
        BaseScreenManager.SetNextScreen(BaseScreenManager.Screen.CPU_SELECT);
    }

    //マルチ
    public void SelectRiajuu()
    {  
        inputNameObject.SetActive(true);  //名前入力の表示
        screenMask.SetActive(true); //後ろのボタンを押せなくする
        BrightnessManager.SetGameAlfa(0.7f);    //後ろを暗くする
    }

    //戻る
    public void SelectBack()
    {
        BaseScreenManager.SetNextScreen(BaseScreenManager.Screen.GAME_MODE_SELECT);
    }


    //マッチング
    public void SelectMatching()
    {
        if(inputField.text != "")
        {
            name = inputField.text;
            inputNameObject.SetActive(false);    //名前入力の非表示
            screenMask.SetActive(false);         //後ろのボタンを押せるようにする
            BrightnessManager.SetGameAlfa(0);    //明るさを元に戻す

            BaseScreenManager.SetNextScreen(BaseScreenManager.Screen.MATCHING);
        }
    }

    //名前入力中の戻る
    public void SelectBack_InputName()
    {
        inputNameObject.SetActive(false);    //名前入力の非表示
        screenMask.SetActive(false);    //後ろのボタンを押せるようにする
        BrightnessManager.SetGameAlfa(0);   //明るさを元に戻す
    }
}
