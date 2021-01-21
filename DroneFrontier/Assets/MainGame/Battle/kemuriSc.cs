using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class kemuriSc : MonoBehaviour
{
    private ParticleSystem particle;
    int flg = 0;

    // Use this for initialization
    void Start()
    {
        particle = this.GetComponent<ParticleSystem>();
        particle.Stop();
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time > 10 & flg == 0)
        {
            flg = 1;
            //Debug.Log("うにｔ");
            particle.Play(); //パーティクルの再生
        }

        if (Time.time > 20 & flg == 1)
        {
            flg = 0;
            particle.Stop(); //パーティクルの停止
        }

    }
}
