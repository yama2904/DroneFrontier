using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Barrier : MonoBehaviour
{
    public float HP { get; private set; } = 100;

    void Start()
    {
        
    }
    
    void Update()
    {
        
    }

    //バリアに引数分のダメージを与える
    public void Damage(float power)
    {
        HP -= power;
        if(HP < 0)
        {
            HP = 0;
        }

        Debug.Log("バリアに" + power + "のダメージ\n残りHP: " + HP);
    }
}
