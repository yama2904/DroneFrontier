using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponSelectButtonsController : MonoBehaviour
{
    const string SHOTGUN_TEXT = "射程が非常に短いが\n威力が高く、リキャストが短い。";
    const string MISSILE_TEXT = "誘導力とスピードが高く、発射後に爆発を起こす。\nリキャストが長い";
    const string LASER_TEXT = "極めて高威力だが、発動時にチャージが必要。\nまた、リキャストが最も長い。";

    //ボタン処理用
    [SerializeField] GameObject onButton = null;
    const string SELECT_BUTTON_COLOR = "#4C76FF";     //ボタンを押したときの色の16進数
    const string NOT_SELECT_BUTTON_COLOR = "#FFFFFF";    //他のボタンが押されている時の色の16進数
    Color selectButtonColor;  //16進数をColorに変換したやつ
    Color notSelectButtonColor;

    //武器の説明
    [SerializeField] GameObject MessageWindowText = null;
    Text messageText;

    //選択した武器
    AtackManager.Weapon weapon;

    void Start()
    {
        //Color型に変換
        ColorUtility.TryParseHtmlString(SELECT_BUTTON_COLOR, out selectButtonColor);
        ColorUtility.TryParseHtmlString(NOT_SELECT_BUTTON_COLOR, out notSelectButtonColor);
        onButton.GetComponent<Button>().image.color = selectButtonColor; //デフォルトでアイテムONボタンが押されているようにする

        messageText = MessageWindowText.GetComponent<Text>();
        messageText.text = "武器を選択してください。";
        
        weapon = AtackManager.Weapon.NONE;
        MainGameManager.IsItem = true;
    }

    public void SelectShotgun()
    {
        messageText.text = SHOTGUN_TEXT;
        weapon = AtackManager.Weapon.SHOTGUN;
    }

    public void SelectMissile()
    {
        messageText.text = MISSILE_TEXT;
        weapon = AtackManager.Weapon.MISSILE;
    }

    public void SelectLaser()
    {
        messageText.text = LASER_TEXT;
        weapon = AtackManager.Weapon.LASER;
    }

    public void SelectDecision()
    {
        if(weapon == AtackManager.Weapon.NONE)
        {
            return;
        }
        if (!MainGameManager.IsMulti)
        {
            MainGameManager.LoadMainGameScene();
        }
    }


    public void SelectItemOn()
    {
        MainGameManager.IsItem = true;

        //ボタンを押したらインスペクターで設定している色と被るので
        //どちらかボタンが押されたらデフォルトの色を解除
        onButton.GetComponent<Button>().image.color = notSelectButtonColor; 
    }

    public void SelectItemOff()
    {
        MainGameManager.IsItem = false;

        //ボタンを押したらインスペクターで設定している色と被るので
        //どちらかボタンが押されたらデフォルトの色を解除
        onButton.GetComponent<Button>().image.color = notSelectButtonColor;
    }


    public void SelectBack()
    {
        BaseScreenManager.SetNextScreen(BaseScreenManager.Screen.CPU_SELECT);
    }
}
