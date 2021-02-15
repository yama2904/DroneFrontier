using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConfigScreenManager : MonoBehaviour
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
        BGMSlider.value = SoundManager.BaseBGMVolume;
        SESlider.value = SoundManager.BaseSEVolume;
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
        SoundManager.BaseBGMVolume = BGMSlider.value;
        BGMValueText.text = valueToText(BGMSlider.value);
    }

    //SE調整
    public void MoveSliderSE()
    {
        SoundManager.BaseSEVolume = SESlider.value;
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
    public void InitSetting()
    {
        //初期化
        SoundManager.BaseBGMVolume = 1.0f;
        SoundManager.BaseSEVolume = 1.0f;
        BrightnessManager.BaseAlfa = 0;
        CameraManager.BaseSpeed = 1.0f;

        //SE再生
        SoundManager.Play(SoundManager.SE.SELECT, SoundManager.BaseSEVolume);

        //Sliderの値の設定
        BGMSlider.value = SoundManager.BaseBGMVolume;
        SESlider.value = SoundManager.BaseSEVolume;
        BrightnessSlider.value = 1.0f - BrightnessManager.BaseAlfa;
        CameraSlider.value = CameraManager.BaseSpeed;

        //Textの設定
        BGMValueText.text = valueToText(BGMSlider.value);
        SEValueText.text = valueToText(SESlider.value);
        BrightnessValueText.text = valueToText(BrightnessSlider.value);
        CameraValueText.text = valueToText(CameraSlider.value);
    }


    //戻る
    public void SelectBack()
    {
        //SE再生
        SoundManager.Play(SoundManager.SE.CANCEL, SoundManager.BaseSEVolume);

        //メインゲーム中なら設定画面のを非表示
        if (Online.MainGameManager.IsMainGaming)
        {
            Online.MainGameManager.Singleton.ConfigToMainGame();
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
