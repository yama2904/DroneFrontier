using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosion : MonoBehaviour
{
    [SerializeField] float size = 10;

    void Start()
    {
        foreach(Transform child in transform)
        {
            Vector3 scale = child.localScale;
            child.localScale = scale * size;
        }
    }
    
    void Update()
    {
        
    }
}
