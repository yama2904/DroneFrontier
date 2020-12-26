using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Barrier : MonoBehaviour
{
    public float HP { get; private set; } = 100;
    float reduction;    //軽減率

    void Start()
    {
        reduction = 1;
    }
    
    void Update()
    {
        
    }

    //バリアに引数分のダメージを与える
    public void Damage(float power)
    {
        HP -= power * reduction;
        if(HP < 0)
        {
            HP = 0;
        }

        Debug.Log("バリアに" + power * reduction + "のダメージ\n残りHP: " + HP);
    }

    /*
     * バリアの受けるダメージを軽減する
     * 引数1: 軽減する割合(0～1)
     * 引数2: 軽減する時間(秒数)
     */
    public void BarrierStrength(float strengthRate, float time)
    {
        reduction = 1 - strengthRate;
        StartCoroutine(EndStrength(time));

        //デバッグ用
        Debug.Log("バリア強化");
    }

    //time秒後にバリア強化を終了させる
    IEnumerator EndStrength(float time)
    {
        yield return new WaitForSeconds(time);
        reduction = 1;


        //デバッグ用
        Debug.Log("バリア強化解除");
    }
}
