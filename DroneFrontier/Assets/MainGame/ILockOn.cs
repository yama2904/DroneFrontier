using System.Collections;
using UnityEngine;

public interface ILockOn
{
    GameObject Target { get; set; }   //ロックオン中のターゲット  
    float SearchRadius { get; set; }  //ロックオン範囲
    float TrackingSpeed { get; set; } //ロックオンした際にカメラを敵に向けるスピード

    void StartLockOn();
    void ReleaseLockOn();
}
