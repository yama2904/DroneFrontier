using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Explosion : MonoBehaviour
{
    public GameObject notHitObject { private get; set; } = null;  //当たり判定を行わないオブジェクト
    [SerializeField] float size = 20;    //爆発範囲
    [SerializeField] float power = 20;   //威力
    [SerializeField] float powerDownRate = 0.8f;   //中心地からの距離による威力減衰率
    [SerializeField] float notPowerDownRange = 0.25f;    //威力が減衰しない範囲
    [SerializeField] float lengthReference = 0.1f;    //威力減衰の基準の長さ

    List<GameObject> wasHitObjects;    //触れたオブジェクトを全て格納する
    const float DESTROY_TIME = 3.0f;    //生存時間

    IEnumerator Start()
    {
        //サイズに応じて変数の値も変える
        notPowerDownRange *= size;
        lengthReference *= size;

        //各オブジェクトのサイズ変更
        foreach (Transform child in transform)
        {
            Vector3 scale = child.localScale;
            child.localScale = scale * size;

            //孫オブジェクトもあるか
            foreach (Transform grandChild in child)
            {
                scale = grandChild.localScale;
                grandChild.localScale = scale * size;
            }
        }
        //コライダーの設定
        SphereCollider sc = GetComponent<SphereCollider>();
        sc.radius *= size;
        sc.center = new Vector3(0, 0, 0);

        wasHitObjects = new List<GameObject>();

        //一定時間後に消滅
        yield return new WaitForSeconds(DESTROY_TIME);
        Destroy(gameObject);
    }

    void Update()
    {
    }

    private void OnTriggerEnter(Collider other)
    {
        //当たり判定を行わないオブジェクトだったら処理をしない
        if (ReferenceEquals(other.gameObject, notHitObject))
        {
            return;
        }

        //既にヒット済のオブジェクトはスルー
        foreach (GameObject o in wasHitObjects)
        {
            if (ReferenceEquals(other.gameObject, o))
            {
                return;
            }
        }

        if (other.CompareTag(Player.PLAYER_TAG))
        {
            other.GetComponent<Player>().Damage(CalcPower(other.transform.position));
            wasHitObjects.Add(other.gameObject);
        }

        if (other.CompareTag(CPUController.CPU_TAG))
        {
            other.GetComponent<CPUController>().Damage(CalcPower(other.transform.position));
            wasHitObjects.Add(other.gameObject);


            //デバッグ用
            Debug.Log("威力: " + CalcPower(other.transform.position));
        }
    }

    //相手の座標を入れると距離による最終的な威力を返す
    float CalcPower(Vector3 pos)
    {
        //相手との距離と求める
        float distance = Vector3.Distance(transform.position, pos);


        //デバッグ用
        Debug.Log("距離: " + distance);


        //威力が減衰しない範囲内に敵がいたらそのままの威力を返す
        distance -= notPowerDownRange;
        if (distance <= 0)
        {
            return power;
        }

        //長さに応じた減衰率を適用する
        return power * Mathf.Pow(powerDownRate, distance / lengthReference);
    }
}
