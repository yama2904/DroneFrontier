using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

namespace Online
{
    public class WeaponSelectManager : NetworkBehaviour
    {
        const string SHOTGUN_TEXT = "射程が非常に短いが威力が高く、リキャストが短い。\nまた、攻撃中の移動速度低下がなく、ブーストが多少強化される。\n近距離特化型。";
        const string MISSILE_TEXT = "誘導力とスピードが高く、発射後に爆発を起こす。\nリキャストが長い。\n最も安定している。";
        const string LASER_TEXT = "極めて高威力だが、発動時にチャージが必要。\nまた、攻撃中の移動速度低下が大きい。\n扱いづらく、上級者向け。";


        //選択した武器
        BaseWeapon.Weapon weapon = BaseWeapon.Weapon.NONE;

        //武器選択ボタン用
        [SerializeField] Button shotgunSelectButton = null;
        [SerializeField] Button missileSelectButton = null;
        [SerializeField] Button laserSelectButton = null;
        [SerializeField] Text messageWindowText = null;  //武器の説明
        Color selectWeaponButtonColor = new Color(0.784f, 0.784f, 0.784f, 1f);  //武器を選択するボタンを押したときの色

        //アイテムボタン処理用
        [SerializeField] GameObject itemSelectParent = null;
        [SerializeField] Button itemOnButton = null;
        [SerializeField] Button itemOffButton = null;
        Color selectItemButtonColor = new Color(0.3f, 0.46f, 1f, 1f);  //アイテムボタンを選択したときの色
        Color notSelectButtonColor = new Color(1f, 1f, 1f, 1f);  //ボタンを押してないときの色
        bool isItemOnButton = false; //アイテムオンか

        //決定ボタン
        [SerializeField] Button decisionButton = null;


        void Awake()
        {
            messageWindowText.text = "武器を選択してください。";
        }

        public void DisplayItemSelect()
        {
            //アイテム選択枠の表示
            itemSelectParent.SetActive(true);

            SelectItemOn(); //デフォルトでアイテムONボタンが押されているようにする
        }


        public void SelectShotgun()
        {
            BaseWeapon.Weapon w = BaseWeapon.Weapon.SHOTGUN;  //名前省略
            if (weapon == w) return;

            //SE再生
            SoundManager.Play(SoundManager.SE.Select, SoundManager.MasterSEVolume);

            messageWindowText.text = SHOTGUN_TEXT;
            SetWeaponButtonsColor(w);
            weapon = w;
        }

        public void SelectMissile()
        {
            BaseWeapon.Weapon w = BaseWeapon.Weapon.MISSILE;  //名前省略
            if (weapon == w) return;

            //SE再生
            SoundManager.Play(SoundManager.SE.Select, SoundManager.MasterSEVolume);

            messageWindowText.text = MISSILE_TEXT;
            SetWeaponButtonsColor(w);
            weapon = w;
        }

        public void SelectLaser()
        {
            BaseWeapon.Weapon w = BaseWeapon.Weapon.LASER;  //名前省略
            if (weapon == w) return;

            //SE再生
            SoundManager.Play(SoundManager.SE.Select, SoundManager.MasterSEVolume);

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
            SoundManager.Play(SoundManager.SE.Select, SoundManager.MasterSEVolume);

            //選択した武器の情報を送る
            MatchingManager.Singleton.CmdSetWeapon((int)weapon);

            //すべてのボタンを押せないようにする
            shotgunSelectButton.interactable = false;
            missileSelectButton.interactable = false;
            laserSelectButton.interactable = false;
            decisionButton.interactable = false;
            if (isServer)
            {
                itemOnButton.interactable = false;
                itemOffButton.interactable = false;
            }
        }


        public void SelectItemOn()
        {
            //色変更
            if (!isItemOnButton)
            {
                //SE再生
                SoundManager.Play(SoundManager.SE.Select, SoundManager.MasterSEVolume);

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
                SoundManager.Play(SoundManager.SE.Select, SoundManager.MasterSEVolume);

                BattleManager.IsItemSpawn = false;
                itemOnButton.image.color = notSelectButtonColor;
                itemOffButton.image.color = selectItemButtonColor;

                isItemOnButton = false;
            }
        }


        //武器のボタンを押した時のボタンの色変え
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