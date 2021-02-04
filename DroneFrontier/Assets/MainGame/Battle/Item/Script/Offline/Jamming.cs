using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Offline
{
    public class Jamming : MonoBehaviour
    {
        GameObject creater;
        [SerializeField, Tooltip("ジャミングボットの生存時間")] float destroyTime = 60.0f;

        [SerializeField] JammingBot jammingBot = null;
        [SerializeField] Transform jammingBotPosition = null;
        JammingBot createBot = null;
        bool isCreateBot = false;
        List<BattleDrone> jamingPlayers = new List<BattleDrone>();


        void Start() { }

        void Update()
        {
            if (!isCreateBot) return;
            if (createBot == null)
            {
                Destroy(gameObject);
            }
        }

        //ジャミングボットを生成する
        public void CreateBot(GameObject creater)
        {
            //キャッシュ
            Transform t = transform;

            this.creater = creater;
            t.position = creater.transform.position;

            //ボット生成
            createBot = Instantiate(jammingBot);

            createBot.creater = creater;
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

                createBot.transform.position = new Vector3(
                    jammingBotPosition.position.x, hit.point.y - 8f, jammingBotPosition.position.z);
            }
            else
            {
                createBot.transform.position = jammingBotPosition.position;
            }


            //一定時間後にボットを削除
            Destroy(createBot.gameObject, destroyTime);


            //デバッグ用
            Debug.Log("ジャミングボット生成");
        }

        void OnDestroy()
        {
            //ジャミングを解除する
            foreach (BattleDrone p in jamingPlayers)
            {
                if (p == null) continue;
                p.UnSetJamming();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(TagNameManager.PLAYER)) return;   //プレイヤーのみ対象

            BattleDrone p = other.GetComponent<BattleDrone>();
            if (ReferenceEquals(p.gameObject, creater)) return; //ジャミングを付与しないプレイヤーならスキップ

            p.SetJamming(); //ジャミング付与
            jamingPlayers.Add(p);    //リストに追加
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(TagNameManager.PLAYER)) return;   //プレイヤーのみ対象

            BattleDrone p = other.GetComponent<BattleDrone>();
            if (ReferenceEquals(p.gameObject, creater)) return; //ジャミングを付与しないプレイヤーならスキップ

            //リストにない場合は処理しない
            int index = jamingPlayers.FindIndex(o => ReferenceEquals(p, o));
            if (index == -1) return;

            p.UnSetJamming();   //ジャミング解除
            jamingPlayers.RemoveAt(index);  //解除したプレイヤーをリストから削除
        }
    }
}