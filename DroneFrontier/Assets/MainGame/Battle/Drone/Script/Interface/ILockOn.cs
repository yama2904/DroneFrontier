using System.Collections;
using UnityEngine;

public interface ILockOn
{
    GameObject Target { get; }   //ロックオン中のターゲット

    void StartLockOn(float speed);  //0～1のスピードでロックオン
    void ReleaseLockOn();           //ロックオン解除
    void SetNotLockOnObject(GameObject o);    //ロックオンしないオブジェクトを設定
    void UnSetNotLockOnObject(GameObject o);  //SetNotLockOnObjectで設定したオブジェクトをロックオンするように設定
}
