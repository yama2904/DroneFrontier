using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Jamming : NetworkBehaviour
{
    [SyncVar] GameObject creater;
    [SerializeField] float destroyTime = 60.0f;

    [SerializeField] GameObject jammingBot = null;
    [SerializeField] Transform jammingBotPosition = null;
    [SyncVar] GameObject createBot = null;
    SyncList<GameObject> jamingPlayers = new SyncList<GameObject>();


    void Start()
    {
    }

    [ServerCallback]
    void Update()
    {
        if (createBot == null)
        {
            //ジャミングを解除する
            foreach (GameObject p in jamingPlayers)
            {
                p.GetComponent<Player>().TargetUnSetJamming(p.GetComponent<NetworkIdentity>().connectionToClient);
            }

            NetworkServer.Destroy(gameObject);
        }
    }

    //ジャミングボットを生成する
    [Command(ignoreAuthority = true)]
    public void CmdCreateBot(GameObject creater)
    {
        this.creater = creater;
        transform.position = creater.transform.position;

        //ボット生成
        JammingBot jb = Instantiate(jammingBot, jammingBotPosition).GetComponent<JammingBot>();
        jb.parentNetId = netId;
        jb.creater = creater;

        NetworkServer.Spawn(jb.gameObject);
        createBot = jb.gameObject;

        //一定時間後にボットを削除
       Invoke(nameof(DestroyBot), destroyTime);


        //デバッグ用
        Debug.Log("ジャミングボット生成");
    }

    void DestroyBot()
    {
        NetworkServer.Destroy(createBot);
    }

    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(TagNameManager.PLAYER))
        {
            GameObject o = other.gameObject;
            if (ReferenceEquals(o, creater))   //ジャミングを付与しないプレイヤーならスキップ
            {
                return;
            }

            //ジャミング付与
            o.GetComponent<Player>().TargetSetJamming(o.GetComponent<NetworkIdentity>().connectionToClient);

            jamingPlayers.Add(o);    //リストに追加
        }
    }

    [ServerCallback]
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(TagNameManager.PLAYER))
        {
            GameObject p = other.gameObject;
            if (ReferenceEquals(p, creater))   //ジャミングを付与しないプレイヤーならスキップ
            {
                return;
            }

            //ジャミング解除
            p.GetComponent<Player>().TargetUnSetJamming(other.GetComponent<NetworkIdentity>().connectionToClient);

            //解除したプレイヤーをリストから削除
            int index = jamingPlayers.FindIndex(o => ReferenceEquals(p, o));
            if (index >= 0)
            {
                jamingPlayers.RemoveAt(index);
            }
        }
    }
}
