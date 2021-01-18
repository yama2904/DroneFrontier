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
    List<Player> jamingPlayers = new List<Player>();


    void Start()
    {
    }

    void Update()
    {
        if (createBot == null)
        {
            //ジャミングを解除する
            foreach (Player p in jamingPlayers)
            {
                p.UnSetJamming();
            }
            Destroy(gameObject);
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

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(TagNameManager.PLAYER)) return;   //プレイヤーのみ対象

        Player p = other.GetComponent<Player>();
        if (!p.IsLocalPlayer) return;   //ローカルプレイヤーのみ処理
        if (ReferenceEquals(p.gameObject, creater)) return; //ジャミングを付与しないプレイヤーならスキップ

        p.SetJamming(); //ジャミング付与
        jamingPlayers.Add(p);    //リストに追加
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(TagNameManager.PLAYER)) return;   //プレイヤーのみ対象

        Player p = other.GetComponent<Player>();
        if (!p.IsLocalPlayer) return;   //ローカルプレイヤーのみ処理
        if (ReferenceEquals(p.gameObject, creater)) return; //ジャミングを付与しないプレイヤーならスキップ
        
        //リストにない場合は処理しない
        int index = jamingPlayers.FindIndex(o => ReferenceEquals(p, o));
        if (index == -1) return;
                
        p.UnSetJamming();   //ジャミング解除
        jamingPlayers.RemoveAt(index);  //解除したプレイヤーをリストから削除
    }
}
