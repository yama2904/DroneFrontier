using System;
using UnityEngine;
using UnityEngine.UI;

public class BarrierWeakStatus : IDroneStatusChange
{
    public StatusChangeType StatusType => StatusChangeType.BarrierWeak;

    public event EventHandler StatusEndEvent;

    public Image InstantiateIcon()
    {
        throw new NotImplementedException();
    }

    public bool Invoke(GameObject drone, float statusSec, params object[] addParams)
    {
        throw new NotImplementedException();
    }
}
