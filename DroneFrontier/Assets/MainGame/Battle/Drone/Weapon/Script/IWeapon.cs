﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IWeapon
{
    void Shot(GameObject target = null);
}