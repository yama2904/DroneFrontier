using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DownLowBill : MonoBehaviour
{
    [SerializeField, Tooltip("1回目の沈下速度")] float firstDownSpeed = 10f;
    [SerializeField, Tooltip("1回目の沈下開始時間")] float firstDownTime = 60f;
    [SerializeField, Tooltip("1回目の沈下が終了するY座標")] float firsDownPos = 0;

    [SerializeField] GameObject billObject = null;
    [SerializeField] GameObject particles = null;

    Transform cacheTransform = null;
    bool isFirst = false;


    void Start()
    {
        particles.SetActive(false);
        cacheTransform = billObject.transform;
        Invoke(nameof(StartFirsDown), firstDownTime);
    }

    void Update()
    {
        if (isFirst)
        {
            cacheTransform.Translate(0, firstDownSpeed * Time.deltaTime * -1, 0);
            if (cacheTransform.localPosition.y < firsDownPos)
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
}