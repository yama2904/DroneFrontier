using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class MagnetArea : NetworkBehaviour
{
    [SerializeField] ParticleSystem particle1 = null;
    [SerializeField] ParticleSystem particle2 = null;

    [SerializeField, Tooltip("速度低下率")] float downPercent = 0.7f;  //下がる倍率
    public float DownPercent
    {
        get { return downPercent; }
        set
        {
            float v = value;
            if(v <= 0)
            {
                v = 0;
            }
            if(v >= 1f)
            {
                v = 1f;
            }
            downPercent = 1f;
        }
    }

    //バグ防止用
    class HitPlayerData
    {
        public BattleDrone player;
        public int id;
    }
    //ヒットしているプレイヤーを格納
    List<HitPlayerData> hitPlayerDatas = new List<HitPlayerData>();

    //エリアが起動中か
    bool areaFlag = false;


    private void OnTriggerEnter(Collider other)
    {
        if (!areaFlag) return;

        //プレイヤーのみ判定
        if (!other.CompareTag(TagNameManager.PLAYER)) return;

        BattleDrone player = other.GetComponent<BattleDrone>();   //名前省略        
        if (!player.isLocalPlayer) return;  //ローカルプレイヤーのみ判定

        //バグ防止用
        //既に範囲内に入っているプレイヤーは除外
        int index = hitPlayerDatas.FindIndex(p => p.player.netId == player.netId);
        if (index != -1) return;

        //プレイヤーに状態異常を与えてリストに格納
        HitPlayerData hp = new HitPlayerData();
        hp.player = player;
        hp.id = player.SetSpeedDown(downPercent);
        hitPlayerDatas.Add(hp);


        //デバッグ用
        Debug.Log(other.GetComponent<BattleDrone>().name + ": in磁場エリア");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!areaFlag) return;

        //プレイヤーのみ判定
        if (!other.CompareTag(TagNameManager.PLAYER)) return;

        BattleDrone player = other.GetComponent<BattleDrone>();   //名前省略        
        if (!player.isLocalPlayer) return;  //ローカルプレイヤーのみ判定

        //抜けるプレイヤーをリストから探す
        int index = hitPlayerDatas.FindIndex(p => p.player.netId == player.netId);
        if (index == -1) return;   //バグ防止用
        player.UnSetSpeedDown(hitPlayerDatas[index].id);

        //抜けたプレイヤーはリストから削除
        hitPlayerDatas.RemoveAt(index);


        //デバッグ用
        Debug.Log(other.GetComponent<BattleDrone>().name + ": out磁場エリア");
    }

    public void SetArea(bool flag)
    {
        if (flag)
        {
            //オブジェクトを表示
            particle1.Play();
            particle2.Play();
        }
        else
        {
            //速度低下中の全てのプレイヤーの速度を戻す
            foreach (HitPlayerData hpd in hitPlayerDatas)
            {
                if (hpd.player == null) continue;
                hpd.player.UnSetSpeedDown(hpd.id);
            }
            hitPlayerDatas.Clear();

            //オブジェクトを非表示
            particle1.Stop();
            particle2.Stop();
        }
        areaFlag = flag;
    }

    [ClientRpc]
    public void RpcSetArea(bool flag)
    {
        SetArea(flag);
    }
}