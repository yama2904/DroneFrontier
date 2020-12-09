using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponSelectButtonsController : MonoBehaviour
{
    const string SHOTGUN_TEXT = "射程が非常に短いが\n威力が高く、リキャストが短い。";
    const string MISSILE_TEXT = "誘導力とスピードが高く、発射後に爆発を起こす。\nリキャストが長い";
    const string LASER_TEXT = "極めて高威力だが、発動時にチャージが必要。\nまた、リキャストが最も長い。";

    [SerializeField] GameObject MessageWindowText = null;
    Text messageText;

    AtackManager.Weapon weapon;

    void Start()
    {
        messageText = MessageWindowText.GetComponent<Text>();
        messageText.text = "武器を選択してください。";
        weapon = AtackManager.Weapon.NONE;
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

    public void SelectBack()
    {
        BaseScreenManager.SetNextScreen(BaseScreenManager.Screen.CPU_SELECT);
    }
}
