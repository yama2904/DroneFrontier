using System;
using UnityEngine;
using UnityEngine.UI;

public class ConfigScreen : MonoBehaviour, IScreen
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

    [SerializeField, Tooltip("BGM調整スライダー")]
    private Slider _bgmSlider = null;

    [SerializeField, Tooltip("SE調整スライダー")]
    private Slider _seSlider = null;

    [SerializeField, Tooltip("明るさ調整スライダー")]
    private Slider _brightnessSlider = null;

    [SerializeField, Tooltip("カメラ感度調整スライダー")]
    private Slider _cameraSlider = null;

    [SerializeField, Tooltip("BGM音量表示テキスト")]
    private Text _bgmValueText = null;

    [SerializeField, Tooltip("SE音量表示テキスト")]
    private Text _seValueText = null;

    [SerializeField, Tooltip("明るさ表示テキスト")]
    private Text _brightnessValueText = null;

    [SerializeField, Tooltip("カメラ感度表示テキスト")]
    private Text _cameraValueText = null;

    /// <summary>
    /// 設定初期化
    /// </summary>
    public void Initialize()
    {
        // Sliderの値の設定
        _bgmSlider.value = SoundManager.MasterBGMVolume;
        _seSlider.value = SoundManager.MasterSEVolume;
        _brightnessSlider.value = BrightnessManager.Brightness;
        _cameraSlider.value = CameraManager.CameraSpeed;

        // Textの設定
        _bgmValueText.text = ConvertToText(_bgmSlider.value);
        _seValueText.text = ConvertToText(_seSlider.value);
        _brightnessValueText.text = ConvertToText(_brightnessSlider.value);
        _cameraValueText.text = ConvertToText(_cameraSlider.value);
    }

    /// <summary>
    /// BGM調整
    /// </summary>
    public void MoveSliderBGM()
    {
        SoundManager.MasterBGMVolume = _bgmSlider.value;
        _bgmValueText.text = ConvertToText(_bgmSlider.value);
    }

    /// <summary>
    /// SE調整
    /// </summary>
    public void MoveSliderSE()
    {
        SoundManager.MasterSEVolume = _seSlider.value;
        _seValueText.text = ConvertToText(_seSlider.value);
    }

    /// <summary>
    /// 明るさ調整
    /// </summary>
    public void MoveSliderBrightness()
    {
        BrightnessManager.Brightness = _brightnessSlider.value;
        _brightnessValueText.text = ConvertToText(_brightnessSlider.value);
    }

    /// <summary>
    /// カメラ感度調整
    /// </summary>
    public void MoveSliderCamera()
    {
        CameraManager.CameraSpeed = _cameraSlider.value;
        _cameraValueText.text = ConvertToText(_cameraSlider.value);
    }

    /// <summary>
    /// 設定初期化ボタン選択
    /// </summary>
    public void ClickInitialize()
    {
        Initialize();
        SoundManager.Play(SoundManager.SE.Select);
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

    /// <summary>
    /// 設定値を表示用テキストへ変換
    /// </summary>
    /// <param name="value">変換する設定値</param>
    /// <returns>変換後のテキスト</returns>
    private string ConvertToText(float value)
    {
        return (value * 100).ToString("F0");
    }
}
