using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AtackManager : MonoBehaviour
{
    const string FOLDER_PATH = "Atack/";

    public enum Weapon
    {
        SHOTGUN,
        GATLING,
        MISSILE,
        LASER,

        NONE
    }

    public static void CreateAtack(out GameObject create, Weapon weapon)
    {
        GameObject o = null;
        if(weapon == Weapon.SHOTGUN)
        {

        }
        else if (weapon == Weapon.GATLING)
        {
            //ResourcesフォルダからGatlingオブジェクトを複製してロード
            o = GameObject.Instantiate(Resources.Load(FOLDER_PATH + "Gatling")) as GameObject;
        }
        else if (weapon == Weapon.MISSILE)
        {
            //ResourcesフォルダからMissileShotオブジェクトを複製してロード
            o = GameObject.Instantiate(Resources.Load(FOLDER_PATH + "MissileShot")) as GameObject;
        }
        else if (weapon == Weapon.LASER)
        {
            //ResourcesフォルダからLaserオブジェクトを複製してロード
            o = GameObject.Instantiate(Resources.Load(FOLDER_PATH + "Laser")) as GameObject;
        }
        else
        {
            //エラー
            Application.Quit();
        }

        create = o;
    }
}
