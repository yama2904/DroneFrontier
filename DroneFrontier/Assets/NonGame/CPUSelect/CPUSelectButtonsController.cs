using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CPUSelectButtonsController : MonoBehaviour
{
    const short MAX_CPU_NUM = 3;
    const short MIN_CPU_NUM = 1;

    [SerializeField] Text CPUNumText = null;    
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
    GameObject[] CPULists = new GameObject[(int)List.NONE];  //CPUリスト
    Weapon[] CPUsWeapon = new Weapon[(int)List.NONE];    //各CPUの武器
    Button[] buttons = new Button[(int)List.NONE * (int)Weapon.NONE];  //CPUの武器を選択するボタン(2次元配列を1次元にまとめる)
    
    //色用変数
    const string SELECT_BUTTON_COLOR = "#A2A2A2";       //ボタンを押したときの色の16進数
    const string NOT_SELECT_BUTTON_COLOR = "#FFFFFF";   //他のボタンが押されている時の色の16進数
    Color selectButtonColor;  //16進数をColorに変換したやつ
    Color notSelectButtonColor;


    void Start()
    {
        cpuNum = MIN_CPU_NUM;
        CPUNumText.text = cpuNum.ToString();

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
            CPUNumText.text = cpuNum.ToString();

            CPULists[cpuNum - 1].SetActive(true);
        }
    }

    //CPUの数を減らす
    public void SelectNumDonw()
    {
        if(cpuNum > MIN_CPU_NUM)
        {
            cpuNum--;
            CPUNumText.text = cpuNum.ToString();

            CPULists[cpuNum].SetActive(false);
        }
    }


    public void SelectCPU1Shotgun()
    {
        CPUsWeapon[(int)List.LIST_1] = Weapon.SHOTGUN;
        SetButtonColor(List.LIST_1, Weapon.SHOTGUN);
    }
    public void SelectCPU1Missile()
    {
        CPUsWeapon[(int)List.LIST_1] = Weapon.MISSILE;
        SetButtonColor(List.LIST_1, Weapon.MISSILE);
    }
    public void SelectCPU1Laser()
    {
        CPUsWeapon[(int)List.LIST_1] = Weapon.LASER;
        SetButtonColor(List.LIST_1, Weapon.LASER);
    }

    public void SelectCPU2Shotgun()
    {
        CPUsWeapon[(int)List.LIST_2] = Weapon.SHOTGUN;
        SetButtonColor(List.LIST_2, Weapon.SHOTGUN);
    }
    public void SelectCPU2Missile()
    {
        CPUsWeapon[(int)List.LIST_2] = Weapon.MISSILE;
        SetButtonColor(List.LIST_2, Weapon.MISSILE);
    }
    public void SelectCPU2Laser()
    {
        CPUsWeapon[(int)List.LIST_2] = Weapon.LASER;
        SetButtonColor(List.LIST_2, Weapon.LASER);
    }

    public void SelectCPU3Shotgun()
    {
        CPUsWeapon[(int)List.LIST_3] = Weapon.SHOTGUN;
        SetButtonColor(List.LIST_3, Weapon.SHOTGUN);
    }
    public void SelectCPU3Missile()
    {
        CPUsWeapon[(int)List.LIST_3] = Weapon.MISSILE;
        SetButtonColor(List.LIST_3, Weapon.MISSILE);
    }
    public void SelectCPU3Laser()
    {
        CPUsWeapon[(int)List.LIST_3] = Weapon.LASER;
        SetButtonColor(List.LIST_3, Weapon.LASER);
    }

    //決定
    public void SelectDecision()
    {
        for(int i = 0; i < cpuNum; i++)
        {
            string cpuName = "CPU" + (i + 1);
            BaseWeapon.Weapon cpuWeapon = ConverWeaponToBaseWepon(CPUsWeapon[i]);
            NonGameManager.SetPlayer(cpuName, cpuWeapon, false);
        }
        BaseScreenManager.SetScreen(BaseScreenManager.Screen.WEAPON_SELECT);
    }

    public void SelectBack()
    {
        //ソロモードなら戻る
        if (!MainGameManager.IsMulti)
        {
            NonGameManager.ClearSetedPlayers();
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

    BaseWeapon.Weapon ConverWeaponToBaseWepon(Weapon weapon)
    {
        int w = (int)weapon;
        if(w == (int)BaseWeapon.Weapon.SHOTGUN)
        {
            return BaseWeapon.Weapon.SHOTGUN;
        }
        else if (w == (int)BaseWeapon.Weapon.MISSILE)
        {
            return BaseWeapon.Weapon.MISSILE;
        }
        else if (w == (int)BaseWeapon.Weapon.LASER)
        {
            return BaseWeapon.Weapon.LASER;
        }
        return BaseWeapon.Weapon.NONE;
    }
}
