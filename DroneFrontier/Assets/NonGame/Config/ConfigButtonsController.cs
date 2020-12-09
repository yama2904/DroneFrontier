using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConfigButtonsController : MonoBehaviour
{
    const string SLIDER_NAME = "Slider";
    const string VALUE_DISPLAY_TEXT = "ValueDisplay/Text";

    [SerializeField] GameObject BGMConfigs = null;
    [SerializeField] GameObject SEConfigs = null;
    [SerializeField] GameObject BrightnessConfigs = null;
    [SerializeField] GameObject CameraConfigs = null;

    enum Type
    {
        BGM,
        SE,
        BRIGHTNESS,
        CAMERA,

        NONE
    }
    Slider[] sliders;
    Text[] valueDisplays;

    void Start()
    {
        //Sliderの初期設定
        sliders = new Slider[(int)Type.NONE];
        sliders[(int)Type.BGM] = BGMConfigs.transform.Find(SLIDER_NAME).GetComponent<Slider>();
        sliders[(int)Type.SE] = SEConfigs.transform.Find(SLIDER_NAME).GetComponent<Slider>();
        sliders[(int)Type.BRIGHTNESS] = BrightnessConfigs.transform.Find(SLIDER_NAME).GetComponent<Slider>();
        sliders[(int)Type.CAMERA] = CameraConfigs.transform.Find(SLIDER_NAME).GetComponent<Slider>();

        //ValueDisplayの初期設定
        valueDisplays = new Text[(int)Type.NONE];
        valueDisplays[(int)Type.BGM] = BGMConfigs.transform.Find(VALUE_DISPLAY_TEXT).GetComponent<Text>();
        valueDisplays[(int)Type.SE] = SEConfigs.transform.Find(VALUE_DISPLAY_TEXT).GetComponent<Text>();
        valueDisplays[(int)Type.BRIGHTNESS] = BrightnessConfigs.transform.Find(VALUE_DISPLAY_TEXT).GetComponent<Text>();
        valueDisplays[(int)Type.CAMERA] = CameraConfigs.transform.Find(VALUE_DISPLAY_TEXT).GetComponent<Text>();

        sliders[(int)Type.BGM].value = SoundManager.GetBaseVolumeBGM();
        sliders[(int)Type.SE].value = SoundManager.GetBaseVolumeSE();
        sliders[(int)Type.BRIGHTNESS].value = 1.0f - BrightnessManager.GetBaseAlfa();
        sliders[(int)Type.CAMERA].value = CameraManager.GetBaseSpeed();
    }

    //BGM調整
    public void MoveSliderBGM()
    {
        SoundManager.SetBaseVolumeBGM(sliders[(int)Type.BGM].value);
        valueDisplays[(int)Type.BGM].text = (sliders[(int)Type.BGM].value * 100).ToString("F0");
    }

    //SE調整
    public void MoveSliderSE()
    {
        SoundManager.SetBaseVolumeSE(sliders[(int)Type.SE].value);
        valueDisplays[(int)Type.SE].text = (sliders[(int)Type.SE].value * 100).ToString("F0");
    }

    //明るさ調整
    public void MoveSliderBrightness()
    {
        BrightnessManager.SetBaseAlfa(1.0f - sliders[(int)Type.BRIGHTNESS].value);
        valueDisplays[(int)Type.BRIGHTNESS].text = (sliders[(int)Type.BRIGHTNESS].value * 100).ToString("F0");
    }

    //カメラ感度調整
    public void MoveSliderCamera()
    {
        CameraManager.SetBaseSpeed(sliders[(int)Type.CAMERA].value);
        valueDisplays[(int)Type.CAMERA].text = (sliders[(int)Type.CAMERA].value * 100).ToString("F0");
    }

    //設定初期化
    public void InitSetting()
    {
        BaseScreenManager.InitConfig();
        sliders[(int)Type.BGM].value = SoundManager.GetBaseVolumeBGM();
        sliders[(int)Type.SE].value = SoundManager.GetBaseVolumeSE();
        sliders[(int)Type.BRIGHTNESS].value = 1.0f - BrightnessManager.GetBaseAlfa();
        sliders[(int)Type.CAMERA].value = CameraManager.GetBaseSpeed();
    }


    //戻る
    public void SelectBack()
    {
        BaseScreenManager.SetNextScreen(BaseScreenManager.Screen.GAME_MODE_SELECT);
    }

    //小数点切り捨て
    //引数1を引数2未満の小数点を切り捨てる
    float DecimalPointTruncation(float value, int num)
    {
        float x = Mathf.Pow(10, num);
        value *= x;
        value = Mathf.Floor(value) / x;

        return value;
    }
}
