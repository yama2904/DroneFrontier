using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    [SerializeField] bool isTime = true;
    float deltaTime;
    int count;

    //カメラ用変数
    [SerializeField] GameObject subCamera = null;
    //GameObject mainCamera;
    bool isMainCamera = true;

    [SerializeField] bool isWeaponDebug = false;
    [SerializeField] BasePlayer player;
    [SerializeField] BasePlayer cpu;

    void Start()
    {
        deltaTime = 0;
        count = 0;

        if (isWeaponDebug)
        {
            player.SetWeapon(AtackManager.Weapon.MISSILE);
            cpu.SetWeapon(AtackManager.Weapon.LASER);
        }

        //mainCamera = Camera.main.gameObject;
    }

    void Update()
    {
        if (isTime)
        {
            if (deltaTime >= 1.0f)
            {
                Debug.Log(++count + "秒");
                deltaTime = 0;
            }
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            isMainCamera = !isMainCamera;
            if (isMainCamera)
            {
                Camera.main.depth = 1;
                subCamera.GetComponent<Camera>().depth = 0;
            }
            else
            {
                Camera.main.depth = 0;
                subCamera.GetComponent<Camera>().depth = 1;
            }
        }

        if (!isMainCamera)
        {
            if (Input.GetKey(KeyCode.UpArrow))
            {
                subCamera.transform.Translate(0, 0, 0.01f);
            }
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                subCamera.transform.Translate(-0.01f, 0, 0);
            }
            if (Input.GetKey(KeyCode.DownArrow))
            {
                subCamera.transform.Translate(0, 0, -0.01f);
            }
            if (Input.GetKey(KeyCode.RightArrow))
            {
                subCamera.transform.Translate(0.01f, 0, 0);
            }
            if (Input.GetKey(KeyCode.RightShift))
            {
                subCamera.transform.Translate(0, 0.01f, 0);
            }
            if (Input.GetKey(KeyCode.RightControl))
            {
                subCamera.transform.Translate(0, -0.01f, 0);
            }
        }

        deltaTime += Time.deltaTime;
    }
}
