using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosion : MonoBehaviour
{
    public string OwnerName { private get; set; } = "";  //当たり判定を行わないオブジェクトの名前
    [SerializeField] float size = 20;    //爆発範囲
    [SerializeField] float power = 20;   //威力

    List<string> names;    //触れたプレイヤーの名前を全て格納する
    const float DESTROY_TIME = 3.0f;    //生存時間
    float deltaTime;    //時間を計測する用

    void Start()
    {
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

        names = new List<string>();
        deltaTime = 0;
    }

    void Update()
    {
        deltaTime += Time.deltaTime;
        if (deltaTime > DESTROY_TIME)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        //当たり判定を行わないオブジェクトだったら処理をしない
        if (other.name == OwnerName)
        {
            return;
        }

        //既にヒット済のオブジェクトはスルー
        foreach (string name in names)
        {
            if (other.name == name)
            {
                return;
            }
        }

        if (other.gameObject.tag == Player.PLAYER_TAG)
        {
            other.GetComponent<Player>().Damage(power);
            names.Add(other.name);
        }

        if (other.gameObject.tag == CPUController.CPU_TAG)
        {
            other.GetComponent<CPUController>().Damage(power);
            names.Add(other.name);
        }
    }
}
