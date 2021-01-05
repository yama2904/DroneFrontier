using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IWeapon
{
    void SetChild(Transform parent);
    void Shot(GameObject target = null);
}
