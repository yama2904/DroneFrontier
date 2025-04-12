using Network.Udp;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Network
{
    /// <summary>
    /// 全ての通信相手とのゲームオブジェクトの生成・削除を管理するクラス
    /// </summary>
    public class NetworkObjectSpawner
    {
        public static Dictionary<string, MyNetworkBehaviour> SpawnedObjects { get; private set; } = new Dictionary<string, MyNetworkBehaviour>();

        public static void Initialize() 
        {
            // 受信イベント設定
            MyNetworkManager.Singleton.OnUdpReceiveOnMainThread += OnUdpReceive;
        }

        /// <summary>
        /// 指定したオブジェクトを全プレイヤーに生成させる
        /// </summary>
        /// <param name="obj">生成させるオブジェクト</param>
        public static void Spawn(MyNetworkBehaviour obj)
        {
            // ID設定
            obj.ObjectId = Guid.NewGuid().ToString("N");

            // パケット送信
            MyNetworkManager.Singleton.SendToAll(new SpawnPacket(obj));

            // 削除イベント設定
            obj.OnDestroyObject += OnDestroy;

            // 生成オブジェクト一覧に追加
            SpawnedObjects.Add(obj.ObjectId, obj);
        }

        /// <summary>
        /// 指定したオブジェクトを全プレイヤーから削除する
        /// </summary>
        /// <param name="obj">削除するオブジェクト</param>
        public static void Destroy(MyNetworkBehaviour obj)
        {
            MyNetworkManager.Singleton.SendToAll(new DestroyPacket(obj.ObjectId));
            SpawnedObjects.Remove(obj.ObjectId);
        }

        /// <summary>
        /// UDPパケット受信イベント
        /// </summary>
        /// <param name="name">プレイヤー名</param>
        /// <param name="header">受信したUDPパケットのヘッダ</param>
        /// <param name="packet">受信したUDPパケット</param>
        private static async void OnUdpReceive(string name, UdpHeader header, UdpPacket packet)
        {
            // オブジェクト生成パケット
            if (header == UdpHeader.Spawn)
            {
                SpawnPacket spawnPacket = packet as SpawnPacket;

                // オブジェクト生成
                GameObject obj = await Addressables.InstantiateAsync(spawnPacket.AddressKey, spawnPacket.Position, spawnPacket.Rotation).Task;
                MyNetworkBehaviour spawn = obj.GetComponent<MyNetworkBehaviour>();

                // ID設定
                spawn.ObjectId = spawnPacket.ObjectId;

                // 生成データ設定
                spawn.ImportSpawnData(spawnPacket.SpawnData);

                // 削除イベント設定
                spawn.OnDestroyObject += OnDestroy;

                // 初期化
                spawn.Initialize();

                // 生成オブジェクト一覧に追加
                SpawnedObjects.Add(spawn.ObjectId, spawn);
            }

            // オブジェクト削除パケット
            if (header == UdpHeader.Destroy)
            {
                string id = (packet as DestroyPacket).Id;
                if (SpawnedObjects.ContainsKey(id))
                {
                    SpawnedObjects[id].OnDestroyObject -= OnDestroy;
                    UnityEngine.Object.Destroy(SpawnedObjects[id].gameObject);
                    SpawnedObjects.Remove(id);
                }
            }
        }

        /// <summary>
        /// 生成したオブジェクトの削除ベント
        /// </summary>
        /// <param name="sender">イベントオブジェクト</param>
        /// <param name="args">イベント引数</param>
        private static void OnDestroy(object sender, EventArgs args)
        {
            // 削除オブジェクト取得
            MyNetworkBehaviour obj = sender as MyNetworkBehaviour;

            // 削除イベント除去
            obj.OnDestroyObject -= OnDestroy;

            // 全プレイヤーに削除を知らせる
            Destroy(obj);
        }
    }
}
