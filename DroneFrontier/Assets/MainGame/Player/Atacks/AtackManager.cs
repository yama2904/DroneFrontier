//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class AtackManager
//{
//    const string FOLDER_PATH = "Weapon/";

//    public enum Weapon
//    {
//        SHOTGUN,
//        GATLING,
//        MISSILE,
//        LASER,

//        NONE
//    }

//    /*
//     * 武器を生成する
//     * 引数1: 生成した武器を格納
//     * 引数2: 生成する武器の種類
//     */
//    public static void CreateWeapon(out GameObject create, Weapon weapon)
//    {
//        GameObject o = null;
//        if(weapon == Weapon.SHOTGUN)
//        {
//            //ResourcesフォルダからShotgunオブジェクトを複製してロード
//            o = GameObject.Instantiate(Resources.Load(FOLDER_PATH + "Shotgun")) as GameObject;
//        }
//        else if (weapon == Weapon.GATLING)
//        {
//            //ResourcesフォルダからGatlingオブジェクトを複製してロード
//            o = GameObject.Instantiate(Resources.Load(FOLDER_PATH + "Gatling")) as GameObject;
//        }
//        else if (weapon == Weapon.MISSILE)
//        {
//            //ResourcesフォルダからMissileShotオブジェクトを複製してロード
//            o = GameObject.Instantiate(Resources.Load(FOLDER_PATH + "MissileShot")) as GameObject;
//        }
//        else if (weapon == Weapon.LASER)
//        {
//            //ResourcesフォルダからLaserオブジェクトを複製してロード
//            o = GameObject.Instantiate(Resources.Load(FOLDER_PATH + "Laser")) as GameObject;
//        }
//        else
//        {
//            //エラー
//            Application.Quit();
//        }

//        create = o;
//    }
//}
