using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CPUController : BasePlayer
{
    public const string CPU_TAG = "CPU";    //タグ名
    Transform cacheTransform = null;

    //デバッグ用
    [SerializeField] float speed = 0.1f;
    [SerializeField] bool isAtack = true;
    float deltaTime = 1;

    protected override void Start()
    {
        cacheTransform = transform;
        _rigidbody = GetComponent<Rigidbody>();

        HP = 1000;
        MoveSpeed = speed;
        MaxSpeed = 30.0f;

        //武器の初期化
        weapons = new AtackBase[(int)Weapon.NONE];

        //メインウェポンの処理
        AtackManager.CreateAtack(out GameObject main, AtackManager.Weapon.GATLING);    //Gatlingの生成
        Transform mainTransform = main.transform;   //キャッシュ
        mainTransform.parent = cacheTransform;      //作成したGatlingを子オブジェクトにする

        //位置と角度の初期設定
        mainTransform.localPosition = new Vector3(0, 0, 0);
        mainTransform.localRotation = Quaternion.Euler(0, 0, 0);

        //コンポーネントの取得
        AtackBase abM = main.GetComponent<AtackBase>(); //名前省略
        abM.notHitObject = gameObject;    //自分をヒットさせない
        weapons[(int)Weapon.MAIN] = abM;


        //サブウェポンの処理
        AtackManager.CreateAtack(out GameObject sub, AtackManager.Weapon.SHOTGUN);    //Shotgunの作成
        Transform subTransform = sub.transform; //キャッシュ
        subTransform.parent = cacheTransform;   //作成したGatlingを子オブジェクトにする

        //位置と角度の初期設定
        subTransform.localPosition = new Vector3(0, 0, 0);
        subTransform.localRotation = Quaternion.Euler(0, 0, 0);

        //コンポーネントの取得
        AtackBase abS = sub.GetComponent<AtackBase>();
        abS.notHitObject = gameObject;    //自分をヒットさせない
        weapons[(int)Weapon.SUB] = abS;

        items = new Item[(int)ItemNum.NONE];
    }

    protected override void Update()
    {
        if (isAtack)
        {
            UseWeapon(Weapon.MAIN);
        }

        //デバッグ用
        //transform.position += new Vector3(MoveSpeed * Mathf.Sin(deltaTime), 0, 0);
        deltaTime += Time.deltaTime;
    }

    protected override void UseWeapon(Weapon weapon)
    {
        weapons[(int)weapon].Shot();
    }

    protected override void Move(float speed, float _maxSpeed, Vector3 direction)
    {
        
    }

    //ダメージを与える
    public override void Damage(float power)
    {
        float p = Useful.DecimalPointTruncation(power, 1);  //小数点第2以下切り捨て
        HP -= p;
        if(HP < 0)
        {
            HP = 0;
        }
        Debug.Log(name + "に" + p + "のダメージ\n残りHP: " + HP);
    }
}
