using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StunScreenMask : MonoBehaviour
{
    [SerializeField] Image screenMaskImage = null;
    float alfa = 1.0f;

    //画面のマスクが徐々に消える用
    float maxMaskTime = 0;      //画面が真っ白の時間
    float removeMaskTime = 0;   //画面のマスクが消える時間
    float subtractAlfa = 0; //割り算は重いので先に計算させる用
    bool isEndCoroutine = false;

    //マスクする色
    const float RED = 1;     //赤
    const float GREEN = 1;   //緑
    const float BLUE = 1;    //青

    void Start()
    {
        screenMaskImage.enabled = true;
        screenMaskImage.color = new Color(
            RED, GREEN, BLUE, alfa);
        subtractAlfa = 1 / removeMaskTime;
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

    public static void CreateStunMask(float time)
    {
        GameObject o = Instantiate(Resources.Load("Item/StunScreenMask")) as GameObject;
        float divideTime = time / 3;
        StunScreenMask s = o.GetComponent<StunScreenMask>();
        s.maxMaskTime = divideTime;
        s.removeMaskTime = divideTime * 2;
    }
}
