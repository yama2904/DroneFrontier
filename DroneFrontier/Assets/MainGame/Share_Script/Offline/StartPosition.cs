using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartPosition : MonoBehaviour
{
    //シングルトン
    public static StartPosition Singleton { get; private set; }

    Transform[] starts;
    int count = 0;

    private void Awake()
    {
        Singleton = this;

        starts = new Transform[transform.childCount];
        for(int i = 0; i < transform.childCount; i++)
        {
            starts[i] = transform.GetChild(i);
        }
    }

    public Transform GetPos()
    {
        Transform t = starts[count++];
        if(count >= starts.Length)
        {
            count = 0;
        }

        return t;
    }
}
