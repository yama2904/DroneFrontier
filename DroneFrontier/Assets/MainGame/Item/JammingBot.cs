using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class JammingBot : NetworkBehaviour
{
    [SyncVar] float HP = 30.0f;
    [SyncVar, HideInInspector] public uint parentNetId = 0;
    [SyncVar, HideInInspector] public GameObject creater = null;
    [SyncVar] bool syncIsDestroy = false;
    public bool IsDestroy { get { return syncIsDestroy; } }


    public override void OnStartClient()
    {
        base.OnStartClient();
        GameObject parent = NetworkIdentity.spawned[parentNetId].gameObject;
        transform.SetParent(parent.transform);

        //ボットの向きを変える
        Vector3 angle = transform.localEulerAngles;
        angle.y += creater.transform.localEulerAngles.y;
        transform.localEulerAngles = angle;

        //生成した自分のジャミングボットをプレイヤーがロックオン・照射しないように設定
        if (creater.CompareTag(TagNameManager.PLAYER))
        {
            creater.GetComponent<Player>().SetNotLockOnObject(gameObject);
            creater.GetComponent<Player>().SetNotRadarObject(gameObject);
        }
    }

    private void OnDestroy()
    {
        //SetNotLockOnObject、SetNotRadarObjectを解除
        if (creater.CompareTag(TagNameManager.PLAYER))
        {
            creater.GetComponent<Player>().UnSetNotLockOnObject(gameObject);
            creater.GetComponent<Player>().UnSetNotRadarObject(gameObject);
        }


        //デバッグ用
        Debug.Log("ジャミングボット破壊");
    }

    [Command(ignoreAuthority = true)]
    public void CmdDamage(float power)
    {
        float p = Useful.DecimalPointTruncation(power, 1);   //小数点第2以下切り捨て
        HP -= p;
        if (HP < 0)
        {
            HP = 0;
            syncIsDestroy = true;
        }
    }
}
