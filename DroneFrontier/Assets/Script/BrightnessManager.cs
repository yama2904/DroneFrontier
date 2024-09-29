using UnityEngine;
using UnityEngine.UI;

public class BrightnessManager : MonoBehaviour
{
    //最も暗い画面のアルファ値
    const float MAX_ALFA = 200.0f / 255.0f;

    //黒
    const float RED = 0;
    const float GREEN = 0;
    const float BLUE = 0;

    static Image screenMaskImage = null;
    static float baseAlfa = 0;      //SetBaseAlfaで設定したゲーム全体の画面の明るさ
    static float gameAlfa = 0;      //ゲームの演出の方の画面の明るさ

    static float fadeAlfa = 0;      //フェードイン・フェードアウトの1フレームのアルファ値の変化量
    static float deltaTime = 0;     //static関数で使えるようにTime.deltaTimeを代入する変数
    static bool isFadeIn = false;   //フェードインするか
    static bool isFadeOut = false;  //フェードアウトするか

    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        screenMaskImage = transform.Find("Canvas/Panel").GetComponent<Image>();
        screenMaskImage.color = new Color(RED, GREEN, BLUE, AddAlfa(gameAlfa));
    }

    void Update()
    {
        //フェードイン
        if (isFadeIn)
        {
            gameAlfa -= fadeAlfa;
            screenMaskImage.color = new Color(RED, GREEN, BLUE, AddAlfa(gameAlfa));
            if (AddAlfa(gameAlfa) <= baseAlfa * MAX_ALFA)
            {
                gameAlfa = 0;
                isFadeIn = false;
                fadeAlfa = 0;
            }
        }

        //フェードアウト
        if (isFadeOut)
        {
            gameAlfa += fadeAlfa;
            screenMaskImage.color = new Color(RED, GREEN, BLUE, AddAlfa(gameAlfa));
            if (gameAlfa >= 1.0f)
            {
                gameAlfa = 1.0f;
                isFadeOut = false;
                fadeAlfa = 0;
            }
        }
        deltaTime = Time.deltaTime;
    }

    //ゲーム全体の画面の明るさ
    public static float BaseAlfa
    {
        get { return baseAlfa; }
        set
        {
            float a = value;
            if(a < 0)
            {
                a = 0;
            }
            if(a > 1.0f)
            {
                a = 1.0f;
            }
            baseAlfa = a;
            screenMaskImage.color = new Color(RED, GREEN, BLUE, AddAlfa(gameAlfa));
        }
    }

    //画面の明るさを0～1で設定
    //ゲームの演出での明るさ変更など
    public static void SetGameAlfa(float x)
    {
        if (x < 0)
        {
            x = 0;
        }
        if (x > 1)
        {
            x = 1;
        }
        gameAlfa = x;
        screenMaskImage.color = new Color(RED, GREEN, BLUE, AddAlfa(gameAlfa));
    }

    //SetGameAlfaで設定した明るさを取得
    public static float GetGameAlfa()
    {
        return gameAlfa;
    }

    /*
     SoundManager同様どう頑張っても指定したtimeより数秒長く
     フェード処理が行われてしまいます
     Debu.Logで確認しないと気付かない感じなので多分大丈夫
     */

    //フェードイン(徐々に明るくする)
    //timeは最大の明るさになるまでの時間
    public static void FadeIn(float time)
    {
        if (isFadeOut)
        {
            isFadeOut = false;
        }
        isFadeIn = true;
        float diff = gameAlfa;    //今の明るさと最大の明るさの差
        fadeAlfa = (deltaTime / time) * diff;
    }

    //フェードアウト(徐々に暗くする)
    //timeは真っ暗になるまでの時間
    public static void FadeOut(float time)
    {
        if (isFadeIn)
        {
            isFadeIn = false;
        }
        isFadeOut = true;
        float diff = 1.0f - gameAlfa;    //今の明るさと最小の明るさの差
        fadeAlfa = (deltaTime / time) * diff;
    }

    //フェードイン・フェードアウトを途中で止めて画面の明るさをそのままにする
    public static void FadeStop()
    {
        isFadeIn = false;
        isFadeOut = false;
        fadeAlfa = 0;
    }

    //baseAlfaとgameAlgaを合わせた最終的な画面の明るさを取得
    private static float AddAlfa(float alfa)
    {
        float a = 1.0f - (1.0f - (baseAlfa * MAX_ALFA)) * (1.0f - alfa);
        if (a < 0)
        {
            a = 0;
        }
        if (a > 1.0f)
        {
            a = 1.0f;
        }
        return a;
    }
}
