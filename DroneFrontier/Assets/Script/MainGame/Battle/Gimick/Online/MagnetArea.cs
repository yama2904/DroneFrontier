using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace Online
{
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
                if (v <= 0)
                {
                    v = 0;
                }
                if (v >= 1f)
                {
                    v = 1f;
                }
                downPercent = 1f;
            }
        }

        //ヒットしているプレイヤーを格納
        List<DroneStatusAction> hitPlayerDatas = new List<DroneStatusAction>();

        //エリアが起動中か
        bool areaFlag = false;


        private void OnTriggerEnter(Collider other)
        {
            if (!areaFlag) return;

            //プレイヤーのみ判定
            if (!other.CompareTag(TagNameManager.PLAYER)) return;

            DroneStatusAction player = other.GetComponent<DroneStatusAction>();   //名前省略        
            if (!player.isLocalPlayer) return;  //ローカルプレイヤーのみ判定

            //バグ防止用
            //既に範囲内に入っているプレイヤーは除外
            int index = hitPlayerDatas.FindIndex(p => p.netId == player.netId);
            if (index != -1) return;

            //プレイヤーに状態異常を与えてリストに格納
            player.SetSpeedDown(downPercent);
            hitPlayerDatas.Add(player);


            //デバッグ用
            Debug.Log(other.GetComponent<BattleDrone>().name + ": in磁場エリア");
        }

        private void OnTriggerExit(Collider other)
        {
            if (!areaFlag) return;

            //プレイヤーのみ判定
            if (!other.CompareTag(TagNameManager.PLAYER)) return;

            DroneStatusAction player = other.GetComponent<DroneStatusAction>();   //名前省略        
            if (!player.isLocalPlayer) return;  //ローカルプレイヤーのみ判定

            //抜けるプレイヤーをリストから探す
            int index = hitPlayerDatas.FindIndex(p => p.netId == player.netId);
            if (index == -1) return;   //バグ防止用
            player.UnSetSpeedDown(downPercent);

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
                foreach (DroneStatusAction p in hitPlayerDatas)
                {
                    if (p == null) continue;
                    p.UnSetSpeedDown(downPercent);
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
}