using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IBarrierStatus
{
    /*
     * バリア強化
     * 引数1: バリアのダメージ軽減率(0～1)
     * 引数2: 強化する時間
     */
    void BarrierStrength(float strengthRate, float time);

    /*
     * バリア弱体化 
     * 引数1: 弱体化する時間
     */
    void BarrierWeak(float time);
}
