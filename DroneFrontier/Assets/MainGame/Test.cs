using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    [SerializeField] bool isTime = true;
    float deltaTime;
    int count;

    private void Awake()
    {
    }

    void Start()
    {
        deltaTime = 0;
        count = 0;
    }

    void Update()
    {
    }

    private void FixedUpdate()
    {
        if (isTime)
        {
            if (deltaTime >= 1.0f)
            {
                Debug.Log(++count + "秒");
                deltaTime = 0;
            }
        }
        deltaTime += Time.deltaTime;
    }
}
