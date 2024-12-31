using Network.Udp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;

namespace Network
{
    public class MyNetworkBehaviour : MonoBehaviour
    {
        [SerializeField, Tooltip("オブジェクトをIDで管理して全ての通信相手と共有するか")]
        private bool _generateId = false;

        /// <summary>
        /// クラス名
        /// </summary>
        private string _className = string.Empty;

        /// <summary>
        /// メソッド一覧
        /// </summary>
        private MethodInfo[] _methods = null;

        /// <summary>
        /// 通信相手とのオブジェクト共有用ID
        /// </summary>
        private long _id = -1;

        /// <summary>
        /// オブジェクト共有ID採番値
        /// </summary>
        private static long _numberingId = 1;

        /// <summary>
        /// 引数に渡されたメソッドを実行し、全ての通信相手にも実行させる
        /// </summary>
        /// <param name="action">実行するメソッド</param>
        protected void SendMethod(Expression<Action> action)
        {
            // メソッド解析
            MethodCallExpression methodCall = action.Body as MethodCallExpression;

            // メソッド名取得
            string name = methodCall.Method.Name;

            // 引数取得
            List<object> args = new List<object>();
            foreach (var arg in methodCall.Arguments)
            {
                object value = Expression.Lambda(arg).Compile().DynamicInvoke();
                args.Add(value);
            }

            // パケット送信
            Packet packet = new SendMethodPacket(_className, name, args.ToArray(), _id);
            MyNetworkManager.Singleton.SendAsync(packet);

            // メソッド実行
            action.Compile().Invoke();
        }

        private void Awake()
        {
            // クラス名取得
            _className = GetType().Name;

            // メソッド一覧取得
            _methods = GetType().GetMethods(BindingFlags.Instance |
                                            BindingFlags.Public |
                                            BindingFlags.NonPublic |
                                            BindingFlags.Static |
                                            BindingFlags.DeclaredOnly);

            // 受信イベント設定
            MyNetworkManager.Singleton.OnUdpReceive += OnUdpReceive;

            // ToDo:IDで通信相手と共有
        }

        private void OnDestroy()
        {
            // 受信イベント削除
            MyNetworkManager.Singleton.OnUdpReceive -= OnUdpReceive;
        }

        /// <summary>
        /// UDPパケット受信イベント
        /// </summary>
        /// <param name="header">受信したUDPパケットのヘッダ</param>
        /// <param name="packet">受信したUDPパケット</param>
        private void OnUdpReceive(UdpHeader header, UdpPacket packet)
        {
            // メソッド実行パケット以外は無視
            if (header != UdpHeader.SendMethod) return;

            // 実行クラスが異なる場合は無視
            SendMethodPacket methodPacket = packet as SendMethodPacket;
            if (methodPacket.ClassName != _className) return;

            // IDが異なる場合は無視
            if (methodPacket.ObjectId != _id) return;

            // メソッド実行
            var method = _methods.Where(x => x.Name == methodPacket.MethodName).First();
            method.Invoke(this, methodPacket.Arguments);
        }
    }
}