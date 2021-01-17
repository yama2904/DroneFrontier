using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class JammingBot : NetworkBehaviour
{
    public GameObject Creater { get; set; } = null;
    [SyncVar] float HP = 30.0f;
    [SyncVar, HideInInspector] public uint parentNetId = 0;
    [SyncVar, HideInInspector] public GameObject createrTransform = null;


    public override void OnStartClient()
    {
        base.OnStartClient();
        GameObject parent = NetworkIdentity.spawned[parentNetId].gameObject;
        transform.SetParent(parent.transform);

        //ボットの向きを変える
        Vector3 angle = transform.localEulerAngles;
        angle.y += createrTransform.transform.localEulerAngles.y;
        transform.localEulerAngles = angle;
    }

    public void Init(GameObject creater)
    {
        //生成した自分のジャミングボットをプレイヤーがロックオンしないように設定
        if (creater.CompareTag(TagNameManager.PLAYER))
        {
            creater.GetComponent<Player>().CmdSetNotLockOnObject(gameObject);
        }
    }

    private void OnDestroy()
    {
        //SetNotLockOnObjectを解除
        if (Creater.CompareTag(TagNameManager.PLAYER))
        {
            Creater.GetComponent<Player>().CmdUnSetNotLockOnObject(gameObject);
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
            NetworkServer.Destroy(gameObject);
        }
    }
}
