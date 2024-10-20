using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Mirror;

namespace Online
{
    public class Jamming : NetworkBehaviour
    {
        [SyncVar] GameObject creater;
        [SerializeField, Tooltip("ジャミングボットの生存時間")] float destroyTime = 60.0f;
        [SerializeField, Tooltip("ジャミングエリアから出てジャミングが継続する時間")] float jammingTime = 5f;

        [SerializeField] JammingBot jammingBot = null;
        [SerializeField] Transform jammingBotPosition = null;
        [SerializeField] ParticleSystem particle = null;
        GameObject createdBot = null;
        [SyncVar] bool syncIsCreateBot = false;
        bool isDestroy = false;

        class JammingPlayerData
        {
            public DroneStatusAction player;
            public float jammingTime = 0;
            public bool isExit = false;
        }
        List<JammingPlayerData> jammingPlayerDatas = new List<JammingPlayerData>();


        void Start() { }

        void Update()
        {
            if (!syncIsCreateBot) return;

            for(int i = jammingPlayerDatas.Count - 1; i >= 0; i--)
            {
                //名前省略
                JammingPlayerData jpd = jammingPlayerDatas[i];

                //ジャミングエリアから出て一定時間たったプレイヤーのジャミングを解除
                if (jpd.isExit)
                {
                    jpd.jammingTime += Time.deltaTime;
                    if(jpd.jammingTime >= jammingTime)
                    {
                        jpd.player.UnSetJamming();
                        jammingPlayerDatas.RemoveAt(i);
                    }
                }
            }

            if (isDestroy) return;
            if (isServer)
            {
                if (createdBot == null)
                {
                    RpcStopJamming();
                }

                //生成したプレイヤーが死んだら削除
                if (creater == null)
                {
                    NetworkServer.Destroy(createdBot);
                    RpcStopJamming();
                }
            }
        }

        //ジャミングボットを生成する
        [Command(ignoreAuthority = true)]
        public void CmdCreateBot(GameObject creater)
        {
            //キャッシュ
            Transform t = transform;

            this.creater = creater;
            t.position = creater.transform.position;

            //ボット生成
            JammingBot jb = Instantiate(jammingBot);

            jb.creater = creater;
            createdBot = jb.gameObject;
            NetworkServer.Spawn(createdBot);
            syncIsCreateBot = true;

            //ボットを生成した場所にオブジェクトがあるとオブジェクトの中にBotが入りこんで
            //破壊不可になるのでオブジェクトがある場合は避ける
            var hits = Physics.SphereCastAll(
                t.position, jammingBot.transform.localScale.x, t.up, jammingBotPosition.localPosition.y)
                .Where(h => !ReferenceEquals(creater, h.transform.gameObject))
                .Where(h => !h.transform.CompareTag(TagNameConst.JAMMING))
                .Where(h => !h.transform.CompareTag(TagNameConst.ITEM))
                .Where(h => !h.transform.CompareTag(TagNameConst.BULLET))
                .Where(h => !h.transform.CompareTag(TagNameConst.GIMMICK))
                .ToArray();

            Vector3 pos = jammingBotPosition.position;
            if (hits.Length > 0)
            {
                //一番近いオブジェクトの手前に避ける
                RaycastHit hit = hits[0];
                float minTargetDistance = float.MaxValue;   //初期化
                foreach (RaycastHit h in hits)
                {
                    //距離が最小だったら更新
                    if (h.distance < minTargetDistance)
                    {
                        minTargetDistance = h.distance;
                        hit = h;
                    }
                }

                pos = new Vector3(
                    jammingBotPosition.position.x, hit.point.y - 8f, jammingBotPosition.position.z);
            }

            //生成したボットと自分も移動
            createdBot.transform.position = pos;
            transform.position = pos;

            //一定時間後にボットを削除
            Invoke(nameof(DestroyJammingBot), destroyTime);


            //デバッグ用
            Debug.Log("ジャミングボット生成");
        }


        //ジャミングボットの破壊
        [Server]
        void DestroyJammingBot()
        {
            NetworkServer.Destroy(createdBot);
        }

        //ジャミングを停止する
        [ClientRpc]
        void RpcStopJamming()
        {
            GetComponent<Collider>().enabled = false;
            Destroy(particle);
            Destroy(gameObject, jammingTime);
            isDestroy = true;
        }

        void OnDestroy()
        {
            //ジャミングを解除する
            foreach (JammingPlayerData p in jammingPlayerDatas)
            {
                if (p.player == null) continue;
                p.player.UnSetJamming();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(TagNameConst.PLAYER)) return;   //プレイヤーのみ対象

            DroneStatusAction p = other.GetComponent<DroneStatusAction>();
            if (!p.isLocalPlayer) return;   //ローカルプレイヤーのみ処理
            if (ReferenceEquals(p.gameObject, creater)) return; //ジャミングを付与しないプレイヤーならスキップ

            //既にリストにある場合は経過時間をリセット
            int index = jammingPlayerDatas.FindIndex(o => ReferenceEquals(p, o.player));
            if(index >= 0)
            {
                jammingPlayerDatas[index].jammingTime = 0;
                return;
            }

            //リストにない場合はジャミング付与してリストに追加
            p.SetJamming();
            jammingPlayerDatas.Add(new JammingPlayerData
            {
                player = p
            });
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(TagNameConst.PLAYER)) return;   //プレイヤーのみ対象

            DroneStatusAction p = other.GetComponent<DroneStatusAction>();
            if (!p.isLocalPlayer) return;   //ローカルプレイヤーのみ処理
            if (ReferenceEquals(p.gameObject, creater)) return; //ジャミングを付与しないプレイヤーならスキップ

            //リストにない場合は処理しない
            int index = jammingPlayerDatas.FindIndex(o => ReferenceEquals(p, o.player));
            if (index == -1) return;

            jammingPlayerDatas[index].isExit = true;
        }
    }
}