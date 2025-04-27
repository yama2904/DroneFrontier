using Battle;
using Battle.Weapon;
using Common;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Screen
{
    public class WeaponSelectScreen : MonoBehaviour, IScreen
    {
        /// <summary>
        /// ボタン種類
        /// </summary>
        public enum ButtonType
        {
            /// <summary>
            /// 決定
            /// </summary>
            Ok,

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

        [Header("武器選択ボタン")]
        [SerializeField, Tooltip("ショットガン選択ボタン")]
        private Button _shotgunButton = null;

        [SerializeField, Tooltip("ミサイル選択ボタン")]
        private Button _missileButton = null;

        [SerializeField, Tooltip("レーザー選択ボタン")]
        private Button _laserButton = null;

        [SerializeField, Tooltip("武器選択時のボタン色")]
        private Color _selectWeaponColor = new Color(0.784f, 0.784f, 0.784f, 1f);

        [SerializeField, Tooltip("武器非選択時のボタン色")]
        private Color _notSelectWeaponColor = new Color(1f, 1f, 1f, 1f);

        [Header("アイテム選択ボタン")]
        [SerializeField, Tooltip("アイテムonボタン")]
        private Button _itemOnButton = null;

        [SerializeField, Tooltip("アイテムoffボタン")]
        private Button _itemOffButton = null;

        [SerializeField, Tooltip("アイテムon/off選択時のボタン色")]
        private Color _selectItemColor = new Color(0.3f, 0.46f, 1f, 1f);

        [SerializeField, Tooltip("アイテムon/off非選択時のボタン色")]
        private Color _notSelectItemColor = new Color(1f, 1f, 1f, 1f);

        [Header("武器説明欄")]
        [SerializeField, Tooltip("武器説明文テキスト")]
        private Text _descriptionText = null;

        [SerializeField, TextArea, Tooltip("ショットガン選択時の説明文")]
        private string _shotgunDescription = "射程が非常に短いが威力が高く、リキャストが短い。\nまた、攻撃中の移動速度低下がなく、ブーストが多少強化される。\n近距離特化型。";

        [SerializeField, TextArea, Tooltip("ミサイル選択時の説明文")]
        private string _missileDescription = "誘導力とスピードが高く、発射後に爆発を起こす。\nリキャストが長い。\n最も安定している。";

        [SerializeField, TextArea, Tooltip("レーザー選択時の説明文")]
        private string _lazerDescription = "極めて高威力だが、発動時にチャージが必要。\nまた、攻撃中の移動速度低下が大きい。\n扱いづらく、上級者向け。";

        /// <summary>
        /// 選択した武器
        /// </summary>
        private WeaponType _selectedWeapon = WeaponType.None;

        /// <summary>
        /// アイテムonボタン選択中であるか
        /// </summary>
        private bool _isSelectedItemOn = true;

        public void Initialize()
        {
            BattleManager.PlayerWeapon = WeaponType.None;
            _descriptionText.text = "武器を選択してください。";
            ChangeWeaponButtonsColor(WeaponType.None);
            ChangeItemButtonsColor(true);
        }

        /// <summary>
        /// ショットガン選択
        /// </summary>
        public void ClickShotgun()
        {
            const WeaponType WEAPON = WeaponType.Shotgun;

            // 既にショットガン選択中の場合は何もしない
            if (_selectedWeapon == WEAPON) return;

            // SE再生
            SoundManager.Play(SoundManager.SE.Select);

            // ショットガンの説明を表示してボタン色変更
            _descriptionText.text = _shotgunDescription;
            ChangeWeaponButtonsColor(WEAPON);

            // 選択武器更新
            _selectedWeapon = WEAPON;
        }

        /// <summary>
        /// ミサイル選択
        /// </summary>
        public void ClickMissile()
        {
            const WeaponType WEAPON = WeaponType.Missile;

            // 既にミサイル選択中の場合は何もしない
            if (_selectedWeapon == WEAPON) return;

            // SE再生
            SoundManager.Play(SoundManager.SE.Select);

            // ミサイルの説明を表示してボタン色変更
            _descriptionText.text = _missileDescription;
            ChangeWeaponButtonsColor(WEAPON);

            // 選択武器更新
            _selectedWeapon = WEAPON;
        }

        /// <summary>
        /// レーザー選択
        /// </summary>
        public void ClickLaser()
        {
            const WeaponType WEAPON = WeaponType.Lazer;

            // 既にレーザー選択中の場合は何もしない
            if (_selectedWeapon == WEAPON) return;

            // SE再生
            SoundManager.Play(SoundManager.SE.Select);

            // レーザーの説明を表示してボタン色変更
            _descriptionText.text = _lazerDescription;
            ChangeWeaponButtonsColor(WEAPON);

            // 選択武器更新
            _selectedWeapon = WEAPON;
        }

        /// <summary>
        /// アイテムon選択
        /// </summary>
        public void ClickItemOn()
        {
            // 既にアイテムon選択中の場合は何もしない
            if (_isSelectedItemOn) return;

            // SE再生
            SoundManager.Play(SoundManager.SE.Select);

            // ボタン色変更
            ChangeItemButtonsColor(true);

            // BattleManagerにアイテムonを伝える
            BattleManager.IsItemSpawn = true;

            _isSelectedItemOn = true;
        }

        /// <summary>
        /// アイテムoff選択
        /// </summary>
        public void ClickItemOff()
        {
            // 既にアイテムoff選択中の場合は何もしない
            if (!_isSelectedItemOn) return;

            // SE再生
            SoundManager.Play(SoundManager.SE.Select);

            // ボタン色変更
            ChangeItemButtonsColor(false);

            // BattleManagerにアイテムoffを伝える
            BattleManager.IsItemSpawn = false;

            _isSelectedItemOn = false;
        }

        /// <summary>
        /// 決定ボタン選択
        /// </summary>
        public void ClickOK()
        {
            // 武器未選択の場合は何もしない
            if (_selectedWeapon == WeaponType.None) return;

            // SE再生
            SoundManager.Play(SoundManager.SE.Select);

            // BattleManagerに選択武器を伝える
            BattleManager.PlayerWeapon = _selectedWeapon;

            // ボタン選択イベント発火
            SelectedButton = ButtonType.Ok;
            OnButtonClick(this, EventArgs.Empty);
        }

        /// <summary>
        /// 戻るボタン選択
        /// </summary>
        public void ClickBack()
        {
            // SE再生
            SoundManager.Play(SoundManager.SE.Cancel);

            // ボタン選択イベント発火
            SelectedButton = ButtonType.Back;
            OnButtonClick(this, EventArgs.Empty);
        }

        /// <summary>
        /// 武器選択ボタン色を変更
        /// </summary>
        /// <param name="selectWeapon">現在選択中の武器</param>
        private void ChangeWeaponButtonsColor(WeaponType selectWeapon)
        {
            // 一度全ての武器を非選択色に変更
            _shotgunButton.image.color = _notSelectWeaponColor;
            _missileButton.image.color = _notSelectWeaponColor;
            _laserButton.image.color = _notSelectWeaponColor;

            // 選択したボタンを選択色に変更
            Button selectButton = null;
            switch (selectWeapon)
            {
                case WeaponType.Shotgun:
                    selectButton = _shotgunButton;
                    break;

                case WeaponType.Missile:
                    selectButton = _missileButton;
                    break;

                case WeaponType.Lazer:
                    selectButton = _laserButton;
                    break;

                default:
                    return;
            }
            selectButton.image.color = _selectWeaponColor;
        }

        /// <summary>
        /// アイテムon/offボタン色を変更
        /// </summary>
        /// <param name="itemOn">アイテムon選択中の場合はtrue</param>
        private void ChangeItemButtonsColor(bool itemOn)
        {
            // 一度両方を非選択色に変更
            _itemOnButton.image.color = _notSelectItemColor;
            _itemOffButton.image.color = _notSelectItemColor;

            // 選択したボタンを選択色に変更
            if (itemOn)
            {
                _itemOnButton.image.color = _selectItemColor;
            }
            else
            {
                _itemOffButton.image.color = _selectItemColor;
            }
        }
    }
}