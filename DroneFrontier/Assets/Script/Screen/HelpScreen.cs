using Common;
using System;
using UnityEngine;

namespace Screen
{
    public class HelpScreen : MonoBehaviour, IScreen
    {
        /// <summary>
        /// ボタン種類
        /// </summary>
        public enum ButtonType
        {
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

        [SerializeField]
        private GameObject HelpBasicOperationDescription = null;

        [SerializeField]
        private GameObject HelpBattleModeDescription = null;

        [SerializeField]
        private GameObject HelpRaceModeDescription = null;

        private enum Help
        {
            Basic,
            Battle,
            Race,

            None
        }
        Help selectHelp = Help.None;

        public void Initialize() { }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        //基本操作
        public void ClickBasicOperation()
        {
            SoundManager.Play(SoundManager.SE.Select);

            HelpBasicOperationDescription.SetActive(true);
            selectHelp = Help.Basic;
        }

        //バトルモード
        public void ClickBattleModeHelp()
        {
            SoundManager.Play(SoundManager.SE.Select);

            HelpBattleModeDescription.SetActive(true);
            selectHelp = Help.Battle;
        }

        //レースモード
        public void ClickRaceModeHelp()
        {
            SoundManager.Play(SoundManager.SE.Select);

            HelpRaceModeDescription.SetActive(true);
            selectHelp = Help.Race;
        }

        //戻る
        public void ClickBack()
        {
            SoundManager.Play(SoundManager.SE.Cancel);

            switch (selectHelp)
            {
                case Help.Basic:
                    HelpBasicOperationDescription.SetActive(false);
                    break;

                case Help.Battle:
                    HelpBattleModeDescription.SetActive(false);
                    break;

                case Help.Race:
                    HelpRaceModeDescription.SetActive(false);
                    break;

                default:
                    SelectedButton = ButtonType.Back;
                    OnButtonClick(this, EventArgs.Empty);
                    break;
            }

            selectHelp = Help.None;
        }
    }
}