using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AtackBase : MonoBehaviour
{
    protected abstract void Start();
    protected abstract void Update();

    public abstract void Shot(Transform transform);
}
