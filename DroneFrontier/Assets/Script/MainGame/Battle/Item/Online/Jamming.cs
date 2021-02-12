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

        [SerializeField] JammingBot jammingBot = null;
        [SerializeField] Transform jammingBotPosition = null;
        GameObject createBot = null;
        bool isCreateBot = false;
        List<DroneStatusAction> jammingPlayers = new List<DroneStatusAction>();


        void Start() { }

        [ServerCallback]
        void Update()
        {
            if (!isCreateBot) return;
            if (createBot == null)
            {
                NetworkServer.Destroy(gameObject);
            }

            //生成したプレイヤーが死んだら削除
            if(creater == null)
            {
                NetworkServer.Destroy(createBot);
                NetworkServer.Destroy(gameObject);
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
            createBot = jb.gameObject;
            NetworkServer.Spawn(createBot);
            isCreateBot = true;

            //ボットを生成した場所にオブジェクトがあるとオブジェクトの中にBotが入りこんで
            //破壊不可になるのでオブジェクトがある場合は避ける
            var hits = Physics.SphereCastAll(
                t.position, jammingBot.transform.localScale.x, t.up, jammingBotPosition.localPosition.y)
                .Where(h => !ReferenceEquals(creater, h.transform.gameObject))
                .Where(h => !h.transform.CompareTag(TagNameManager.JAMMING))
                .Where(h => !h.transform.CompareTag(TagNameManager.ITEM))
                .Where(h => !h.transform.CompareTag(TagNameManager.BULLET))
                .Where(h => !h.transform.CompareTag(TagNameManager.GIMMICK))
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
            createBot.transform.position = pos;
            transform.position = pos;

            //一定時間後にボットを削除
            Invoke(nameof(DestroyMe), destroyTime);


            //デバッグ用
            Debug.Log("ジャミングボット生成");
        }

        void DestroyMe()
        {
            NetworkServer.Destroy(createBot);
        }

        void OnDestroy()
        {
            //ジャミングを解除する
            foreach (DroneStatusAction p in jammingPlayers)
            {
                if (p == null) continue;
                p.UnSetJamming();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(TagNameManager.PLAYER)) return;   //プレイヤーのみ対象

            DroneStatusAction p = other.GetComponent<DroneStatusAction>();
            if (!p.isLocalPlayer) return;   //ローカルプレイヤーのみ処理
            if (ReferenceEquals(p.gameObject, creater)) return; //ジャミングを付与しないプレイヤーならスキップ

            p.SetJamming(); //ジャミング付与
            jammingPlayers.Add(p);    //リストに追加
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(TagNameManager.PLAYER)) return;   //プレイヤーのみ対象

            DroneStatusAction p = other.GetComponent<DroneStatusAction>();
            if (!p.isLocalPlayer) return;   //ローカルプレイヤーのみ処理
            if (ReferenceEquals(p.gameObject, creater)) return; //ジャミングを付与しないプレイヤーならスキップ

            //リストにない場合は処理しない
            int index = jammingPlayers.FindIndex(o => ReferenceEquals(p, o));
            if (index == -1) return;

            p.UnSetJamming();   //ジャミング解除
            jammingPlayers.RemoveAt(index);  //解除したプレイヤーをリストから削除
        }
    }
}