using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BillSmoke : MonoBehaviour
{
    private ParticleSystem particle;
    int flg = 0;

    void Start()
    {
        particle = this.GetComponent<ParticleSystem>();
        particle.Stop();
    }

    void Update()
    {
        if (Time.time > 10 & flg == 0)
        {
            flg = 1;
            particle.Play(); //パーティクルの再生
        }

        if (Time.time > 20 & flg == 1)
        {
            flg = 0;
            particle.Stop(); //パーティクルの停止
        }

    }
}
