using System.Collections;
using UnityEngine;

public interface ILockOn
{
    GameObject Target { get; }   //ロックオン中のターゲット

    void StartLockOn(float speed);  //0～1のスピードでロックオン
    void ReleaseLockOn();
}
