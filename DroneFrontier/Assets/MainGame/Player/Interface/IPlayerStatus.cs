using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPlayerStatus
{
    bool SetBarrierStrength(float strengthPercent, float time);
    void SetBarrierWeak();    //バリア弱体化
    void UnSetBarrierWeak();  //バリア弱体化解除
    void SetJamming();        //ジャミング
    void UnSetJamming();      //ジャミング解除
    void SetStun(float time); //スタン
    void SetSpeedDown(float downPercent);  //速度を低下させる
    void UnSetSpeedDown();    //速度低下を解除
}
