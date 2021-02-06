using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Offline
{
    public class CPUSelectScreenManager : MonoBehaviour
    {
        //選択したCPU情報
        public class CPUData
        {
            public string name;
            public BaseWeapon.Weapon weapon;
        }
        static CPUData[] cpuDatas = new CPUData[(int)List.NONE];

        //CPU情報
        public static CPUData[] CPUDatas
        {
            get
            {
                CPUData[] cpus = new CPUData[CPUNum];
                for (int i = 0; i < CPUNum; i++)
                {
                    cpus[i] = cpuDatas[i];
                }
                return cpus;
            }
        }

        //CPU数
        public static short CPUNum { get; private set; }


        //CPUリスト用
        enum List
        {
            LIST_1,
            LIST_2,
            LIST_3,

            NONE
        }
        //選択できる武器
        enum Weapon
        {
            SHOTGUN,
            MISSILE,
            LASER,

            NONE
        }

        //CPU数
        [SerializeField] Text CPUNumText = null;
        const short MAX_CPU_NUM = 3;
        const short MIN_CPU_NUM = 1;

        //CPU名
        const string CPU_NAME = "CPU";

        //画面情報
        [SerializeField] GameObject screenCPUList = null;
        GameObject[] screenCPULists = new GameObject[(int)List.NONE];  //CPUリスト
        Button[] screenButtons = new Button[(int)List.NONE * (int)Weapon.NONE];  //CPUの武器を選択するボタン(2次元配列を1次元にまとめる)

        //色用変数
        Color selectButtonColor = new Color(0.635f, 0.635f, 0.635f, 1f);  //16進数をColorに変換したやつ
        Color notSelectButtonColor = new Color(1f, 1f, 1f, 1f);


        void Start()
        {
            //CPU数の初期化
            CPUNum = MIN_CPU_NUM;
            CPUNumText.text = CPUNum.ToString();

            //オブジェクト名
            string[] listName = new string[(int)List.NONE];
            listName[(int)List.LIST_1] = "List1";
            listName[(int)List.LIST_2] = "List2";
            listName[(int)List.LIST_3] = "List3";

            //オブジェクト名
            string[] buttonName = new string[(int)Weapon.NONE];
            buttonName[(int)Weapon.SHOTGUN] = "SelectShotgunButton";
            buttonName[(int)Weapon.MISSILE] = "SelectMissileButton";
            buttonName[(int)Weapon.LASER] = "SelectLaserButton";


            //CPUListsとbuttonsの要素の初期化
            Weapon defaultWeapon = Weapon.SHOTGUN;
            for (int i = 0; i < (int)List.NONE; i++)
            {
                screenCPULists[i] = screenCPUList.transform.Find(listName[i]).gameObject;

                //CPUの武器を選択するボタンを全て取得
                for (int j = 0; j < (int)Weapon.NONE; j++)
                {
                    int index = (i * (int)List.NONE) + j;
                    screenButtons[index] = screenCPULists[i].transform.Find(buttonName[j]).GetComponent<Button>();
                }
                //一旦CPUリストを非表示
                screenCPULists[i].SetActive(false);

                //CPUデータの更新
                cpuDatas[i] = new CPUData
                {
                    name = CPU_NAME + i,
                    weapon = ConverWeaponToBaseWepon(defaultWeapon)  //デフォルトはショットガン
                };

                //ボタンの色変更
                SetButtonColor((List)i, defaultWeapon);
            }

            //選択しているCPU人数の分だけリストを表示
            for (int i = 0; i < CPUNum; i++)
            {
                screenCPULists[i].SetActive(true);
            }
        }

        //CPUの数を増やす
        public void SelectNumUp()
        {
            if (CPUNum < MAX_CPU_NUM)
            {
                //CPU数のテキストを変更
                CPUNum++;
                CPUNumText.text = CPUNum.ToString();

                //表示するCPUの武器選択リストの数を変更
                screenCPULists[CPUNum - 1].SetActive(true);
            }
        }

        //CPUの数を減らす
        public void SelectNumDown()
        {
            if (CPUNum > MIN_CPU_NUM)
            {
                CPUNum--;
                CPUNumText.text = CPUNum.ToString();

                screenCPULists[CPUNum].SetActive(false);
            }
        }


        public void SelectCPU1Shotgun()
        {
            CPUData cd = cpuDatas[(int)List.LIST_1];  //名前省略
            if (cd.weapon == BaseWeapon.Weapon.SHOTGUN) return;

            //SE再生
            SoundManager.Play(SoundManager.SE.SELECT, SoundManager.BaseSEVolume);

            cd.weapon = BaseWeapon.Weapon.SHOTGUN;  //武器情報の更新
            SetButtonColor(List.LIST_1, Weapon.SHOTGUN);  //ボタンの色変え
        }
        public void SelectCPU1Missile()
        {
            CPUData cd = cpuDatas[(int)List.LIST_1];  //名前省略
            if (cd.weapon == BaseWeapon.Weapon.MISSILE) return;

            //SE再生
            SoundManager.Play(SoundManager.SE.SELECT, SoundManager.BaseSEVolume);

            cd.weapon = BaseWeapon.Weapon.MISSILE;  //武器情報の更新
            SetButtonColor(List.LIST_1, Weapon.MISSILE);  //ボタンの色変え
        }
        public void SelectCPU1Laser()
        {
            CPUData cd = cpuDatas[(int)List.LIST_1];  //名前省略
            if (cd.weapon == BaseWeapon.Weapon.LASER) return;

            //SE再生
            SoundManager.Play(SoundManager.SE.SELECT, SoundManager.BaseSEVolume);

            cd.weapon = BaseWeapon.Weapon.LASER;  //武器情報の更新
            SetButtonColor(List.LIST_1, Weapon.LASER);  //ボタンの色変え
        }

        public void SelectCPU2Shotgun()
        {
            CPUData cd = cpuDatas[(int)List.LIST_2];  //名前省略
            if (cd.weapon == BaseWeapon.Weapon.SHOTGUN) return;

            //SE再生
            SoundManager.Play(SoundManager.SE.SELECT, SoundManager.BaseSEVolume);

            cd.weapon = BaseWeapon.Weapon.SHOTGUN;  //武器情報の更新
            SetButtonColor(List.LIST_2, Weapon.SHOTGUN);  //ボタンの色変え
        }
        public void SelectCPU2Missile()
        {
            CPUData cd = cpuDatas[(int)List.LIST_2];  //名前省略
            if (cd.weapon == BaseWeapon.Weapon.MISSILE) return;

            //SE再生
            SoundManager.Play(SoundManager.SE.SELECT, SoundManager.BaseSEVolume);

            cd.weapon = BaseWeapon.Weapon.MISSILE;  //武器情報の更新
            SetButtonColor(List.LIST_2, Weapon.MISSILE);  //ボタンの色変え
        }
        public void SelectCPU2Laser()
        {
            CPUData cd = cpuDatas[(int)List.LIST_2];  //名前省略
            if (cd.weapon == BaseWeapon.Weapon.LASER) return;

            //SE再生
            SoundManager.Play(SoundManager.SE.SELECT, SoundManager.BaseSEVolume);

            cd.weapon = BaseWeapon.Weapon.LASER;  //武器情報の更新
            SetButtonColor(List.LIST_2, Weapon.LASER);  //ボタンの色変え
        }

        public void SelectCPU3Shotgun()
        {
            CPUData cd = cpuDatas[(int)List.LIST_3];  //名前省略
            if (cd.weapon == BaseWeapon.Weapon.SHOTGUN) return;

            //SE再生
            SoundManager.Play(SoundManager.SE.SELECT, SoundManager.BaseSEVolume);

            cd.weapon = BaseWeapon.Weapon.SHOTGUN;  //武器情報の更新
            SetButtonColor(List.LIST_3, Weapon.SHOTGUN);  //ボタンの色変え
        }
        public void SelectCPU3Missile()
        {
            CPUData cd = cpuDatas[(int)List.LIST_3];  //名前省略
            if (cd.weapon == BaseWeapon.Weapon.MISSILE) return;

            //SE再生
            SoundManager.Play(SoundManager.SE.SELECT, SoundManager.BaseSEVolume);

            cd.weapon = BaseWeapon.Weapon.MISSILE;  //武器情報の更新
            SetButtonColor(List.LIST_3, Weapon.MISSILE);  //ボタンの色変え
        }
        public void SelectCPU3Laser()
        {
            CPUData cd = cpuDatas[(int)List.LIST_3];  //名前省略
            if (cd.weapon == BaseWeapon.Weapon.LASER) return;

            //SE再生
            SoundManager.Play(SoundManager.SE.SELECT, SoundManager.BaseSEVolume);

            cd.weapon = BaseWeapon.Weapon.LASER;  //武器情報の更新
            SetButtonColor(List.LIST_3, Weapon.LASER);  //ボタンの色変え
        }

        //決定
        public void SelectDecision()
        {
            //SE再生
            SoundManager.Play(SoundManager.SE.SELECT, SoundManager.BaseSEVolume);

            //武器選択画面に移動
            BaseScreenManager.SetScreen(BaseScreenManager.Screen.WEAPON_SELECT);
        }

        public void SelectBack()
        {
            //SE再生
            SoundManager.Play(SoundManager.SE.CANCEL, SoundManager.BaseSEVolume);

            //ソロマルチ選択画面に戻る
            BaseScreenManager.SetScreen(BaseScreenManager.Screen.KURIBOCCHI);
        }


        void SetButtonColor(List list, Weapon weapon)
        {
            for (int i = 0; i < (int)Weapon.NONE; i++)
            {
                int index = ((int)list * (int)List.NONE) + i;
                if (i == (int)weapon)
                {
                    screenButtons[index].image.color = selectButtonColor;
                }
                else
                {
                    screenButtons[index].image.color = notSelectButtonColor;
                }
            }
        }

        BaseWeapon.Weapon ConverWeaponToBaseWepon(Weapon weapon)
        {
            int w = (int)weapon;
            if (w == (int)Weapon.SHOTGUN)
            {
                return BaseWeapon.Weapon.SHOTGUN;
            }
            else if (w == (int)Weapon.MISSILE)
            {
                return BaseWeapon.Weapon.MISSILE;
            }
            else if (w == (int)Weapon.LASER)
            {
                return BaseWeapon.Weapon.LASER;
            }
            return BaseWeapon.Weapon.NONE;
        }
    }
}