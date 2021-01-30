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

    [Header("触れるでない")]
    [SerializeField] GameObject billObject = null;
    [SerializeField] GameObject particles = null;

    Transform cacheTransform = null;
    bool isFirst = false;
    bool isSecond = false;
    bool isThird = false;


    void Start()
    {
        particles.SetActive(false);
        cacheTransform = billObject.transform;

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
            cacheTransform.Translate(0, firstDownSpeed * Time.deltaTime * -1, 0);
            if (cacheTransform.localPosition.y < firstDownPos)
            {
                isFirst = false;
                if (!isSecond)
                {
                    particles.SetActive(false);
                }
            }
        }
        else if (isSecond)
        {
            cacheTransform.Translate(0, secondDownSpeed * Time.deltaTime * -1, 0);
            if (cacheTransform.localPosition.y < secondDownPos)
            {
                isSecond = false;
                if (!isThird)
                {
                    particles.SetActive(false);
                }
            }
        }
        else if (isThird)
        {
            cacheTransform.Translate(0, thirdDownSpeed * Time.deltaTime * -1, 0);
            if (cacheTransform.localPosition.y < thirdDownPos)
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
