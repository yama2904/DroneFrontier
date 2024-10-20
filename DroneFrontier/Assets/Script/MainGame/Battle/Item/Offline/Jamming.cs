using Offline.Player;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Offline
{
    public class Jamming : MonoBehaviour, IGameItem
    {
        IBattleDrone creater;
        [SerializeField, Tooltip("ジャミングボットの生存時間")] float destroyTime = 60.0f;

        [SerializeField] JammingBot jammingBot = null;
        [SerializeField] Transform jammingBotPosition = null;
        JammingBot createBot = null;
        bool isCreateBot = false;
        List<DroneStatusComponent> jamingPlayers = new List<DroneStatusComponent>();

        /// <summary>
        /// キャッシュ用Transform
        /// </summary>
        private Transform _transform = null;

        void Start() 
        {
            _transform = transform;
        }

        void Update()
        {
            if (!isCreateBot) return;
            if (createBot == null)
            {
                Destroy(gameObject);
            }
        }

        public bool UseItem(GameObject drone)
        {
            //ボット生成
            createBot = Instantiate(jammingBot);

            createBot.creater = creater;
            isCreateBot = true;

            //ボットを生成した場所にオブジェクトがあるとオブジェクトの中にBotが入りこんで
            //破壊不可になるのでオブジェクトがある場合は避ける
            var hits = Physics.SphereCastAll(
                _transform.position, jammingBot.transform.localScale.x, _transform.up, jammingBotPosition.localPosition.y)
                // ToDo:未定
                //.Where(h => !ReferenceEquals(creater.gameObject, h.transform.gameObject))
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

                pos = new Vector3(jammingBotPosition.position.x, hit.point.y - 8f, jammingBotPosition.position.z);
            }
            //生成したボットと自分も移動
            createBot.transform.position = pos;
            transform.position = pos;

            //一定時間後にボットを削除
            Destroy(createBot.gameObject, destroyTime);

            return true;
        }

        //ジャミングボットを生成する
        public void CreateBot(IBattleDrone creater)
        {
            //キャッシュ
            Transform t = transform;

            // ToDo:未定
            //playerID = creater.PlayerID;
            //t.position = creater.transform.position;

            //ボット生成
            createBot = Instantiate(jammingBot);

            createBot.creater = creater;
            isCreateBot = true;

            //ボットを生成した場所にオブジェクトがあるとオブジェクトの中にBotが入りこんで
            //破壊不可になるのでオブジェクトがある場合は避ける
            var hits = Physics.SphereCastAll(
                t.position, jammingBot.transform.localScale.x, t.up, jammingBotPosition.localPosition.y)
                // ToDo:未定
                //.Where(h => !ReferenceEquals(creater.gameObject, h.transform.gameObject))
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
            createBot.transform.position = pos;
            transform.position = pos;

            //一定時間後にボットを削除
            Destroy(createBot.gameObject, destroyTime);


            //デバッグ用
            Debug.Log("ジャミングボット生成");
        }

        void OnDestroy()
        {
            //ジャミングを解除する
            foreach (DroneStatusComponent p in jamingPlayers)
            {
                if (p == null) continue;
                p.UnSetJamming();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(TagNameConst.PLAYER)) return;   //プレイヤーのみ対象
            if (other.GetComponent<IBattleDrone>() == creater) return; //ジャミングを付与しないプレイヤーならスキップ
            
            DroneStatusComponent player = other.GetComponent<DroneStatusComponent>();  //名前省略
            player.SetJamming(); //ジャミング付与
            jamingPlayers.Add(player);    //リストに追加
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(TagNameConst.PLAYER)) return;   //プレイヤーのみ対象
            if (other.GetComponent<IBattleDrone>() == creater) return; //ジャミングを付与しないプレイヤーならスキップ

            //名前省略
            DroneStatusComponent player = other.GetComponent<DroneStatusComponent>(); 
            
            //リストにない場合は処理しない
            int index = jamingPlayers.FindIndex(o => ReferenceEquals(o, player));
            if (index == -1) return;

            player.UnSetJamming();   //ジャミング解除
            jamingPlayers.RemoveAt(index);  //解除したプレイヤーをリストから削除
        }
    }
}