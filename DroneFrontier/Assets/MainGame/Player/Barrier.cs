using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Barrier : MonoBehaviour
{
    const float MAX_HP = 100;
    public float HP { get; set; } = MAX_HP;

    //バリアの回復用変数
    [SerializeField] float regeneTime = 8.0f;   //バリアが回復しだす時間
    [SerializeField] float regeneValue = 5.0f;  //バリアが毎秒回復する量
    [SerializeField] float repairBarrierTime = 15.0f;   //バリアが破壊されてから修復される時間
    float deltaTime;    //計測用
    bool isRegene;      //回復中か

    float reduction;    //軽減率

    void Start()
    {
        HP = MAX_HP;
        reduction = 1;
        deltaTime = 0;
        isRegene = true;    //ゲーム開始時はHPMAXで回復の必要がないのでtrue
    }

    void Update()
    {
        if(HP <= 0)
        {
            if(deltaTime >= repairBarrierTime)
            {
                Debug.Log("バリア修復");


                HP = 5;
                isRegene = true;
                StartCoroutine(Regene(regeneValue));
            }
        }
        else if (!isRegene)
        {
            if (deltaTime >= regeneTime)
            {
                isRegene = true;
                StartCoroutine(Regene(regeneValue));
            }
        }
        deltaTime += Time.deltaTime;
    }

    //毎秒value値HPを回復する
    IEnumerator Regene(float value)
    {
        while (true)
        {
            //攻撃を受けたら処理をやめる
            if (!isRegene)
            {
                yield break;
            }

            HP += value;
            if(HP >= MAX_HP)
            {
                HP = MAX_HP;
                Debug.Log("バリアHPMAX: " + HP);
                yield break;
            }
            Debug.Log("リジェネ後バリアHP: " + HP);

            yield return new WaitForSeconds(1.0f);
        }
    }

    //バリアに引数分のダメージを与える
    public void Damage(float power)
    {
        float p = Useful.DecimalPointTruncation(power * reduction, 1);  //小数点第2以下切り捨て
        HP -= p;
        if (HP < 0)
        {
            HP = 0;
        }
        deltaTime = 0;
        isRegene = false;


        Debug.Log("バリアに" + p + "のダメージ\n残りHP: " + HP);
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
