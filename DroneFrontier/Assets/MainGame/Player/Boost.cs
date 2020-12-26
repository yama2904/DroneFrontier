using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boost : MonoBehaviour
{
    BasePlayer player;


    float initSpeed;    
    float modifyTime;   //変更する時間
    float deltaTime;    //計測用

    void Start()
    {
        player = null;
        initSpeed = 0;

        modifyTime = 0;
        deltaTime = 0;
    }
    
    void Update()
    {
        
    }

    /*
     * スピードを変える
     * 引数1: スピードを変えるプレイヤー・CPU
     * 引数2: 変更する倍率
     * 引数3: 変更する時間(秒数)
     */
    public void ModifySpeed(BasePlayer player, float speedMgnf, float time = -1)
    {

    }
}
