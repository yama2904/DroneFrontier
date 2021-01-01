using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CPUController : BasePlayer
{
    public const string CPU_TAG = "CPU";    //タグ名

    //デバッグ用
    [SerializeField] float speed = 0.1f;
    [SerializeField] bool isAtack = true;
    [SerializeField] bool isMove = true;
    float deltaTime = 1;

    protected override void Start()
    {
        HP = 30;
        MoveSpeed = speed;
        MaxSpeed = 30.0f;
    }

    protected override void Update()
    {
        base.Update();

        if (isAtack)
        {
            UseWeapon(Weapon.SUB);
        }

        //デバッグ用
        if (isMove)
        {
            transform.position += new Vector3(MoveSpeed * Mathf.Sin(deltaTime), 0, 0);
        }
        deltaTime += Time.deltaTime;
    }
}
