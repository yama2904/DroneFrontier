using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StunScreenMask : MonoBehaviour
{
    [SerializeField] Image screenMaskImage = null;
    float alfa = 1.0f;

    //画面のマスクが徐々に消える用
    float maxMaskTime = 0;      //画面が真っ白の時間
    float subtractAlfa = 0;     //割り算は重いので先に計算させる用
    bool isStartUpdate = false;

    //マスクする色
    const float RED = 1;     //赤
    const float GREEN = 1;   //緑
    const float BLUE = 1;    //青

    public bool IsStun { get; private set; } = false;

    void Start()
    {
        screenMaskImage.color = new Color(RED, GREEN, BLUE, alfa);
        screenMaskImage.enabled = false;
    }

    void Update()
    {
        if (!isStartUpdate)
        {
            return;
        }

        alfa -= subtractAlfa * Time.deltaTime;
        if (alfa <= 0)
        {
            alfa = 0;
            screenMaskImage.color = new Color(RED, GREEN, BLUE, alfa);

            IsStun = false;
            isStartUpdate = false;
            screenMaskImage.enabled = false;
        }
        screenMaskImage.color = new Color(RED, GREEN, BLUE, alfa);
    }

    void StartUpdate()
    {
        isStartUpdate = true;
    }

    public void SetStun(float time)
    {
        //アルファ値をMAXにして画面を真っ白にする
        alfa = 1.0f;
        screenMaskImage.color = new Color(RED, GREEN, BLUE, alfa);

        IsStun = true;
        isStartUpdate = false;
        screenMaskImage.enabled = true;

        float divideTime = time / 3;
        maxMaskTime = divideTime;
        subtractAlfa = 1 / (divideTime * 2);
        Invoke(nameof(StartUpdate), maxMaskTime);
    }
}
