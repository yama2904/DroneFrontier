using System.Collections;
using UnityEngine;

public interface IBarrier
{
    float HP { get; }
    void Damage(float power);    //バリアにダメージを与える
}
