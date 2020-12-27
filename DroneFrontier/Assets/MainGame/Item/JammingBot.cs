using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JammingBot : MonoBehaviour
{
    public const string JAMMING_BOT_TAG = "JammingBot";
    public BasePlayer CreatedPlayer { get; set; } = null;
    [SerializeField] float HP = 30.0f;

    void Start()
    {
        //生成した自分のジャミングボットをプレイヤーがロックオンしないように設定
        CreatedPlayer._LockOn.AddNotLockOnObject(gameObject);
    }

    void Update()
    {
        
    }

    public void DestroyBot()
    {
        //デバッグ用
        Debug.Log("ジャミングボット破壊");


        CreatedPlayer._LockOn.RemoveNotLockOnObject(gameObject);
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
