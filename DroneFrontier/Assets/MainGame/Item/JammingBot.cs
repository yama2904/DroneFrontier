using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JammingBot : MonoBehaviour
{
    public const string JAMMING_BOT_TAG = "JammingBot";
    [SerializeField] float HP = 30.0f;

    void Start()
    {
        
    }
    
    void Update()
    {
        
    }

    public void DestroyBot()
    {
        //デバッグ用
        Debug.Log("ジャミングボット破壊");


        Destroy(gameObject);
    }

    public void Damage(float power)
    {
        float p = Useful.DecimalPointTruncation(power, 1);   //小数点第2以下切り捨て
        HP -= p;
        if (HP < 0)
        {
            HP = 0;
            DestroyBot();
        }
    }
}
