using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class billdown : MonoBehaviour
{
    //ビルが沈むスピード
    [SerializeField] float speeeeed = 10.0f;
    [SerializeField] float downTime = 30f;
    [SerializeField] float destroyPosY = 0;
    [SerializeField] GameObject billObject = null;
    [SerializeField] GameObject particles = null;

    Transform cacheTransform = null;
    bool isStart = false;
    
    void Start()
    {
        particles.SetActive(false);
        cacheTransform = billObject.transform;
        Invoke(nameof(StartDown), downTime);
    }
    
    void Update()
    {
        if (isStart)
        {
            cacheTransform.Translate(0, speeeeed * Time.deltaTime * -1, 0);
            if(cacheTransform.transform.position.y < destroyPosY)
            {
                Destroy(gameObject);
            }
        }
    }

    void StartDown()
    {
        particles.SetActive(true);
        isStart = true;
    }
}

