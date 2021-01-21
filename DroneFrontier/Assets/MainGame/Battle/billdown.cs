using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class billdown : MonoBehaviour
{
    //Vector3 target = new Vector3(0,100,0);

    //ビルが沈むスピード
    public float speeeeed = 10.0f;
    
     // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("経過時間(秒)" + Time.time);

        //ビルが動き始めるタイミング（秒）
        if(Time.time > 10)
        {

            GameObject[] bills = GameObject.FindGameObjectsWithTag("bill");

            foreach (GameObject bill in bills)
            {
                //ビルが沈む動き
                bill.transform.position -= transform.up * speeeeed * Time.deltaTime;

                //ビルの動きが止まるタイミング（秒）
                if (Time.time > 20)
                {
                    break;
                }
            }
        }
        if (Time.time > 30)
        {

            GameObject[] bills = GameObject.FindGameObjectsWithTag("bill");

            foreach (GameObject bill in bills)
            {
                bill.transform.position -= transform.up * speeeeed * Time.deltaTime;
                if (Time.time > 40)
                {
                    break;
                }
            }
        }

    }
}

