using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StunScreenMask : MonoBehaviour
{
    public float StunTime { private get; set; } = 5.0f;
    [SerializeField] Image screenMaskImage = null;
    [SerializeField] float maxMaskTime = 3.0f;      //画面が真っ白の時間
    [SerializeField] float attenuMaskTime = 6.0f;   //減衰する時間

    float alfa = 1.0f;
    float subtractAlfa = 0; //割り算は重いので先に計算させる用
    bool isEndCoroutine = false;

    //マスクする色
    const float RED = 1;     //赤
    const float GREEN = 1;   //緑
    const float BLUE = 1;    //青
    const float ALFA = 1;    //アルファ

    void Start()
    {
        screenMaskImage.enabled = true;
        screenMaskImage.color = new Color(
            RED, GREEN, BLUE, ALFA);
        subtractAlfa = 1 / attenuMaskTime;
        isEndCoroutine = false;

        StartCoroutine(StunEffect());
    }
    
    void Update()
    {
        if (isEndCoroutine)
        {
            Destroy(gameObject);
        }
    }

    IEnumerator StunEffect()
    {
        yield return new WaitForSeconds(maxMaskTime);
        while (true)
        {
            alfa -= subtractAlfa * Time.deltaTime;
            Debug.Log(alfa);
            if(alfa <= 0)
            {
                alfa = 0;
                isEndCoroutine = true;
                yield break;
            }

            screenMaskImage.color = new Color(
            RED, GREEN, BLUE, alfa);

            yield return null;
        }
    }
}
