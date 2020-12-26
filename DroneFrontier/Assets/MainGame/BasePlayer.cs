using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BasePlayer : MonoBehaviour
{
    public float HP { get; protected set; } = 0; //HP
    public float MoveSpeed = 0;                  //移動速度
    public float MaxSpeed { get; set; } = 0;     //最高速度

    protected Rigidbody _Rigidbody = null;
    public Barrier _Barrier { get; protected set; } = null;
    public LockOn _LockOn { get; protected set; } = null;

    //武器
    protected enum Weapon
    {
        MAIN,   //メイン武器
        SUB,    //サブ武器

        NONE
    }
    protected AtackBase[] weapons;  //ウェポン群

    //アイテム
    protected enum ItemNum
    {
        ITEM_1,   //アイテム枠1
        ITEM_2,   //アイテム枠2

        NONE
    }
    protected Item[] items;

    protected virtual void Start() { }
    protected virtual void Update() { }

    protected abstract void Move(float speed, float _maxSpeed, Vector3 direction);  //移動処理
    protected abstract void UseWeapon(Weapon weapon);   //攻撃処理
    protected abstract IEnumerator UseBoost(float speedMgnf, float time);   //ブースト処理
    public abstract void Damage(float power);
}