using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cube : MonoBehaviour
{
    [SerializeField] float speed = 0.03f;
    float deltaTime = 1;

    void Start()
    {

    }

    void Update()
    {
        transform.position += new Vector3(speed * Mathf.Sin(deltaTime), 0, 0);
        deltaTime += Time.deltaTime;
    }
}
