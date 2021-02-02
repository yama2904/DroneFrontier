using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DownBill : MonoBehaviour
{
    enum DownNum
    {
        ONE,
        TWO,
        THREE,

        NONE
    }
    [SerializeField, Tooltip("沈下する回数")] DownNum downNum = DownNum.NONE;

    [Header("1回目")]
    [SerializeField, Tooltip("沈下速度")] float firstDownSpeed = 10f;
    [SerializeField, Tooltip("沈下開始時間")] float firstDownTime = 60f;
    [SerializeField, Tooltip("沈下が終了するY座標")] float firstDownPos = -14f;

    [Header("2回目")]
    [SerializeField, Tooltip("沈下速度")] float secondDownSpeed = 10f;
    [SerializeField, Tooltip("沈下開始時間")] float secondDownTime = 150f;
    [SerializeField, Tooltip("沈下が終了するY座標")] float secondDownPos = -24f;

    [Header("3回目")]
    [SerializeField, Tooltip("沈下速度")] float thirdDownSpeed = 10f;
    [SerializeField, Tooltip("沈下開始時間")] float thirdDownTime = 240f;
    [SerializeField, Tooltip("沈下が終了するY座標")] float thirdDownPos = -45f;

    [Header("ついでに他一緒に沈下させたい奴")]
    [SerializeField] Transform downObject1 = null;
    [SerializeField] Transform downObject2 = null;
    [SerializeField] Transform downObject3 = null;
    [SerializeField] Transform downObject4 = null;
    [SerializeField] Transform downObject5 = null;

    [Header("触れるでない")]
    [SerializeField] Transform billObject = null;
    [SerializeField] GameObject particles = null;
    
    bool isFirst = false;
    bool isSecond = false;
    bool isThird = false;


    void Start()
    {
        particles.SetActive(false);

        if (downNum == DownNum.ONE)
        {
            Invoke(nameof(StartFirsDown), firstDownTime);
        }
        if (downNum == DownNum.TWO)
        {
            Invoke(nameof(StartFirsDown), firstDownTime);
            Invoke(nameof(StartSecondDown), secondDownTime);
        }
        if (downNum == DownNum.THREE)
        {
            Invoke(nameof(StartFirsDown), firstDownTime);
            Invoke(nameof(StartSecondDown), secondDownTime);
            Invoke(nameof(StartThirdDown), thirdDownTime);
        }
    }
    
    void Update()
    {
        if (isFirst)
        {
            //オブジェクトの沈下
            billObject.Translate(0, firstDownSpeed * Time.deltaTime * -1, 0);
            if(downObject1 != null)
            {
                downObject1.Translate(0, firstDownSpeed * Time.deltaTime * -1, 0);
            }
            if (downObject2 != null)
            {
                downObject2.Translate(0, firstDownSpeed * Time.deltaTime * -1, 0);
            }
            if(downObject3 != null)
            {
                downObject3.Translate(0, firstDownSpeed * Time.deltaTime * -1, 0);
            }
            if(downObject4 != null)
            {
                downObject4.Translate(0, firstDownSpeed * Time.deltaTime * -1, 0);
            }
            if(downObject5 != null)
            {
                downObject5.Translate(0, firstDownSpeed * Time.deltaTime * -1, 0);
            }

            //沈下停止ラインの判定
            if (billObject.localPosition.y < firstDownPos)
            {
                if(downNum == DownNum.ONE)
                {
                    Destroy(gameObject);
                }
                isFirst = false;
                if (!isSecond)
                {
                    particles.SetActive(false);
                }
            }
        }
        else if (isSecond)
        {
            //オブジェクトの沈下
            billObject.Translate(0, secondDownSpeed * Time.deltaTime * -1, 0);
            if (downObject1 != null)
            {
                downObject1.Translate(0, secondDownSpeed * Time.deltaTime * -1, 0);
            }
            if (downObject2 != null)
            {
                downObject2.Translate(0, secondDownSpeed * Time.deltaTime * -1, 0);
            }
            if (downObject3 != null)
            {
                downObject3.Translate(0, secondDownSpeed * Time.deltaTime * -1, 0);
            }
            if (downObject4 != null)
            {
                downObject4.Translate(0, secondDownSpeed * Time.deltaTime * -1, 0);
            }
            if (downObject5 != null)
            {
                downObject5.Translate(0, secondDownSpeed * Time.deltaTime * -1, 0);
            }

            //沈下停止ラインの判定
            if (billObject.localPosition.y < secondDownPos)
            {
                if (downNum == DownNum.TWO)
                {
                    Destroy(gameObject);
                }
                isSecond = false;
                if (!isThird)
                {
                    particles.SetActive(false);
                }
            }
        }
        else if (isThird)
        {
            //オブジェクトの沈下
            billObject.Translate(0, thirdDownSpeed * Time.deltaTime * -1, 0);
            if (downObject1 != null)
            {
                downObject1.Translate(0, thirdDownSpeed * Time.deltaTime * -1, 0);
            }
            if (downObject2 != null)
            {
                downObject2.Translate(0, thirdDownSpeed * Time.deltaTime * -1, 0);
            }
            if (downObject3 != null)
            {
                downObject3.Translate(0, thirdDownSpeed * Time.deltaTime * -1, 0);
            }
            if (downObject4 != null)
            {
                downObject4.Translate(0, thirdDownSpeed * Time.deltaTime * -1, 0);
            }
            if (downObject5 != null)
            {
                downObject5.Translate(0, thirdDownSpeed * Time.deltaTime * -1, 0);
            }

            //沈下停止ラインの判定
            if (billObject.localPosition.y < thirdDownPos)
            {
                Destroy(gameObject);
            }
        }
    }

    void StartFirsDown()
    {
        particles.SetActive(true);
        isFirst = true;
    }

    void StartSecondDown()
    {
        particles.SetActive(true);
        isSecond = true;
    }

    void StartThirdDown()
    {
        particles.SetActive(true);
        isThird = true;
    }
}
