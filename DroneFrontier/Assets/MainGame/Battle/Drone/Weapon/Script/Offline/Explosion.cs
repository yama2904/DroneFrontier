using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Offline
{
    public class Explosion : MonoBehaviour, IBullet
    {
        public uint PlayerID { get; set; } = 0;
        public float Power { get; private set; }

        [SerializeField, Tooltip("爆発範囲")] float size = 20; //爆発範囲
        [SerializeField, Tooltip("威力")] float power = 20;    //威力
        [SerializeField, Tooltip("爆発の中心地からの距離の威力減衰率(0～1)")] float powerDownRate = 0.2f; //中心地からの距離による威力減衰率
        [SerializeField, Tooltip("威力が減衰しない範囲")] float notPowerDownRange = 0.25f; //威力が減衰しない範囲
        [SerializeField, Tooltip("lengthReference長さごとにpowerDownRate%ダメージが減少する")] float lengthReference = 0.1f;    //威力減衰の基準の長さ
        AudioSource audioSource = null;

        List<GameObject> wasHitObjects = new List<GameObject>();    //ダメージを与えたオブジェクトを全て格納する
        const float DESTROY_TIME = 3.0f;   //生存時間


        void Awake()
        {
            Power = power;
        }

        void Start()
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

            //オーディオの初期化
            audioSource = GetComponent<AudioSource>();
            audioSource.clip = SoundManager.GetAudioClip(SoundManager.SE.EXPLOSION_MISSILE);
            audioSource.volume = SoundManager.BaseSEVolume;
            audioSource.time = 0.2f;
            audioSource.Play();


            //一定時間後に消滅
           Destroy(gameObject, DESTROY_TIME);
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
            return power * Mathf.Pow(1 - powerDownRate, distance / lengthReference);
        }

        private void OnTriggerEnter(Collider other)
        {
            //当たり判定を行わないオブジェクトだったら処理をしない
            if (other.CompareTag(TagNameManager.BULLET)) return;
            if (other.CompareTag(TagNameManager.ITEM)) return;
            if (other.CompareTag(TagNameManager.GIMMICK)) return;

            if (other.CompareTag(TagNameManager.PLAYER) || other.CompareTag(TagNameManager.CPU))
            {
                //ミサイルを撃った本人なら処理しない
                if (other.GetComponent<BaseDrone>().PlayerID == PlayerID) return;

                //既にヒット済のオブジェクトはスルー
                foreach (GameObject o in wasHitObjects)
                {
                    if (ReferenceEquals(other, o)) return;
                }
                other.GetComponent<DroneDamageAction>().Damage(power);
                wasHitObjects.Add(other.gameObject);

                //デバッグ用
                Debug.Log(other.name + "にExplosionで" + CalcPower(other.transform.position) + "ダメージ");
            }
            else if (other.CompareTag(TagNameManager.JAMMING_BOT))
            {
                //名前省略
                JammingBot jb = other.GetComponent<JammingBot>();

                //撃った人が放ったジャミングボットなら処理しない
                if (jb.creater.PlayerID == PlayerID) return;

                //既にヒット済のオブジェクトはスルー
                foreach (GameObject o in wasHitObjects)
                {
                    if (ReferenceEquals(other.gameObject, o)) return;
                }
                other.GetComponent<JammingBot>().Damage(power);
                wasHitObjects.Add(other.gameObject);


                //デバッグ用
                Debug.Log(other.name + "にExplosionで" + CalcPower(other.transform.position) + "ダメージ");
            }
        }
    }
}