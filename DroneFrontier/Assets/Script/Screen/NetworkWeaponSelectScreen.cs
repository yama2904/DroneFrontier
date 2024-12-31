using Offline;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Network
{
    public class NetworkWeaponSelectScreen : MyNetworkBehaviour
    {
        /// <summary>
        /// ボタン種類
        /// </summary>
        public enum ButtonType
        {
            /// <summary>
            /// 決定
            /// </summary>
            OK,

            /// <summary>
            /// 戻る
            /// </summary>
            Back
        }

        /// <summary>
        /// 選択したボタン
        /// </summary>
        public ButtonType SelectedButton { get; private set; }

        /// <summary>
        /// ボタンクリックイベント
        /// </summary>
        public event EventHandler OnButtonClick;

        //説明文に表示するテキスト
        private const string SHOTGUN_TEXT = "射程が非常に短いが威力が高く、リキャストが短い。\nまた、攻撃中の移動速度低下がなく、ブーストが多少強化される。\n近距離特化型。";
        private const string MISSILE_TEXT = "誘導力とスピードが高く、発射後に爆発を起こす。\nリキャストが長い。\n最も安定している。";
        private const string LASER_TEXT = "極めて高威力だが、発動時にチャージが必要。\nまた、攻撃中の移動速度低下が大きい。\n扱いづらく、上級者向け。";

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
            BattleManager.PlayerWeapon = WeaponType.NONE;
            messageWindowText.text = "武器を選択してください。";
        }

        public void ClickShotgun()
        {
            WeaponType selectWeapon = WeaponType.SHOTGUN;
            if (BattleManager.PlayerWeapon == selectWeapon) return;

            //SE再生
            SoundManager.Play(SoundManager.SE.SELECT, SoundManager.SEVolume);

            messageWindowText.text = SHOTGUN_TEXT;
            SetWeaponButtonsColor(selectWeapon);
            BattleManager.PlayerWeapon = selectWeapon;
        }

        public void ClickMissile()
        {
            WeaponType selectWeapon = WeaponType.MISSILE;
            if (BattleManager.PlayerWeapon == selectWeapon) return;

            //SE再生
            SoundManager.Play(SoundManager.SE.SELECT, SoundManager.SEVolume);

            messageWindowText.text = MISSILE_TEXT;
            SetWeaponButtonsColor(selectWeapon);
            BattleManager.PlayerWeapon = selectWeapon;
        }

        public void ClickLaser()
        {
            WeaponType selectWeapon = WeaponType.LASER;
            if (BattleManager.PlayerWeapon == selectWeapon) return;

            //SE再生
            SoundManager.Play(SoundManager.SE.SELECT, SoundManager.SEVolume);

            messageWindowText.text = LASER_TEXT;
            SetWeaponButtonsColor(selectWeapon);
            BattleManager.PlayerWeapon = selectWeapon;
        }


        public void ClickItemOn()
        {
            //色変更
            if (!isItemOnButton)
            {
                //SE再生
                SoundManager.Play(SoundManager.SE.SELECT, SoundManager.SEVolume);

                BattleManager.IsItemSpawn = true;
                itemOnButton.image.color = selectItemButtonColor;
                itemOffButton.image.color = notSelectButtonColor;

                isItemOnButton = true;
            }
        }

        public void ClickItemOff()
        {
            //色変更
            if (isItemOnButton)
            {
                //SE再生
                SoundManager.Play(SoundManager.SE.SELECT, SoundManager.SEVolume);

                BattleManager.IsItemSpawn = false;
                itemOnButton.image.color = notSelectButtonColor;
                itemOffButton.image.color = selectItemButtonColor;

                isItemOnButton = false;
            }
        }

        //決定
        public void ClickOK()
        {
            if (BattleManager.PlayerWeapon == WeaponType.NONE) return;

            SoundManager.Play(SoundManager.SE.SELECT, SoundManager.SEVolume);
            SelectedButton = ButtonType.OK;
            OnButtonClick(this, EventArgs.Empty);
        }

        public void ClickBack()
        {
            SoundManager.Play(SoundManager.SE.CANCEL);
            SelectedButton = ButtonType.Back;
            OnButtonClick(this, EventArgs.Empty);
        }


        //選択した武器のボタンの色変え
        void SetWeaponButtonsColor(WeaponType selectWeapon)
        {
            shotgunSelectButton.image.color = notSelectButtonColor;
            missileSelectButton.image.color = notSelectButtonColor;
            laserSelectButton.image.color = notSelectButtonColor;

            if (selectWeapon == WeaponType.SHOTGUN)
            {
                shotgunSelectButton.image.color = selectWeaponButtonColor;
            }
            if (selectWeapon == WeaponType.MISSILE)
            {
                missileSelectButton.image.color = selectWeaponButtonColor;
            }
            if (selectWeapon == WeaponType.LASER)
            {
                laserSelectButton.image.color = selectWeaponButtonColor;
            }
        }
    }
}