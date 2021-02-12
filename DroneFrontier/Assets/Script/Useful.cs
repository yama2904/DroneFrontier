using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Useful : MonoBehaviour
{
    //小数点切り捨て
    //引数1を引数2未満の小数点を切り捨てる
    public static float DecimalPointTruncation(float value, int num)
    {
        if(num == 0)
        {
            return Mathf.Floor(value);
        }

        float x = Mathf.Pow(10, num);
        value *= x;
        value = Mathf.Floor(value) / x;

        return value;
    }
}
