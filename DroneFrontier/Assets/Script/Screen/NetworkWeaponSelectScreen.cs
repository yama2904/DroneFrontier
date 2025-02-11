using System;
using System.Collections;
using System.Collections.Specialized;
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

        /// <summary>
        /// 通信切断時のエラーメッセージ
        /// </summary>
        private const string DISCONNECT_MESSAGE = "通信が切断されました。";

        // 説明文に表示するテキスト
        private const string SHOTGUN_TEXT = "射程が非常に短いが威力が高く、リキャストが短い。\nまた、攻撃中の移動速度低下がなく、ブーストが多少強化される。\n近距離特化型。";
        private const string MISSILE_TEXT = "誘導力とスピードが高く、発射後に爆発を起こす。\nリキャストが長い。\n最も安定している。";
        private const string LASER_TEXT = "極めて高威力だが、発動時にチャージが必要。\nまた、攻撃中の移動速度低下が大きい。\n扱いづらく、上級者向け。";

        //武器選択ボタン用
        [SerializeField] Button _shotgunButton = null;
        [SerializeField] Button _missileButton = null;
        [SerializeField] Button _laserButton = null;
        [SerializeField] Text _descriptionText = null;  //武器の説明
        Color selectWeaponButtonColor = new Color(0.784f, 0.784f, 0.784f, 1f);  //武器を選択するボタンを押したときの色

        //アイテムボタン処理用
        [SerializeField, Tooltip("アイテム設定Canvas")]
        private Canvas _itemCanvas = null;

        [SerializeField] Button _itemOnButton = null;
        [SerializeField] Button _itemOffButton = null;
        Color selectItemButtonColor = new Color(0.3f, 0.46f, 1f, 1f);  //アイテムボタンを選択したときの色
        Color notSelectButtonColor = new Color(1f, 1f, 1f, 1f);  //ボタンを押してないときの色
        bool isItemOnButton = true; //アイテムオンか

        [SerializeField, Tooltip("決定ボタン")]
        private Button _okButton = null;

        [SerializeField, Tooltip("エラーメッセージのCanvas")]
        private Canvas _errMsgCanvas = null;

        [SerializeField, Tooltip("エラーメッセージ表示用テキスト")]
        private Text _errMsgText = null;

        /// <summary>
        /// 選択した武器
        /// </summary>
        private WeaponType _selectedWeapon = WeaponType.NONE;

        /// <summary>
        /// 各プレイヤーの選択武器<br/>
        /// key:プレイヤー名 [string]<br/>
        /// value:選択した武器 [WeaponType]
        /// </summary>
        private OrderedDictionary _selectedWeapons = new OrderedDictionary();

        /// <summary>
        /// 通信エラーが発生したか
        /// </summary>
        private bool _isError = false;

        private void Update()
        {
            if (_isError && Input.GetMouseButtonUp(0))
            {
                // SE再生
                SoundManager.Play(SoundManager.SE.SELECT, SoundManager.SEVolume);

                // エラーメッセージ非表示
                _errMsgCanvas.enabled = false;
                _errMsgText.text = "";
                _isError = false;

                // 前の画面に戻る
                SelectedButton = ButtonType.Back;
                OnButtonClick(this, EventArgs.Empty);
            }
        }

        private void OnEnable()
        {
            // 表示初期化
            _descriptionText.text = "武器を選択してください。";
            _selectedWeapons.Clear();
            EnabledButtons(true);

            if (MyNetworkManager.Singleton.IsClient)
            {
                _itemCanvas.enabled = false;
            }

            // 通信イベント設定
            MyNetworkManager.Singleton.OnDisconnect += OnDisconnect;
        }

        public void ClickShotgun()
        {
            WeaponType selectWeapon = WeaponType.SHOTGUN;
            if (_selectedWeapon == selectWeapon) return;

            // SE再生
            SoundManager.Play(SoundManager.SE.SELECT, SoundManager.SEVolume);

            _descriptionText.text = SHOTGUN_TEXT;
            SetWeaponButtonsColor(selectWeapon);
            _selectedWeapon = selectWeapon;
        }

        public void ClickMissile()
        {
            WeaponType selectWeapon = WeaponType.MISSILE;
            if (_selectedWeapon == selectWeapon) return;

            //SE再生
            SoundManager.Play(SoundManager.SE.SELECT, SoundManager.SEVolume);

            _descriptionText.text = MISSILE_TEXT;
            SetWeaponButtonsColor(selectWeapon);
            _selectedWeapon = selectWeapon;
        }

        public void ClickLaser()
        {
            WeaponType selectWeapon = WeaponType.LASER;
            if (_selectedWeapon == selectWeapon) return;

            //SE再生
            SoundManager.Play(SoundManager.SE.SELECT, SoundManager.SEVolume);

            _descriptionText.text = LASER_TEXT;
            SetWeaponButtonsColor(selectWeapon);
            _selectedWeapon = selectWeapon;
        }


        public void ClickItemOn()
        {
            //色変更
            if (!isItemOnButton)
            {
                //SE再生
                SoundManager.Play(SoundManager.SE.SELECT, SoundManager.SEVolume);

                //BattleManager.IsItemSpawn = true;
                _itemOnButton.image.color = selectItemButtonColor;
                _itemOffButton.image.color = notSelectButtonColor;

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

                //BattleManager.IsItemSpawn = false;
                _itemOnButton.image.color = notSelectButtonColor;
                _itemOffButton.image.color = selectItemButtonColor;

                isItemOnButton = false;
            }
        }

        //決定
        public void ClickOK()
        {
            if (_selectedWeapon == WeaponType.NONE) return;

            // SE再生
            SoundManager.Play(SoundManager.SE.SELECT, SoundManager.SEVolume);

            // 選択武器送信
            SendMethod(() => SelectWeapon(MyNetworkManager.Singleton.PlayerName, _selectedWeapon));

            // ボタン非活性
            EnabledButtons(false);
        }

        //選択した武器のボタンの色変え
        private void SetWeaponButtonsColor(WeaponType selectWeapon)
        {
            _shotgunButton.image.color = notSelectButtonColor;
            _missileButton.image.color = notSelectButtonColor;
            _laserButton.image.color = notSelectButtonColor;

            if (selectWeapon == WeaponType.SHOTGUN)
            {
                _shotgunButton.image.color = selectWeaponButtonColor;
            }
            if (selectWeapon == WeaponType.MISSILE)
            {
                _missileButton.image.color = selectWeaponButtonColor;
            }
            if (selectWeapon == WeaponType.LASER)
            {
                _laserButton.image.color = selectWeaponButtonColor;
            }
        }

        /// <summary>
        /// 各ボタンの活性制御を行う
        /// </summary>
        /// <param name="enabled">活性の場合はtrue</param>
        private void EnabledButtons(bool enabled)
        {
            _shotgunButton.enabled = enabled;
            _missileButton.enabled = enabled;
            _laserButton.enabled = enabled;
            _itemOnButton.enabled = enabled;
            _itemOffButton.enabled = enabled;
            _okButton.enabled = enabled;
            _okButton.image.color = enabled ? notSelectButtonColor : selectWeaponButtonColor;
        }

        /// <summary>
        /// 使用する武器を決定
        /// </summary>
        /// <param name="player">プレイヤー名</param>
        /// <param name="weapon">選択した武器</param>
        private void SelectWeapon(string player, WeaponType weapon)
        {
            _selectedWeapons.Add(player, weapon);
            
            // 全てのプレイヤーが選択済みの場合はゲーム開始
            if (_selectedWeapons.Count == MyNetworkManager.Singleton.PlayerCount
                && MyNetworkManager.Singleton.IsHost)
            {
                SendMethod(() => StartGame(_selectedWeapons, isItemOnButton));
            }
        }

        /// <summary>
        /// ゲーム開始
        /// </summary>
        /// <param name="weapons">各プレイヤーの使用武器</param>
        /// <param name="itemOn">アイテム有であるか</param>
        private void StartGame(OrderedDictionary weapons, bool itemOn)
        {
            // イベント削除
            MyNetworkManager.Singleton.OnDisconnect -= OnDisconnect;

            // NetworkBattleManagerにプレイヤー情報送信
            foreach (DictionaryEntry entity in weapons)
            {
                string name = entity.Key as string;
                WeaponType weapon = (WeaponType)Enum.Parse(typeof(WeaponType), entity.Value.ToString());
                NetworkBattleManager.PlayerData data = new NetworkBattleManager.PlayerData
                {
                    Name = name,
                    Weapon = weapon,
                    IsControl = name == MyNetworkManager.Singleton.PlayerName
                };
                NetworkBattleManager.PlayerList.Add(data);
            }

            // アイテム有無をBattleManagerに送信
            NetworkBattleManager.IsItemSpawn = itemOn;

            // 次の画面へ遷移
            SelectedButton = ButtonType.Ok;
            OnButtonClick(this, EventArgs.Empty);
        }

        /// <summary>
        /// プレイヤー切断イベント
        /// </summary>
        /// <param name="name">切断したプレイヤー名</param>
        /// <param name="isHost">切断したプレイヤーがホストであるか</param>
        private void OnDisconnect(string name, bool isHost)
        {
            // イベント削除
            MyNetworkManager.Singleton.OnDisconnect -= OnDisconnect;

            // 通信切断
            MyNetworkManager.Singleton.Disconnect();

            // エラーメッセージ表示
            _errMsgCanvas.enabled = true;
            _errMsgText.text = DISCONNECT_MESSAGE;
            _isError = true;
        }
    }
}