using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseDrone : MonoBehaviour
{
    public uint PlayerID { get; private set; } = 0;
    static uint count = 0;

    //生成時間
    public float StartTime { get; private set; } = 0;

    protected virtual void Awake()
    {
        PlayerID = count++;
    }

    protected virtual void Start()
    {
        StartTime = Time.time;
    }
}
