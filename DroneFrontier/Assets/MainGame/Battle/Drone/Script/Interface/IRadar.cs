using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IRadar
{
    void StartRadar();
    void ReleaseRadar();
    void SetNotRadarObject(GameObject o);
    void UnSetNotRadarObject(GameObject o);
}
