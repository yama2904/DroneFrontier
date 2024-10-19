using System;
using UnityEngine;

public class BarrierWeak : MonoBehaviour, IDroneStatus
{
    public RectTransform IconImage => throw new NotImplementedException();

    public event EventHandler StatusEndEvent;

    public bool Invoke(GameObject drone, params object[] parameters)
    {
        throw new NotImplementedException();
    }
}
