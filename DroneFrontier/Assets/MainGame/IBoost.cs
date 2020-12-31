using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IBoost
{
    float Accele { get; set; }  //ブースト時の加速度

    void UseBoost(float speedMgnf, float time);
}
