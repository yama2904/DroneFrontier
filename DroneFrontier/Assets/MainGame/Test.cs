using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    float deltaTime;
    int count;
    
    void Start()
    {
        deltaTime = 0;
        count = 0;
    }
    
    void Update()
    {
        if(deltaTime >= 1.0f)
        {
            Debug.Log(++count + "秒");
            deltaTime = 0;
        }
        //if (Input.GetMouseButtonDown(0))
        //{
        //    Debug.Log("左クリック押");
        //}

        //if (Input.GetMouseButtonUp(0))
        //{
        //    Debug.Log("左クリック離");
        //}


        //if (Input.GetMouseButtonDown(1))
        //{
        //    Debug.Log("右クリック押");
        //}

        //if (Input.GetMouseButtonUp(1))
        //{
        //    Debug.Log("右クリック離");
        //}

        deltaTime += Time.deltaTime;
    }
}
