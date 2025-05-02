using Battle;
using Battle.Weapon;
using Common;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Screen
{
    public class CPUSelectScreen : MonoBehaviour, IScreen
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
        /// 選択可能武器
        /// </summary>
        private enum Weapon
        {
            Shotbun,
            Missile,
            Lazer,

            None
        }

        #region SerializeField

        /// <summary>
        /// CPU数表示テキスト
        /// </summary>
        [SerializeField]
        private Text _cpuNumText = null;

        /// <summary>
        /// CPUリストオブジェクト
        /// </summary>
        [SerializeField]
        private GameObject _cpuListObject = null;

        #endregion

        #region 定数

        /// <summary>
        /// 最大CPU数
        /// </summary>
        private const short MAX_CPU_NUM = 3;

        /// <summary>
        /// 最小CPU数
        /// </summary>
        private const short MIN_CPU_NUM = 1;

        /// <summary>
        /// 初期選択CPU数
        /// </summary>
        private const int INIT_CPU_NUM = 1;

        /// <summary>
        /// 選択中のボタン色
        /// </summary>
        private readonly Color SELECTED_COLOR = new Color(0.635f, 0.635f, 0.635f, 1f);

        /// <summary>
        /// 未選択中のボタン色
        /// </summary>
        private readonly Color UNSELECTED_COLOR = new Color(1f, 1f, 1f, 1f);

        /// <summary>
        /// 初期選択武器
        /// </summary>
        private const Weapon INIT_SELECT_WEAPON = Weapon.Shotbun;

        #endregion

        /// <summary>
        /// 選択したCPU武器
        /// </summary>
        private List<Weapon> _selectedWeapons = new List<Weapon>();

        /// <summary>
        /// ゲームオブジェクト
        /// </summary>
        private List<(GameObject line, Button[] buttons)> _objects = new List<(GameObject line, Button[] buttons)>();

        public void Initialize()
        {
            _selectedWeapons.Clear();
            for (int number = 1; number <= _objects.Count; number++)
            {
                // 初期選択CPUはショットガンを選択
                if (number <= INIT_CPU_NUM)
                {
                    _objects[number - 1].line.gameObject.SetActive(true);

                    // CPU武器リストに追加
                    _selectedWeapons.Add(INIT_SELECT_WEAPON);

                    // ボタンをショットガン選択中にする
                    ChangeButtonsColor(_objects[number - 1].buttons, INIT_SELECT_WEAPON);
                }
                else
                {
                    // リストを非表示
                    _objects[number - 1].line.gameObject.SetActive(false);
                }
            }
            _cpuNumText.text = INIT_CPU_NUM.ToString();
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// CPUの数を増やす
        /// </summary>
        public void ClickNumUp()
        {
            // 上限に達している場合は処理しない
            if (_selectedWeapons.Count >= MAX_CPU_NUM) return;

            // CPU武器リストにショットガンで追加
            _selectedWeapons.Add(INIT_SELECT_WEAPON);

            // CPU数のテキストを変更
            _cpuNumText.text = _selectedWeapons.Count.ToString();

            // CPU武器リストを1行表示
            _objects[_selectedWeapons.Count - 1].line.SetActive(true);

            // ショットガン選択中にする
            ChangeButtonsColor(_objects[_selectedWeapons.Count - 1].buttons, INIT_SELECT_WEAPON);
        }

        /// <summary>
        /// CPUの数を減らす
        /// </summary>
        public void ClickNumDown()
        {
            // 下限に達している場合は処理しない
            if (_selectedWeapons.Count <= MIN_CPU_NUM) return;

            // CPU武器リストの末端を削除
            _selectedWeapons.RemoveAt(_selectedWeapons.Count - 1);

            // CPU数のテキストを変更
            _cpuNumText.text = _selectedWeapons.Count.ToString();

            // CPU武器リストを1行非表示
            _objects[_selectedWeapons.Count].line.SetActive(false);
        }

        /// <summary>
        /// ショットガン選択
        /// </summary>
        /// <param name="cpuNumber">選択されたCPU番号</param>
        public void ClickCPUShotgun(int cpuNumber)
        {
            // 同じ武器が選ばれた場合は処理しない
            if (_selectedWeapons[cpuNumber - 1] == Weapon.Shotbun) return;

            // SE再生
            SoundManager.Play(SoundManager.SE.Select);

            // 武器更新
            _selectedWeapons[cpuNumber - 1] = Weapon.Shotbun;

            // ボタン色変更
            ChangeButtonsColor(_objects[cpuNumber - 1].buttons, Weapon.Shotbun);
        }

        /// <summary>
        /// ミサイル選択
        /// </summary>
        /// <param name="cpuNumber">選択されたCPU番号</param>
        public void ClickCPUMissile(int cpuNumber)
        {
            // 同じ武器が選ばれた場合は処理しない
            if (_selectedWeapons[cpuNumber - 1] == Weapon.Missile) return;

            //SE再生
            SoundManager.Play(SoundManager.SE.Select);

            // 武器更新
            _selectedWeapons[cpuNumber - 1] = Weapon.Missile;

            // ボタン色変更
            ChangeButtonsColor(_objects[cpuNumber - 1].buttons, Weapon.Missile);
        }

        /// <summary>
        /// レーザー選択
        /// </summary>
        /// <param name="cpuNumber">選択されたCPU番号</param>
        public void ClickCPULaser(int cpuNumber)
        {
            // 同じ武器が選ばれた場合は処理しない
            if (_selectedWeapons[cpuNumber - 1] == Weapon.Lazer) return;

            //SE再生
            SoundManager.Play(SoundManager.SE.Select);

            // 武器更新
            _selectedWeapons[cpuNumber - 1] = Weapon.Lazer;

            // ボタン色変更
            ChangeButtonsColor(_objects[cpuNumber - 1].buttons, Weapon.Lazer);
        }

        /// <summary>
        /// 決定ボタンクリック
        /// </summary>
        public void ClickOK()
        {
            // BattleManagerにCPU情報適用
            for (int i = 0; i < _selectedWeapons.Count; i++)
            {
                BattleManager.CpuData cpu = new BattleManager.CpuData
                {
                    Name = "CPU" + i
                };

                switch (_selectedWeapons[i])
                {
                    case Weapon.Shotbun:
                        cpu.Weapon = WeaponType.Shotgun;
                        break;

                    case Weapon.Missile:
                        cpu.Weapon = WeaponType.Missile;
                        break;

                    case Weapon.Lazer:
                        cpu.Weapon = WeaponType.Lazer;
                        break;

                    default:
                        throw new Exception("想定外の武器が選択されました。");
                }

                BattleManager.CpuList.Add(cpu);
            }

            SoundManager.Play(SoundManager.SE.Select);
            SelectedButton = ButtonType.Ok;
            OnButtonClick(this, EventArgs.Empty);
        }

        /// <summary>
        /// 戻るボタンクリック
        /// </summary>
        public void ClickBack()
        {
            // CPU情報クリア
            BattleManager.CpuList.Clear();

            SoundManager.Play(SoundManager.SE.Cancel);
            SelectedButton = ButtonType.Back;
            OnButtonClick(this, EventArgs.Empty);
        }

        private void Awake()
        {
            // CPU数の表示初期化
            _cpuNumText.text = INIT_CPU_NUM.ToString();

            // List1、List2...で子オブジェクトを検索して取得
            int number = 1;
            while (true)
            {
                Transform weaponObject = _cpuListObject.transform.Find("List" + number);

                // オブジェクトが存在しない場合は終了
                if (weaponObject == null) break;

                // ボタンリストに追加
                Button[] buttons = new Button[(int)Weapon.None];
                buttons[(int)Weapon.Shotbun] = weaponObject.Find("SelectShotgunButton").GetComponent<Button>();
                buttons[(int)Weapon.Missile] = weaponObject.Find("SelectMissileButton").GetComponent<Button>();
                buttons[(int)Weapon.Lazer] = weaponObject.Find("SelectLaserButton").GetComponent<Button>();

                // オブジェクトリストに追加
                _objects.Add((weaponObject.gameObject, buttons));

                number++;
            }

            Initialize();
        }

        /// <summary>
        /// 選択中のボタンを灰色にし、他のボタンの色を戻す
        /// </summary>
        /// <param name="buttons">色変更するボタンリスト</param>
        /// <param name="weapon">選択する武器</param>
        private void ChangeButtonsColor(Button[] buttons, Weapon weapon)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                if (i == (int)weapon)
                {
                    buttons[i].image.color = SELECTED_COLOR;
                }
                else
                {
                    buttons[i].image.color = UNSELECTED_COLOR;
                }
            }
        }
    }
}