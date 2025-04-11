using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Offline
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

        /// <summary>
        /// 選択可能武器
        /// </summary>
        private enum Weapon
        {
            SHOTGUN,
            MISSILE,
            LASER,

            NONE
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
        private const Weapon INIT_SELECT_WEAPON = Weapon.SHOTGUN;

        #endregion

        /// <summary>
        /// 選択したCPU武器リスト
        /// </summary>
        private List<Weapon> _cpuWeaponList = new List<Weapon>();

        /// <summary>
        /// CPU武器選択オブジェクトリスト
        /// </summary>
        private List<GameObject> _cpuWeaponObjectList = new List<GameObject>();

        /// <summary>
        /// 武器ボタンリスト
        /// </summary>
        private List<Button[]> _cpuWeaponButtonList = new List<Button[]>();

        public void Initialize() { }

        /// <summary>
        /// CPUの数を増やす
        /// </summary>
        public void ClickNumUp()
        {
            // 上限に達している場合は処理しない
            if (_cpuWeaponList.Count >= MAX_CPU_NUM) return;

            // CPU武器リストにショットガンで追加
            _cpuWeaponList.Add(INIT_SELECT_WEAPON);

            // CPU数のテキストを変更
            _cpuNumText.text = _cpuWeaponList.Count.ToString();

            // CPU武器リストを1行表示
            _cpuWeaponObjectList[_cpuWeaponList.Count - 1].SetActive(true);

            // ショットガン選択中にする
            ChangeButtonsColor(_cpuWeaponButtonList[_cpuWeaponList.Count - 1], INIT_SELECT_WEAPON);
        }

        /// <summary>
        /// CPUの数を減らす
        /// </summary>
        public void ClickNumDown()
        {
            // 下限に達している場合は処理しない
            if (_cpuWeaponList.Count <= MIN_CPU_NUM) return;

            // CPU武器リストの末端を削除
            _cpuWeaponList.RemoveAt(_cpuWeaponList.Count - 1);

            // CPU数のテキストを変更
            _cpuNumText.text = _cpuWeaponList.Count.ToString();

            // CPU武器リストを1行非表示
            _cpuWeaponObjectList[_cpuWeaponList.Count].SetActive(false);
        }

        /// <summary>
        /// ショットガン選択
        /// </summary>
        /// <param name="cpuNumber">選択されたCPU番号</param>
        public void ClickCPUShotgun(int cpuNumber)
        {
            // 同じ武器が選ばれた場合は処理しない
            if (_cpuWeaponList[cpuNumber - 1] == Weapon.SHOTGUN) return;

            //SE再生
            SoundManager.Play(SoundManager.SE.Select, SoundManager.MasterSEVolume);

            // 武器更新
            _cpuWeaponList[cpuNumber - 1] = Weapon.SHOTGUN;

            // ボタン色変更
            ChangeButtonsColor(_cpuWeaponButtonList[cpuNumber - 1], Weapon.SHOTGUN);
        }

        /// <summary>
        /// ミサイル選択
        /// </summary>
        /// <param name="cpuNumber">選択されたCPU番号</param>
        public void ClickCPUMissile(int cpuNumber)
        {
            // 同じ武器が選ばれた場合は処理しない
            if (_cpuWeaponList[cpuNumber - 1] == Weapon.MISSILE) return;

            //SE再生
            SoundManager.Play(SoundManager.SE.Select, SoundManager.MasterSEVolume);

            // 武器更新
            _cpuWeaponList[cpuNumber - 1] = Weapon.MISSILE;

            // ボタン色変更
            ChangeButtonsColor(_cpuWeaponButtonList[cpuNumber - 1], Weapon.MISSILE);
        }

        /// <summary>
        /// レーザー選択
        /// </summary>
        /// <param name="cpuNumber">選択されたCPU番号</param>
        public void ClickCPULaser(int cpuNumber)
        {
            // 同じ武器が選ばれた場合は処理しない
            if (_cpuWeaponList[cpuNumber - 1] == Weapon.LASER) return;

            //SE再生
            SoundManager.Play(SoundManager.SE.Select, SoundManager.MasterSEVolume);

            // 武器更新
            _cpuWeaponList[cpuNumber - 1] = Weapon.LASER;

            // ボタン色変更
            ChangeButtonsColor(_cpuWeaponButtonList[cpuNumber - 1], Weapon.LASER);
        }

        /// <summary>
        /// 決定ボタンクリック
        /// </summary>
        public void ClickOK()
        {
            // BattleManagerにCPU情報適用
            for (int i = 0; i < _cpuWeaponList.Count; i++)
            {
                BattleManager.CpuData cpu = new BattleManager.CpuData
                {
                    Name = "CPU" + i
                };

                switch (_cpuWeaponList[i])
                {
                    case Weapon.SHOTGUN:
                        cpu.Weapon = WeaponType.SHOTGUN; 
                        break;

                    case Weapon.MISSILE:
                        cpu.Weapon = WeaponType.MISSILE;
                        break;

                    case Weapon.LASER:
                        cpu.Weapon = WeaponType.LASER;
                        break;

                    default:
                        throw new Exception("想定外の武器が選択されました。");
                }

                BattleManager.CpuList.Add(cpu);
            }

            SoundManager.Play(SoundManager.SE.Select, SoundManager.MasterSEVolume);
            SelectedButton = ButtonType.OK;
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

        private void Start()
        {
            // CPU数の表示初期化
            _cpuNumText.text = INIT_CPU_NUM.ToString();

            // List1、List2...で子オブジェクトを検索して取得
            int num = 1;
            while (true)
            {
                Transform weaponObject = _cpuListObject.transform.Find("List" + num);

                // オブジェクトが存在しない場合は終了
                if (weaponObject == null) break;

                // オブジェクトリストに追加
                _cpuWeaponObjectList.Add(weaponObject.gameObject);

                // ボタンリストに追加
                Button[] buttons = new Button[(int)Weapon.NONE];
                buttons[(int)Weapon.SHOTGUN] = weaponObject.Find("SelectShotgunButton").GetComponent<Button>();
                buttons[(int)Weapon.MISSILE] = weaponObject.Find("SelectMissileButton").GetComponent<Button>();
                buttons[(int)Weapon.LASER] = weaponObject.Find("SelectLaserButton").GetComponent<Button>();
                _cpuWeaponButtonList.Add(buttons);

                // 初期選択CPU数を超える場合
                if (num > INIT_CPU_NUM)
                {
                    // リストを非表示
                    weaponObject.gameObject.SetActive(false);
                }
                else
                {
                    // 初期CPU数に達していない場合、ショットガンを初期選択

                    // CPU武器リストに追加
                    _cpuWeaponList.Add(INIT_SELECT_WEAPON);

                    // ボタンをショットガン選択中にする
                    ChangeButtonsColor(buttons, INIT_SELECT_WEAPON);
                }

                num++;
            }
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