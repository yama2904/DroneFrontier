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

    public float AtackingDecreaseSpeed { protected get; set; } = 0.5f;   //攻撃中の移動速度の低下率

    //武器
    protected enum Weapon
    {
        MAIN,   //メイン武器
        SUB,    //サブ武器

        NONE
    }
    protected BaseAtack[] weapons;  //ウェポン群

    //アイテム
    protected enum ItemNum
    {
        ITEM_1,   //アイテム枠1
        ITEM_2,   //アイテム枠2

        NONE
    }
    protected Item[] items;

    protected virtual void Awake()
    {
        //武器の初期化
        weapons = new BaseAtack[(int)Weapon.NONE];

        //メインウェポンの処理
        AtackManager.CreateAtack(out GameObject main, AtackManager.Weapon.GATLING);    //Gatlingの生成
        Transform mainTransform = main.transform;   //キャッシュ
        mainTransform.SetParent(transform);         //作成したGatlingを子オブジェクトにする

        //位置と角度の初期設定
        mainTransform.localPosition = new Vector3(0, 0, 0);
        mainTransform.localRotation = Quaternion.Euler(0, 0, 0);

        //コンポーネントの取得
        BaseAtack ba = main.GetComponent<BaseAtack>(); //名前省略
        ba.Shooter = this;    //自分をヒットさせない
        weapons[(int)Weapon.MAIN] = ba;


        items = new Item[(int)ItemNum.NONE];
    }
    protected virtual void Start() { }
    protected virtual void Update() { }

    protected abstract void Move(float speed, float _maxSpeed, Vector3 direction);  //移動処理
    protected abstract IEnumerator UseBoost(float speedMgnf, float time);   //ブースト処理


    protected virtual void UseWeapon(Weapon weapon)     //攻撃処理
    {
        weapons[(int)weapon].Shot(_LockOn.Target);
    }

    public virtual void Damage(float power)
    {
        float p = Useful.DecimalPointTruncation(power, 1);  //小数点第2以下切り捨て

        //バリアが破壊されていなかったらバリアにダメージを肩代わりさせる
        if (_Barrier.HP > 0)
        {
            _Barrier.Damage(p);
        }
        //バリアが破壊されていたらドローンが直接ダメージを受ける
        else
        {
            HP -= p;
            if (HP < 0)
            {
                HP = 0;
            }


            //デバッグ用
            Debug.Log(name + "に" + p + "のダメージ\n残りHP: " + HP);
        }
    }

    public virtual void SetWeapon(AtackManager.Weapon weapon)
    {
        //サブウェポンの処理
        AtackManager.CreateAtack(out GameObject sub, weapon);    //武器の作成
        Transform subTransform = sub.transform; //キャッシュ
        subTransform.SetParent(transform);   //作成したGatlingを子オブジェクトにする

        //位置と角度の初期設定
        subTransform.localPosition = new Vector3(0, 0, 0);
        subTransform.localRotation = Quaternion.Euler(0, 0, 0);

        //コンポーネントの取得
        BaseAtack ba = sub.GetComponent<BaseAtack>();
        ba.Shooter = this;    //自分をヒットさせない
        weapons[(int)Weapon.SUB] = ba;
    }
}