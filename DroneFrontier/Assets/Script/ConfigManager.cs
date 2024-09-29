using UnityEngine;
using UnityEngine.UI;

public class ConfigManager : MonoBehaviour
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
    /// ボタンクリックイベントハンドラ
    /// </summary>
    /// <param name="type">クリックされたボタン</param>
    public delegate void ButtonClickHandler(ButtonType type);

    /// <summary>
    /// ボタンクリックイベント
    /// </summary>
    public event ButtonClickHandler ButtonClick;

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

    void Start()
    {
        //Sliderの値の設定
        BGMSlider.value = SoundManager.BGMVolume;
        SESlider.value = SoundManager.SEVolume;
        BrightnessSlider.value = 1.0f - BrightnessManager.BaseAlfa;
        CameraSlider.value = CameraManager.BaseSpeed;

        //Textの設定
        BGMValueText.text = valueToText(BGMSlider.value);
        SEValueText.text = valueToText(SESlider.value);
        BrightnessValueText.text = valueToText(BrightnessSlider.value);
        CameraValueText.text = valueToText(CameraSlider.value);
    }

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
        BrightnessManager.BaseAlfa = 1.0f - BrightnessSlider.value;
        BrightnessValueText.text = valueToText(BrightnessSlider.value);
    }

    //カメラ感度調整
    public void MoveSliderCamera()
    {
        CameraManager.BaseSpeed = CameraSlider.value;
        CameraValueText.text = valueToText(CameraSlider.value);
    }

    //設定初期化
    public void ClickInitialization()
    {
        // 初期化
        SoundManager.BGMVolume = 1.0f;
        SoundManager.SEVolume = 1.0f;
        BrightnessManager.BaseAlfa = 0;
        CameraManager.BaseSpeed = 1.0f;

        // Sliderの値の設定
        BGMSlider.value = SoundManager.BGMVolume;
        SESlider.value = SoundManager.SEVolume;
        BrightnessSlider.value = 1.0f - BrightnessManager.BaseAlfa;
        CameraSlider.value = CameraManager.BaseSpeed;

        // Textの設定
        BGMValueText.text = valueToText(BGMSlider.value);
        SEValueText.text = valueToText(SESlider.value);
        BrightnessValueText.text = valueToText(BrightnessSlider.value);
        CameraValueText.text = valueToText(CameraSlider.value);
        
        SoundManager.Play(SoundManager.SE.SELECT);
    }


    //戻る
    public void ClickBack()
    {
        SoundManager.Play(SoundManager.SE.CANCEL);
        ButtonClick(ButtonType.Back);
    }

    //Sliderの値をテキスト用に変換
    string valueToText(float value)
    {
        return (value * 100).ToString("F0");
    }
}
