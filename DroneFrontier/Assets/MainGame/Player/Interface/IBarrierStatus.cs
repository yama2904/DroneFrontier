using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IBarrierStatus
{
    bool IsStrength { get; }
    bool IsWeak { get; }

    /*
     * バリア強化
     * 引数1: バリアのダメージ軽減率(0～1)
     * 引数2: 強化する時間
     */
    void BarrierStrength(float strengthPercent, float time);
        
    void BarrierWeak();         //バリア弱体化
    void ReleaseBarrierWeak();  //バリア弱体化解除
}
