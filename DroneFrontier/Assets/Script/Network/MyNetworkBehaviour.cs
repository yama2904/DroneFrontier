using Cysharp.Threading.Tasks;
using Network.Udp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
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
        /// 定期的に座標を同期するか
        /// </summary>
        public bool IsSyncPosition
        {
            get
            {
                return _isSyncPosition;
            }
            set
            {
                if (_isSyncPosition == value) return;

                if (value)
                {
                    UniTask.Void(async () =>
                    {
                        TimeSpan interval = TimeSpan.FromSeconds(SyncInterval);
                        while (true)
                        {
                            await UniTask.Delay(interval, cancellationToken: _cancel.Token);
                            MyNetworkManager.Singleton.SendToAll(new PositionPacket(this));
                        }
                    });
                }
                else
                {
                    _cancel.Cancel();
                    _cancel = new CancellationTokenSource();
                }

                _isSyncPosition = value;
            }
        }
        private bool _isSyncPosition = false;

        public float SyncInterval
        {
            get => _syncInterval;
            set => _syncInterval = value;
        }

        public float SyncPositionDistance
        {
            get => _syncPositionDistance;
            set => _syncPositionDistance = value;
        }

        /// <summary>
        /// オブジェクト削除イベント
        /// </summary>
        public event EventHandler OnDestroyObject;

        [SerializeField, Tooltip("座標を同期するか")]
        private bool _syncPosition = false;

        [SerializeField, Range(0.1f, 10f), Tooltip("座標の同期間隔（秒）")]
        private float _syncInterval = 1f;

        [SerializeField, Tooltip("座標同期のトリガーとなる座標ズレ量")]
        private float _syncPositionDistance = 20f;

        private CancellationTokenSource _cancel = new CancellationTokenSource();

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
        /// オブジェクト生成パケットによるスポーン時の初期化
        /// </summary>
        public virtual void InitializeSpawn() { }

        protected virtual void Awake()
        {
            IsSyncPosition = _syncPosition;

            // クラス名取得
            _className = GetType().Name;

            // メソッド一覧取得
            _methods = GetType().GetMethods(BindingFlags.Instance |
                                            BindingFlags.Public |
                                            BindingFlags.NonPublic |
                                            BindingFlags.Static |
                                            BindingFlags.DeclaredOnly);
            
            // 受信イベント設定
            MyNetworkManager.Singleton.OnUdpReceiveOnMainThread += OnUdpReceiveOfSendMethod;
            MyNetworkManager.Singleton.OnUdpReceiveOnMainThread += OnUdpReceiveOfPosition;
        }

        protected virtual void OnDestroy()
        {
            // 受信イベント削除
            MyNetworkManager.Singleton.OnUdpReceiveOnMainThread -= OnUdpReceiveOfSendMethod;
            MyNetworkManager.Singleton.OnUdpReceiveOnMainThread -= OnUdpReceiveOfPosition;

            // キャンセル発行
            _cancel.Cancel();

            OnDestroyObject?.Invoke(this, EventArgs.Empty);
        }

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
            UdpPacket packet = new SendMethodPacket(ObjectId, _className, name, args.ToArray());
            MyNetworkManager.Singleton.SendToAll(packet);

            // メソッド実行
            // ★DateTime.Now.Millisecondのようなその瞬間によって値が変わる場合にプレイヤー同士で差異が出るため変更
            //action.Compile().Invoke();
            InvokeMethod(name, args.ToArray());
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
        /// 座標同期パケット受信イベント
        /// </summary>
        /// <param name="name">プレイヤー名</param>
        /// <param name="header">受信したUDPパケットのヘッダ</param>
        /// <param name="packet">受信したUDPパケット</param>
        private void OnUdpReceiveOfPosition(string name, UdpHeader header, UdpPacket packet)
        {
            // 座標同期パケット以外無視
            if (header != UdpHeader.Position) return;

            // パケット取得
            PositionPacket posPacket = packet as PositionPacket;

            // 同一オブジェクトID以外無視
            if (posPacket.ObjectId != ObjectId) return;

            // 座標適用
            var t = transform;
            var pos = posPacket.Position;
            var rotate = posPacket.Rotation;
            if (Vector3.Distance(t.position, pos) >= _syncPositionDistance)
            {
                t.position = new Vector3(pos.x, pos.y, pos.z);
                t.rotation = new Quaternion(rotate.x, rotate.y, rotate.z, rotate.w);
            }
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