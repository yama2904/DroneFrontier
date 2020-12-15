using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cube : MonoBehaviour
{
    [SerializeField] float speed = 0.03f;
    float deltaTime = 1;

    AtackBase weapon;

    void Start()
    {
        AtackManager.CreateAtack(out GameObject o, AtackManager.Weapon.GATLING);    //Gatlingの生成
        o.transform.parent = transform;  //作成したGatlingを子オブジェクトにする

        //位置と角度の初期設定
        o.transform.localPosition = new Vector3(0, 0, 0);
        o.transform.localRotation = Quaternion.Euler(0, 0, 0);

        //コンポーネントの取得
        AtackBase ab = o.GetComponent<AtackBase>();
        ab.OwnerName = name;
        weapon = ab;
    }

    void Update()
    {
        //transform.position += new Vector3(speed * Mathf.Sin(deltaTime), 0, 0);
        deltaTime += Time.deltaTime;

        weapon.Shot(transform);
    }
}
