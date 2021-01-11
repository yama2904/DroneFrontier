using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JammingBot : MonoBehaviour
{
    public const string JAMMING_BOT_TAG = "JammingBot";
    public GameObject Creater { get; set; } = null;
    [SerializeField] float HP = 30.0f;

    void Start()
    {
        //生成した自分のジャミングボットをプレイヤーがロックオンしないように設定
        if(Creater.CompareTag(Player.PLAYER_TAG) || Creater.CompareTag(CPUController.CPU_TAG))
        {
            Creater.GetComponent<BasePlayer>().SetNotLockOnObject(gameObject);
        }
    }

    void Update()
    {        
    }

    private void OnDestroy()
    {
        //デバッグ用
        Debug.Log("ジャミングボット破壊");


        //SetNotLockOnObjectを解除
        if (Creater.CompareTag(Player.PLAYER_TAG) || Creater.CompareTag(CPUController.CPU_TAG))
        {
            Creater.GetComponent<BasePlayer>().UnSetNotLockOnObject(gameObject);
        }
    }

    //public void DestroyBot()
    //{
    //    //デバッグ用
    //    Debug.Log("ジャミングボット破壊");


    //    //SetNotLockOnObjectを解除
    //    if (Creater.CompareTag(Player.PLAYER_TAG) || Creater.CompareTag(CPUController.CPU_TAG))
    //    {
    //        Creater.GetComponent<BasePlayer>().UnSetNotLockOnObject(gameObject);
    //    }

    //    Destroy(gameObject);
    //}

    public void Damage(float power)
    {
        float p = Useful.DecimalPointTruncation(power, 1);   //小数点第2以下切り捨て
        HP -= p;
        if (HP < 0)
        {
            HP = 0;
            Destroy(gameObject);
        }
    }
}
