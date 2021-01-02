using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Jamming : MonoBehaviour
{
    GameObject creater;

    [SerializeField] GameObject jammingBot = null;
    [SerializeField] Transform jammingBotPosition = null;
    JammingBot createBot = null; 
    List<BasePlayer> jamingPlayers = new List<BasePlayer>();

    [SerializeField] float destroyTime = 60.0f;
    float deltaTime = 0;
    bool isEndCoroutine = false;

    void Start()
    {
    }

    void Update()
    {
        //コルーチンを抜けたら処理
        if (isEndCoroutine)
        {
            if (createBot == null)
            {
                //ジャミングを解除する
                foreach (BasePlayer bp in jamingPlayers)
                {
                    IPlayerStatus ps = bp;
                    ps.UnSetJamming();
                }                

                Destroy(gameObject);
            }
        }
    }

    //ジャミングボットを生成する
    public void CreateBot(GameObject creater)
    {
        this.creater = creater;
        transform.position = creater.transform.position;

        //ボット生成
        createBot = Instantiate(jammingBot, jammingBotPosition).GetComponent<JammingBot>();

        //ボットの向きを変える
        Vector3 angle = createBot.transform.localEulerAngles;
        angle.y += creater.transform.localEulerAngles.y;
        createBot.transform.localEulerAngles = angle;

        createBot.Creater = creater;
        createBot.transform.SetParent(transform);   //ボットを子に設定


        //デバッグ用
        Debug.Log("ジャミングボット生成");


        StartCoroutine(JammingCoroutine(createBot, destroyTime));
    }

    IEnumerator JammingCoroutine(JammingBot bot, float time)
    {
        //毎フレーム処理
        while (true)
        {
            //一定時間経過したらbotを破壊してコルーチンを抜ける
            if (deltaTime >= time)
            {
                bot.DestroyBot();
                isEndCoroutine = true;
                yield break;
            }
            //botが破壊されたらコルーチンを抜ける
            if (bot == null)
            {
                isEndCoroutine = true;
                yield break;
            }

            deltaTime += Time.deltaTime;
            yield return null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(Player.PLAYER_TAG) || other.CompareTag(CPUController.CPU_TAG))
        {
            BasePlayer bp = other.GetComponent<BasePlayer>();
            if (ReferenceEquals(bp.gameObject, creater))   //ジャミングを付与しないプレイヤーならスキップ
            {
                return;
            }

            //ジャミング付与
            IPlayerStatus ps = bp;
            ps.SetJamming();

            jamingPlayers.Add(bp);    //リストに追加
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(Player.PLAYER_TAG) || other.CompareTag(CPUController.CPU_TAG))
        {
            BasePlayer bp = other.GetComponent<BasePlayer>();
            if (ReferenceEquals(bp.gameObject, creater))   //ジャミングを付与しないプレイヤーならスキップ
            {
                return;
            }

            //ジャミング解除
            IPlayerStatus ps = bp;
            ps.UnSetJamming();

            //解除したプレイヤーをリストから削除
            int index = jamingPlayers.FindIndex(o => ReferenceEquals(bp, o));
            if (index >= 0)
            {
                jamingPlayers.RemoveAt(index);
            }
        }
    }
}
