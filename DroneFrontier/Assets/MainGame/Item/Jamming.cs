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
                    //bp._LockOn.UseLockOn(true);
                }                
                //Radar.UseRadar(true);

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
        createBot = Instantiate(jammingBot,
            transform.position + jammingBotPosition.localPosition, Quaternion.Euler(0, 0, 0)).GetComponent<JammingBot>();
        createBot.CreatedPlayer = creater;
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
            if (ReferenceEquals(bp, creater))   //ジャミングを付与しないプレイヤーならスキップ
            {
                return;
            }

            //bp._LockOn.UseLockOn(false);
            jamingPlayers.Add(bp);    //リストに追加

            if (other.CompareTag(Player.PLAYER_TAG))
            {
                //Radar.UseRadar(false);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(Player.PLAYER_TAG) || other.CompareTag(CPUController.CPU_TAG))
        {
            BasePlayer bp = other.GetComponent<BasePlayer>();
            if (ReferenceEquals(bp, creater))   //ジャミングを付与しないプレイヤーならスキップ
            {
                return;
            }

            //bp._LockOn.UseLockOn(true);
            int index = jamingPlayers.FindIndex(o => ReferenceEquals(bp, o));
            if (index >= 0)
            {
                jamingPlayers.RemoveAt(index);
            }

            if (other.CompareTag(Player.PLAYER_TAG))
            {
                //Radar.UseRadar(true);
            }
        }
    }
}
