using System;
using UnityEngine;
using UnityEngine.UI;

public class ConfigScreen : MonoBehaviour
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

    //スライダーコンポーネント
    [SerializeField] Slider BGMSlider = null;
    [SerializeField] Slider SESlider = null;
    [SerializeField] Slider BrightnessSlider = null;
    [SerializeField] Slider CameraSlider = null;

    //スライダーの値を表示するテキスト
    [SerializeField] Text BGMValueText = null;
    [SerializeField] Text SEValueText = null;
    [SerializeField] Text BrightnessValueText = null;
    [SerializeField] Text CameraValueText = null;

    //BGM調整
    public void MoveSliderBGM()
    {
        SoundManager.BGMVolume = BGMSlider.value;
        BGMValueText.text = valueToText(BGMSlider.value);
    }

    //SE調整
    public void MoveSliderSE()
    {
        SoundManager.SEVolume = SESlider.value;
        SEValueText.text = valueToText(SESlider.value);
    }

    //明るさ調整
    public void MoveSliderBrightness()
    {
        BrightnessManager.Brightness = BrightnessSlider.value;
        BrightnessValueText.text = valueToText(BrightnessSlider.value);
    }

    //カメラ感度調整
    public void MoveSliderCamera()
    {
        CameraManager.CameraSpeed = CameraSlider.value;
        CameraValueText.text = valueToText(CameraSlider.value);
    }

    //設定初期化
    public void ClickInitialize()
    {
        Initialize();
        SoundManager.Play(SoundManager.SE.SELECT);
    }


    //戻る
    public void ClickBack()
    {
        SoundManager.Play(SoundManager.SE.CANCEL);
        SelectedButton = ButtonType.Back;
        OnButtonClick(this, EventArgs.Empty);
    }

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        // 初期化
        SoundManager.BGMVolume = 0.5f;
        SoundManager.SEVolume = 0.5f;
        BrightnessManager.Brightness = 1f;
        CameraManager.CameraSpeed = 0.5f;

        // Sliderの値の設定
        BGMSlider.value = SoundManager.BGMVolume;
        SESlider.value = SoundManager.SEVolume;
        BrightnessSlider.value = BrightnessManager.Brightness;
        CameraSlider.value = CameraManager.CameraSpeed;

        // Textの設定
        BGMValueText.text = valueToText(BGMSlider.value);
        SEValueText.text = valueToText(SESlider.value);
        BrightnessValueText.text = valueToText(BrightnessSlider.value);
        CameraValueText.text = valueToText(CameraSlider.value);
    }

    //Sliderの値をテキスト用に変換
    private string valueToText(float value)
    {
        return (value * 100).ToString("F0");
    }
}
