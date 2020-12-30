using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CPUController : BasePlayer
{
    public const string CPU_TAG = "CPU";    //タグ名
    [SerializeField] Barrier barrier = null;    //バリア
    [SerializeField] LockOn lockOn = null;      //ロックオン
    Transform cacheTransform = null;

    //デバッグ用
    [SerializeField] float speed = 0.1f;
    [SerializeField] bool isAtack = true;
    [SerializeField] bool isMove = true;
    float deltaTime = 1;

    protected override void Start()
    {
        cacheTransform = transform;
        _Rigidbody = GetComponent<Rigidbody>();
        _Barrier = barrier;
        _LockOn = lockOn;

        HP = 30;
        MoveSpeed = speed;
        MaxSpeed = 30.0f;
    }

    protected override void Update()
    {
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

    protected override void Move(float speed, float _maxSpeed, Vector3 direction)
    {

    }

    protected override IEnumerator UseBoost(float speedMgnf, float time)
    {
        MoveSpeed *= speedMgnf;
        MaxSpeed *= speedMgnf;

        //time秒後に速度を戻す
        yield return new WaitForSeconds(time);
        MoveSpeed /= speedMgnf;
        MaxSpeed /= speedMgnf;
    }
}
