using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPlayerStatus
{
    /*
     * バリア強化
     * 戻り値: 成功したらtrue
     * 引数1: ダメージ軽減率(0～1)
     * 引数1: 強化する時間(秒数)
     */
    bool SetBarrierStrength(float strengthPercent, float time);

    void SetBarrierWeak();    //バリア弱体化
    void UnSetBarrierWeak();  //バリア弱体化解除
    void SetJamming();        //ジャミング
    void UnSetJamming();      //ジャミング解除
    void SetStun(float time); //スタン
    void SetSpeedDown(float downPercent);  //速度を低下させる
    void UnSetSpeedDown();    //速度低下を解除
}
