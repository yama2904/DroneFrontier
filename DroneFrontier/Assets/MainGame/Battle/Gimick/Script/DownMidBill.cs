﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DownMidBill : MonoBehaviour
{
    [SerializeField, Tooltip("1回目の沈下速度")] float firstDownSpeed = 10f;
    [SerializeField, Tooltip("2回目の沈下速度")] float secondDownSpeed = 10f;

    [SerializeField, Tooltip("1回目の沈下開始時間")] float firstDownTime = 60f;
    [SerializeField, Tooltip("2回目の沈下開始時間")] float secondDownTime = 150f;

    [SerializeField, Tooltip("1回目の沈下が終了するY座標")] float firsDownPos = 0;
    [SerializeField, Tooltip("2回目の沈下が終了するY座標")] float secondDownPos = 0;

    [SerializeField] GameObject billObject = null;
    [SerializeField] GameObject particles = null;

    Transform cacheTransform = null;
    bool isFirst = false;
    bool isSecond = false;


    void Start()
    {
        particles.SetActive(false);
        cacheTransform = billObject.transform;
        Invoke(nameof(StartFirsDown), firstDownTime);
        Invoke(nameof(StartSecondDown), secondDownTime);
    }

    void Update()
    {
        if (isFirst)
        {
            cacheTransform.Translate(0, firstDownSpeed * Time.deltaTime * -1, 0);
            if (cacheTransform.localPosition.y < firsDownPos)
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
}