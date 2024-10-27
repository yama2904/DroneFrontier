using System;
using UnityEngine;
using UnityEngine.UI;

public class BarrierWeakStatus : IDroneStatus
{
    public Image IconImage => throw new NotImplementedException();

    public event EventHandler StatusEndEvent;

    public bool Invoke(GameObject drone, float statusSec, params object[] addParams)
    {
        throw new NotImplementedException();
    }
}
