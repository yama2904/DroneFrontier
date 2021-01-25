using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NetworkWeaponSelectController : MonoBehaviour
{
    const string SHOTGUN_TEXT = "射程が非常に短いが\n威力が高く、リキャストが短い。";
    const string MISSILE_TEXT = "誘導力とスピードが高く、発射後に爆発を起こす。\nリキャストが長い";
    const string LASER_TEXT = "極めて高威力だが、発動時にチャージが必要。\nまた、リキャストが最も長い。";

    //武器の説明
    [SerializeField] Text messageWindowText = null;

    //選択した武器
    BaseWeapon.Weapon weapon = BaseWeapon.Weapon.NONE;

    //ボタン処理用
    [SerializeField] Canvas canvas = null;
    [SerializeField] GameObject itemSelect = null;
    const string ITEM_ON_NAME = "OnButton";
    const string ITEM_OFF_NAME = "OffButton";
    Button itemOnButton = null;
    Button itemOffButton = null;
    const string SELECT_BUTTON_COLOR = "#4C76FF";     //ボタンを押したときの色の16進数
    const string NOT_SELECT_BUTTON_COLOR = "#FFFFFF";    //他のボタンが押されている時の色の16進数
    Color selectButtonColor;  //16進数をColorに変換したやつ
    Color notSelectButtonColor;

    bool IsServer = false;


    void Start()
    {
        messageWindowText.text = "武器を選択してください。";
    }

    public void Init(bool isServer)
    {
        IsServer = isServer;
        if (isServer)
        {
            //アイテム選択枠の生成
            RectTransform parent = Instantiate(itemSelect).GetComponent<RectTransform>();
            parent.SetParent(canvas.transform);
            parent.anchoredPosition = new Vector2(-200, -70);


            //アイテムオンボタンの設定
            itemOnButton = parent.Find(ITEM_ON_NAME).GetComponent<Button>();

            //ボタンの色設定
            ColorUtility.TryParseHtmlString(SELECT_BUTTON_COLOR, out selectButtonColor);
            ColorUtility.TryParseHtmlString(NOT_SELECT_BUTTON_COLOR, out notSelectButtonColor);
            itemOnButton.image.color = selectButtonColor; //デフォルトでアイテムONボタンが押されているようにする

            //クリック時の動作設定
            itemOnButton.onClick.AddListener(SelectItemOn);


            //アイテムオフボタンの設定
            itemOffButton = parent.Find(ITEM_OFF_NAME).GetComponent<Button>();
            itemOffButton.onClick.AddListener(SelectItemOff);
        }
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
        if (weapon == BaseWeapon.Weapon.NONE)
        {
            return;
        }
        if (!MainGameManager.IsMulti)
        {
            NonGameManager.SetPlayer("Player", weapon, true);
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
}
