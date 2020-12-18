using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CPUSelectButtonsController : MonoBehaviour
{
    const short MAX_CPU_NUM = 3;
    const short MIN_CPU_NUM = 1;

    [SerializeField] GameObject CPUNumText = null;    
    short cpuNum;

    //CPUリスト用
    enum List
    {
        LIST_1,
        LIST_2,
        LIST_3,

        NONE
    }

    enum Weapon
    {
        SHOTGUN,
        MISSILE,
        LASER,

        NONE
    }
    [SerializeField] GameObject CPUList = null;
    GameObject[] CPULists;  //CPUリスト
    Button[] buttons;       //CPUの武器を選択するボタン
    Weapon[] CPUsWeapon;    //各CPUの武器
    
    //色用変数
    const string SELECT_BUTTON_COLOR = "#A2A2A2";       //ボタンを押したときの色の16進数
    const string NOT_SELECT_BUTTON_COLOR = "#FFFFFF";   //他のボタンが押されている時の色の16進数
    Color selectButtonColor;  //16進数をColorに変換したやつ
    Color notSelectButtonColor;


    void Start()
    {
        cpuNum = MIN_CPU_NUM;
        CPUNumText.GetComponent<Text>().text = cpuNum.ToString();

        //Color型に変換
        ColorUtility.TryParseHtmlString(SELECT_BUTTON_COLOR, out selectButtonColor);
        ColorUtility.TryParseHtmlString(NOT_SELECT_BUTTON_COLOR, out notSelectButtonColor);

        //オブジェクト名
        string[] listName = new string[(int)List.NONE];
        listName[(int)List.LIST_1] = "List1";
        listName[(int)List.LIST_2] = "List2";
        listName[(int)List.LIST_3] = "List3";

        //オブジェクト名
        string[] buttonName = new string[(int)Weapon.NONE];
        buttonName[(int)Weapon.SHOTGUN] = "SelectShotgun";
        buttonName[(int)Weapon.MISSILE] = "SelectMissile";
        buttonName[(int)Weapon.LASER] = "SelectLaser";

        CPULists = new GameObject[(int)List.NONE];
        CPUsWeapon = new Weapon[(int)List.NONE];
        buttons = new Button[(int)List.NONE * (int)Weapon.NONE];    //2次元配列を1次元配列にまとめる

        //CPUListsとbuttonsの要素の初期化
        for (int i = 0; i < (int)List.NONE; i++)
        {
            CPULists[i] = CPUList.transform.Find(listName[i]).gameObject;

            //CPUの武器を選択するボタンを全て取得
            for (int j = 0; j < (int)Weapon.NONE; j++)
            {
                int index = (i * (int)List.NONE) + j;
                buttons[index] = CPULists[i].transform.Find(buttonName[j]).GetComponent<Button>();
            }
            //一旦CPUリストを非表示
            CPULists[i].SetActive(false);

            //デフォルトはショットガン
            CPUsWeapon[i] = Weapon.SHOTGUN;
            SetButtonColor((List)i, Weapon.SHOTGUN);
        }

        //選択しているCPU人数の分だけリストを表示
        for (int i = 0; i < cpuNum; i++)
        {
            CPULists[i].SetActive(true);
        }
    }

    //CPUの数を増やす
    public void SelectNumUp()
    {        
        if(cpuNum < MAX_CPU_NUM)
        {
            cpuNum++;
            CPUNumText.GetComponent<Text>().text = cpuNum.ToString();

            CPULists[cpuNum - 1].SetActive(true);
        }
    }

    //CPUの数を減らす
    public void SelectNumDonw()
    {
        if(cpuNum > MIN_CPU_NUM)
        {
            cpuNum--;
            CPUNumText.GetComponent<Text>().text = cpuNum.ToString();

            CPULists[cpuNum].SetActive(false);
        }
    }


    public void SelectCPU1Shotgun()
    {
        SetButtonColor(List.LIST_1, Weapon.SHOTGUN);
    }
    public void SelectCPU1Missile()
    {
        SetButtonColor(List.LIST_1, Weapon.MISSILE);
    }
    public void SelectCPU1Laser()
    {
        SetButtonColor(List.LIST_1, Weapon.LASER);
    }

    public void SelectCPU2Shotgun()
    {
        SetButtonColor(List.LIST_2, Weapon.SHOTGUN);
    }
    public void SelectCPU2Missile()
    {
        SetButtonColor(List.LIST_2, Weapon.MISSILE);
    }
    public void SelectCPU2Laser()
    {
        SetButtonColor(List.LIST_2, Weapon.LASER);
    }

    public void SelectCPU3Shotgun()
    {
        SetButtonColor(List.LIST_3, Weapon.SHOTGUN);
    }
    public void SelectCPU3Missile()
    {
        SetButtonColor(List.LIST_3, Weapon.MISSILE);
    }
    public void SelectCPU3Laser()
    {
        SetButtonColor(List.LIST_3, Weapon.LASER);
    }

    public void SelectDecision()
    {
        BaseScreenManager.SetScreen(BaseScreenManager.Screen.WEAPON_SELECT);
    }

    public void SelectBack()
    {
        //ソロモードなら戻る
        if (!MainGameManager.IsMulti)
        {
            BaseScreenManager.SetScreen(BaseScreenManager.Screen.KURIBOCCHI);
        }
    }


    void SetButtonColor(List list, Weapon weapon)
    {
        for (int i = 0; i < (int)Weapon.NONE; i++)
        {
            int index = ((int)list * (int)List.NONE) + i;
            if (i == (int)weapon)
            {
                buttons[index].image.color = selectButtonColor;
            }
            else
            {
                buttons[index].image.color = notSelectButtonColor;
            }
        }
    }

    int GetIndex(List l, Weapon w)
    {
        return ((int)l * (int)List.NONE) + (int)w;
    }
}
