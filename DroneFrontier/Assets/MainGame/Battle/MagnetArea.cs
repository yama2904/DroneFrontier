using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagnetArea : MonoBehaviour
{
    [SerializeField, Tooltip("速度低下率")] float downPercent = 0.7f;  //下がる倍率
    public float DownPercent { get { return downPercent; } }

    //バグ防止用
    class HitPlayerData
    {
        public BattlePlayer player;
        public int id;
    }

    //ヒットしているプレイヤーを格納
    List<HitPlayerData> hitPlayerDatas = new List<HitPlayerData>();


    private void OnTriggerEnter(Collider other)
    {
        //プレイヤーのみ判定
        if (!other.CompareTag(TagNameManager.PLAYER)) return;

        BattlePlayer player = other.GetComponent<BattlePlayer>();   //名前省略        
        if (!player.isLocalPlayer) return;  //ローカルプレイヤーのみ判定

        //バグ防止用
        //既に範囲内に入っているプレイヤーは除外
        int index = hitPlayerDatas.FindIndex(p => ReferenceEquals(p.player, player));
        if (index != -1) return;

        //プレイヤーに状態異常を与えてリストに格納
        HitPlayerData hp = new HitPlayerData();
        hp.id = player.SetSpeedDown(downPercent);
        hp.player = player;
        hitPlayerDatas.Add(hp);


        //デバッグ用
        Debug.Log(other.GetComponent<BattlePlayer>().name + ": in磁場エリア");
    }

    private void OnTriggerExit(Collider other)
    {
        //プレイヤーのみ判定
        if (!other.CompareTag(TagNameManager.PLAYER)) return;

        BattlePlayer player = other.GetComponent<BattlePlayer>();   //名前省略        
        if (!player.isLocalPlayer) return;  //ローカルプレイヤーのみ判定

        //抜けるプレイヤーをリストから探す
        int index = hitPlayerDatas.FindIndex(p => ReferenceEquals(p.player, player));
        if (index == -1) return;   //バグ防止用
        player.UnSetSpeedDown(hitPlayerDatas[index].id);

        //抜けたプレイヤーはリストから削除
        hitPlayerDatas.RemoveAt(index);


        //デバッグ用
        Debug.Log(other.GetComponent<BattlePlayer>().name + ": out磁場エリア");
    }
}