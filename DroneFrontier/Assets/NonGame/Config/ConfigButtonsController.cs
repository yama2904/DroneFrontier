using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConfigButtonsController : MonoBehaviour
{
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
        BGMSlider.value = SoundManager.GetBaseVolumeBGM();
        SESlider.value = SoundManager.GetBaseVolumeSE();
        BrightnessSlider.value = 1.0f - BrightnessManager.GetBaseAlfa();
        CameraSlider.value = CameraManager.GetBaseSpeed();

        //Textの設定
        BGMValueText.text = valueToText(BGMSlider.value);
        SEValueText.text = valueToText(SESlider.value);
        BrightnessValueText.text = valueToText(BrightnessSlider.value);
        CameraValueText.text = valueToText(CameraSlider.value);
    }

    //BGM調整
    public void MoveSliderBGM()
    {
        SoundManager.SetBaseVolumeBGM(BGMSlider.value);
        BGMValueText.text = valueToText(BGMSlider.value);
    }

    //SE調整
    public void MoveSliderSE()
    {
        SoundManager.SetBaseVolumeSE(SESlider.value);
        SEValueText.text = valueToText(SESlider.value);
    }

    //明るさ調整
    public void MoveSliderBrightness()
    {
        BrightnessManager.SetBaseAlfa(1.0f - BrightnessSlider.value);
        BrightnessValueText.text = valueToText(BrightnessSlider.value);
    }

    //カメラ感度調整
    public void MoveSliderCamera()
    {
        CameraManager.SetBaseSpeed(CameraSlider.value);
        CameraValueText.text = valueToText(CameraSlider.value);
    }

    //設定初期化
    public void InitSetting()
    {
        ConfigManager.InitConfig();
        //Sliderの値の設定
        BGMSlider.value = SoundManager.GetBaseVolumeBGM();
        SESlider.value = SoundManager.GetBaseVolumeSE();
        BrightnessSlider.value = 1.0f - BrightnessManager.GetBaseAlfa();
        CameraSlider.value = CameraManager.GetBaseSpeed();

        //Textの設定
        BGMValueText.text = valueToText(BGMSlider.value);
        SEValueText.text = valueToText(SESlider.value);
        BrightnessValueText.text = valueToText(BrightnessSlider.value);
        CameraValueText.text = valueToText(CameraSlider.value);
    }


    //戻る
    public void SelectBack()
    {
        //メインゲーム中なら設定画面のを非表示
        if (MainGameManager.IsMainGaming)
        {
            MainGameManager.ConfigToMainGame();
        }
        //ゲームモード選択画面に戻る
        else
        {
            BaseScreenManager.SetScreen(BaseScreenManager.Screen.GAME_MODE_SELECT);
        }
    }

    //Sliderの値をテキスト用に変換
    string valueToText(float value)
    {
        return (value * 100).ToString("F0");
    }
}
