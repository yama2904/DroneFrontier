using Common;
using System;
using UnityEngine;

namespace Screen
{
    public class SoloMultiSelectScreen : MonoBehaviour, IScreen
    {
        /// <summary>
        /// ボタン種類
        /// </summary>
        public enum ButtonType
        {
            /// <summary>
            /// ソロモード
            /// </summary>
            SoloMode,

            /// <summary>
            /// マルチモード
            /// </summary>
            MultiMode,

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

        public void Initialize() { }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// ソロモード選択
        /// </summary>
        public void ClickSolo()
        {
            SoundManager.Play(SoundManager.SE.Select);
            SelectedButton = ButtonType.SoloMode;
            OnButtonClick(this, EventArgs.Empty);
        }

        /// <summary>
        /// マルチモード選択
        /// </summary>
        public void ClickMulti()
        {
            // SE再生
            SoundManager.Play(SoundManager.SE.Select);

            // ボタン選択イベント発火
            SoundManager.Play(SoundManager.SE.Select);
            SelectedButton = ButtonType.MultiMode;
            OnButtonClick(this, EventArgs.Empty);
        }

        /// <summary>
        /// 戻るボタン選択
        /// </summary>
        public void ClickBack()
        {
            SoundManager.Play(SoundManager.SE.Cancel);
            SelectedButton = ButtonType.Back;
            OnButtonClick(this, EventArgs.Empty);
        }
    }
}