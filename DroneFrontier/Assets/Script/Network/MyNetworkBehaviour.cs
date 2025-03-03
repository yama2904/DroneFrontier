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
        /// <summary>
        /// 通信相手とのオブジェクト共有用ID
        /// </summary>
        public string ObjectId { get; internal set; } = string.Empty;

        /// <summary>
        /// オブジェクト削除イベント
        /// </summary>
        public event EventHandler OnDestroyObject;

        /// <summary>
        /// クラス名
        /// </summary>
        private string _className = string.Empty;

        /// <summary>
        /// メソッド一覧
        /// </summary>
        private MethodInfo[] _methods = null;

        public virtual string GetAddressKey() { return string.Empty; }

        public virtual object CreateSpawnData() { return null; }

        public virtual void ImportSpawnData(object data) { }

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
            IPacket packet = new SendMethodPacket(ObjectId, _className, name, args.ToArray());
            MyNetworkManager.Singleton.SendToAll(packet);

            // メソッド実行
            // ★DateTime.Now.Millisecondのようなその瞬間によって値が変わる場合にプレイヤー同士で差異が出るため変更
            //action.Compile().Invoke();
            InvokeMethod(name, args.ToArray());
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
            MyNetworkManager.Singleton.OnUdpReceive += OnUdpReceiveOfSendMethod;
        }

        private void OnDestroy()
        {
            // 受信イベント削除
            MyNetworkManager.Singleton.OnUdpReceive -= OnUdpReceiveOfSendMethod;

            OnDestroyObject?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// メソッド実行パケット受信イベント
        /// </summary>
        /// <param name="name">プレイヤー名</param>
        /// <param name="header">受信したUDPパケットのヘッダ</param>
        /// <param name="packet">受信したUDPパケット</param>
        private void OnUdpReceiveOfSendMethod(string name, UdpHeader header, UdpPacket packet)
        {
            // メソッド実行パケット以外は無視
            if (header != UdpHeader.SendMethod) return;

            // 実行クラスが異なる場合は無視
            SendMethodPacket methodPacket = packet as SendMethodPacket;
            if (methodPacket.ClassName != _className) return;

            // IDが異なる場合は無視
            if (methodPacket.ObjectId != ObjectId) return;

            // メソッド実行
            InvokeMethod(methodPacket.MethodName, methodPacket.Arguments);
        }

        /// <summary>
        /// メソッド名と引数を基にメソッド実行
        /// </summary>
        /// <param name="name">実行するメソッド名</param>
        /// <param name="arguments">実行するメソッドに渡す引数</param>
        private void InvokeMethod(string name, object[] arguments)
        {
            // 実行メソッド取得
            var method = _methods.Where(x => x.Name == name).First();
            var parameters = method.GetParameters();

            // 型が一致していない引数をキャスト
            object[] args = new object[arguments.Length];
            Array.Copy(arguments, args, args.Length);
            for (int i = 0; i < args.Length; i++)
            {
                object receiveArg = arguments[i];
                Type type = parameters[i].ParameterType;
                if (!type.Equals(receiveArg.GetType()))
                {
                    if (type.IsEnum)
                    {
                        args[i] = Enum.Parse(type, receiveArg.ToString());
                    }
                    else
                    {
                        args[i] = Convert.ChangeType(receiveArg, type);
                    }
                }
            }

            // メソッド実行
            method.Invoke(this, args);
        }
    }
}