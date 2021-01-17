using System.Collections;
using UnityEngine;

public interface IBarrier
{
    float HP { get; }
    void CmdDamage(float power);    //バリアにダメージを与える
}
