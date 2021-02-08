using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Offline
{
    public class WeaponSelectScreenManager : MonoBehaviour
    {
        //選択した武器
        public static BaseWeapon.Weapon weapon { get; private set; } = BaseWeapon.Weapon.NONE;

        //説明文に表示するテキスト
        const string SHOTGUN_TEXT = "射程が非常に短いが\n威力が高く、リキャストが短い。";
        const string MISSILE_TEXT = "誘導力とスピードが高く、発射後に爆発を起こす。\nリキャストが長い";
        const string LASER_TEXT = "極めて高威力だが、発動時にチャージが必要。\nまた、リキャストが最も長い。";

        //武器選択ボタン用
        [SerializeField] Button shotgunSelectButton = null;
        [SerializeField] Button missileSelectButton = null;
        [SerializeField] Button laserSelectButton = null;
        [SerializeField] Text messageWindowText = null;  //武器の説明
        Color selectWeaponButtonColor = new Color(0.784f, 0.784f, 0.784f, 1f);  //武器を選択するボタンを押したときの色

        //アイテムボタン処理用
        [SerializeField] Button itemOnButton = null;
        [SerializeField] Button itemOffButton = null;
        Color selectItemButtonColor = new Color(0.3f, 0.46f, 1f, 1f);  //アイテムボタンを選択したときの色
        Color notSelectButtonColor = new Color(1f, 1f, 1f, 1f);  //ボタンを押してないときの色
        bool isItemOnButton = true; //アイテムオンか


        void Start()
        {
            weapon = BaseWeapon.Weapon.NONE;
            messageWindowText.text = "武器を選択してください。";
        }

        public void SelectShotgun()
        {
            BaseWeapon.Weapon w = BaseWeapon.Weapon.SHOTGUN;  //名前省略
            if (weapon == w) return;

            //SE再生
            SoundManager.Play(SoundManager.SE.SELECT, SoundManager.BaseSEVolume);

            messageWindowText.text = SHOTGUN_TEXT;
            SetWeaponButtonsColor(w);
            weapon = w;
        }

        public void SelectMissile()
        {
            BaseWeapon.Weapon w = BaseWeapon.Weapon.MISSILE;  //名前省略
            if (weapon == w) return;

            //SE再生
            SoundManager.Play(SoundManager.SE.SELECT, SoundManager.BaseSEVolume);

            messageWindowText.text = MISSILE_TEXT;
            SetWeaponButtonsColor(w);
            weapon = w;
        }

        public void SelectLaser()
        {
            BaseWeapon.Weapon w = BaseWeapon.Weapon.LASER;  //名前省略
            if (weapon == w) return;

            //SE再生
            SoundManager.Play(SoundManager.SE.SELECT, SoundManager.BaseSEVolume);

            messageWindowText.text = LASER_TEXT;
            SetWeaponButtonsColor(w);
            weapon = w;
        }

        //決定
        public void SelectDecision()
        {
            //バグ防止
            if (weapon == BaseWeapon.Weapon.NONE) return;

            //SE再生
            SoundManager.Play(SoundManager.SE.SELECT, SoundManager.BaseSEVolume);

            //メインゲームに移動
            NonGameManager.LoadMainGameScene();
        }


        public void SelectItemOn()
        {
            //色変更
            if (!isItemOnButton)
            {
                //SE再生
                SoundManager.Play(SoundManager.SE.SELECT, SoundManager.BaseSEVolume);

                BattleManager.IsItemSpawn = true;
                itemOnButton.image.color = selectItemButtonColor;
                itemOffButton.image.color = notSelectButtonColor;

                isItemOnButton = true;
            }
        }

        public void SelectItemOff()
        {
            //色変更
            if (isItemOnButton)
            {
                //SE再生
                SoundManager.Play(SoundManager.SE.SELECT, SoundManager.BaseSEVolume);

                BattleManager.IsItemSpawn = false;
                itemOnButton.image.color = notSelectButtonColor;
                itemOffButton.image.color = selectItemButtonColor;

                isItemOnButton = false;
            }
        }


        public void SelectBack()
        {
            //SE再生
            SoundManager.Play(SoundManager.SE.SELECT, SoundManager.BaseSEVolume);

            //CPU選択画面に戻る
            BaseScreenManager.SetScreen(BaseScreenManager.Screen.CPU_SELECT);
        }


        //選択した武器のボタンの色変え
        void SetWeaponButtonsColor(BaseWeapon.Weapon selectWeapon)
        {
            shotgunSelectButton.image.color = notSelectButtonColor;
            missileSelectButton.image.color = notSelectButtonColor;
            laserSelectButton.image.color = notSelectButtonColor;

            if (selectWeapon == BaseWeapon.Weapon.SHOTGUN)
            {
                shotgunSelectButton.image.color = selectWeaponButtonColor;
            }
            if (selectWeapon == BaseWeapon.Weapon.MISSILE)
            {
                missileSelectButton.image.color = selectWeaponButtonColor;
            }
            if (selectWeapon == BaseWeapon.Weapon.LASER)
            {
                laserSelectButton.image.color = selectWeaponButtonColor;
            }
        }
    }
}