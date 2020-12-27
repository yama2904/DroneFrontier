using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Jamming : MonoBehaviour
{
    [SerializeField] GameObject jammingBot = null;
    JammingBot createBot;
    List<BasePlayer> jamingObjects;

    [SerializeField] float destroyTime = 60.0f;
    float deltaTime;
    bool isEndCoroutine;

    void Start()
    {
        createBot = null;
        jamingObjects = new List<BasePlayer>();
        deltaTime = 0;
        isEndCoroutine = false;
    }

    void Update()
    {
        //コルーチンを抜けたら処理
        if (isEndCoroutine)
        {
            if (createBot == null)
            {
                //ジャミングを解除する
                foreach(BasePlayer bp in jamingObjects)
                {
                    bp._LockOn.UseLockOn(true);
                }
                Radar.UseRadar(true);

                Destroy(gameObject);
            }
        }
    }

    //ジャミングボットを生成する
    public void CreateBot(Transform t)
    {
        transform.position = t.position;

        Vector3 pos = transform.position;   //名前省略        
        createBot = Instantiate(jammingBot, //ボットを生成
            new Vector3(pos.x, pos.y + 2.0f, pos.z), Quaternion.Euler(0, 0, 0)).GetComponent<JammingBot>();
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
            if(deltaTime >= time)
            {
                bot.DestroyBot();
                isEndCoroutine = true;
                yield break;
            }
            //botが破壊されたらコルーチンを抜ける
            if(bot == null)
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
        bool isBasePlayer = false;
        BasePlayer bp = null;

        if (other.CompareTag(Player.PLAYER_TAG))
        {
            bp = other.GetComponent<BasePlayer>();
            bp._LockOn.UseLockOn(false);
            Radar.UseRadar(false);
            isBasePlayer = true;
        }
        else if (other.CompareTag(CPUController.CPU_TAG))
        {
            bp = other.GetComponent<BasePlayer>();
            bp._LockOn.UseLockOn(false);
            isBasePlayer = true;
        }

        //ジャミング内に入ったプレイヤーをリストに追加
        if (isBasePlayer)
        {
            jamingObjects.Add(bp);    //リストに追加
        }
    }

    private void OnTriggerExit(Collider other)
    {
        bool isBasePlayer = false;
        BasePlayer bp = null;
        if (other.CompareTag(Player.PLAYER_TAG))
        {
            bp = other.GetComponent<BasePlayer>();
            bp._LockOn.UseLockOn(true);
            Radar.UseRadar(true);
            isBasePlayer = true;
        }
        else if (other.CompareTag(CPUController.CPU_TAG))
        {
            bp = other.GetComponent<BasePlayer>();
            bp._LockOn.UseLockOn(true);
            isBasePlayer = true;
        }

        //ジャミングから出たプレイヤーをリストから削除
        if (isBasePlayer)
        {
            int index = jamingObjects.FindIndex(o => ReferenceEquals(bp, o));
            if (index >= 0)
            {
                jamingObjects.RemoveAt(index);
            }
        }
    }
}
