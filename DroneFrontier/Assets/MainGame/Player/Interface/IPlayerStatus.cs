using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPlayerStatus
{
    bool SetBarrierStrength(float strengthValue, float time);
    void SetBarrierWeak(float time);
    void SetJamming();
    void UnSetJamming();
    void SetStun(float time);
}
