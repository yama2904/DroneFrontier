using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Mirror;

namespace Online
{
    public class Explosion : NetworkBehaviour
    {
        [SyncVar, HideInInspector] public GameObject Shooter = null;  //撃ったプレイヤー
        [SerializeField, Tooltip("爆発範囲")] float size = 20;    //爆発範囲
        [SerializeField, Tooltip("威力")] float power = 20;   //威力
        [SerializeField, Tooltip("爆発の中心地からの距離の威力減衰率(0～1)")] float powerDownRate = 0.2f; //中心地からの距離による威力減衰率
        [SerializeField, Tooltip("威力が減衰しない範囲")] float notPowerDownRange = 0.25f; //威力が減衰しない範囲
        [SerializeField, Tooltip("lengthReference長さごとにpowerDownRate%ダメージが減少する")] float lengthReference = 0.1f;    //威力減衰の基準の長さ
        AudioSource audioSource = null;

        SyncList<GameObject> wasHitObjects = new SyncList<GameObject>();    //ダメージを与えたオブジェクトを全て格納する
        const float DESTROY_TIME = 3.0f;   //生存時間


        public override void OnStartClient()
        {
            base.OnStartClient();
            audioSource = GetComponent<AudioSource>();
            audioSource.clip = SoundManager.GetAudioClip(SoundManager.SE.EXPLOSION_MISSILE);
            audioSource.volume = SoundManager.BaseSEVolume;
            audioSource.time = 0.2f;
            audioSource.Play();
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

            //爆発した直後に当たり判定を消す
            Invoke(nameof(FalseEnabledCollider), 0.2f);


            //一定時間後に消滅
            Invoke(nameof(DestroyMe), DESTROY_TIME);
        }


        void FalseEnabledCollider()
        {
            GetComponent<SphereCollider>().enabled = false;
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

        [ServerCallback]
        void DestroyMe()
        {
            NetworkServer.Destroy(gameObject);
        }


        [ServerCallback]
        private void OnTriggerEnter(Collider other)
        {
            //当たり判定を行わないオブジェクトだったら処理をしない
            if (ReferenceEquals(other.gameObject, Shooter)) return;
            if (other.CompareTag(TagNameManager.BULLET)) return;
            if (other.CompareTag(TagNameManager.ITEM)) return;
            if (other.CompareTag(TagNameManager.JAMMING)) return;
            if (other.CompareTag(TagNameManager.GIMMICK)) return;

            if (other.CompareTag(TagNameManager.PLAYER))
            {
                //既にヒット済のオブジェクトはスルー
                foreach (GameObject o in wasHitObjects)
                {
                    if (ReferenceEquals(other, o)) return;
                }
                other.GetComponent<BattleDrone>().CmdDamage(power);
                wasHitObjects.Add(other.gameObject);

                //デバッグ用
                Debug.Log(other.name + "にExplosionで" + CalcPower(other.transform.position) + "ダメージ");
            }
            else if (other.CompareTag(TagNameManager.JAMMING_BOT))
            {
                JammingBot jb = other.GetComponent<JammingBot>();
                if (ReferenceEquals(jb.creater, Shooter)) return;

                //既にヒット済のオブジェクトはスルー
                foreach (GameObject o in wasHitObjects)
                {
                    if (ReferenceEquals(other.gameObject, o)) return;
                }
                other.GetComponent<JammingBot>().CmdDamage(power);
                wasHitObjects.Add(other.gameObject);


                //デバッグ用
                Debug.Log(other.name + "にExplosionで" + CalcPower(other.transform.position) + "ダメージ");
            }
        }
    }
}