using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    [SerializeField] bool isTime = true;
    float deltaTime;
    int count;

    [SerializeField] bool playBGM = true;

    private void Awake()
    {
    }

    void Start()
    {
        deltaTime = 0;
        count = 0;

        if (playBGM)
        {
            SoundManager.Play(SoundManager.BGM.DRONE_UP, 1.0f);
        }
    }

    void Update()
    {
        if (playBGM)
        {
            if (SoundManager.IsPlayingBGM == SoundManager.BGM.NONE)
            {
                SoundManager.Play(SoundManager.BGM.DRONE_UP, 1.0f);
            }
        }
        else
        {
            if (SoundManager.IsPlayingBGM != SoundManager.BGM.NONE)
            {
                SoundManager.StopBGM();
            }
        }
    }

    private void FixedUpdate()
    {
        if (isTime)
        {
            if (deltaTime >= 1.0f)
            {
                Debug.Log(++count + "秒");
                deltaTime = 0;
            }
        }
        deltaTime += Time.deltaTime;
    }
}
