using System;
using UnityEngine;
using UnityEngine.UI;

public class BarrierWeak : MonoBehaviour, IDroneStatus
{
    public Image IconImage => throw new NotImplementedException();

    public event EventHandler StatusEndEvent;

    public bool Invoke(GameObject drone, params object[] parameters)
    {
        throw new NotImplementedException();
    }
}
