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

    [SerializeField] BasePlayer player;
    [SerializeField] Transform playerPos;
    [SerializeField] BasePlayer cpu;
    [SerializeField] Transform cpuPos;
    [SerializeField] bool isPlayerDebug = false;
    [SerializeField] BaseWeapon.Weapon playerWeapon = BaseWeapon.Weapon.SHOTGUN;
    [SerializeField] BaseWeapon.Weapon cpuWeapon = BaseWeapon.Weapon.SHOTGUN;

    void Awake()
    {
        deltaTime = 0;
        count = 0;

        if (isPlayerDebug)
        {
            BasePlayer p = Instantiate(player);
            p.transform.parent = null;
            p.transform.position = playerPos.position;
            p.transform.rotation = playerPos.rotation;
            p.SetWeapon(playerWeapon);

            BasePlayer c = Instantiate(cpu);
            c.transform.parent = null;
            c.transform.position = cpuPos.position;
            c.transform.rotation = cpuPos.rotation;
            c.SetWeapon(cpuWeapon);
        }

        //mainCamera = Camera.main.gameObject;
    }

    void Update()
    {
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
