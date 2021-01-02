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
    [SerializeField] Button itemOnButton = null;
    const string SELECT_BUTTON_COLOR = "#4C76FF";     //ボタンを押したときの色の16進数
    const string NOT_SELECT_BUTTON_COLOR = "#FFFFFF";    //他のボタンが押されている時の色の16進数
    Color selectButtonColor;  //16進数をColorに変換したやつ
    Color notSelectButtonColor;

    //武器の説明
    [SerializeField] Text messageWindowText = null;

    //選択した武器
    BaseWeapon.Weapon weapon = BaseWeapon.Weapon.NONE;

    void Start()
    {
        //Color型に変換
        ColorUtility.TryParseHtmlString(SELECT_BUTTON_COLOR, out selectButtonColor);
        ColorUtility.TryParseHtmlString(NOT_SELECT_BUTTON_COLOR, out notSelectButtonColor);
        itemOnButton.image.color = selectButtonColor; //デフォルトでアイテムONボタンが押されているようにする

        messageWindowText.text = "武器を選択してください。";
    }

    public void SelectShotgun()
    {
        messageWindowText.text = SHOTGUN_TEXT;
        weapon = BaseWeapon.Weapon.SHOTGUN;
    }

    public void SelectMissile()
    {
        messageWindowText.text = MISSILE_TEXT;
        weapon = BaseWeapon.Weapon.MISSILE;
    }

    public void SelectLaser()
    {
        messageWindowText.text = LASER_TEXT;
        weapon = BaseWeapon.Weapon.LASER;
    }

    //決定
    public void SelectDecision()
    {
        if(weapon == BaseWeapon.Weapon.NONE)
        {
            return;
        }
        if (!MainGameManager.IsMulti)
        {
            MainGameManager.PlayerData pd = new MainGameManager.PlayerData();
            pd.name = "Player";
            pd.weapon = weapon;
            pd.isPlayer = true;
            MainGameManager.playerDatas.Add(pd);

            NonGameManager.LoadMainGameScene();
        }
    }


    public void SelectItemOn()
    {
        MainGameManager.IsItem = true;

        //ボタンを押したらインスペクターで設定している色と被るので
        //どちらかボタンが押されたらデフォルトの色を解除
        itemOnButton.image.color = notSelectButtonColor; 
    }

    public void SelectItemOff()
    {
        MainGameManager.IsItem = false;

        //ボタンを押したらインスペクターで設定している色と被るので
        //どちらかボタンが押されたらデフォルトの色を解除
        itemOnButton.image.color = notSelectButtonColor;
    }


    public void SelectBack()
    {
        BaseScreenManager.SetScreen(BaseScreenManager.Screen.CPU_SELECT);
    }
}
