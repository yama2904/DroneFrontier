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
        /// �ʐM����Ƃ̃I�u�W�F�N�g���L�pID
        /// </summary>
        public string ObjectId { get; internal set; } = string.Empty;

        /// <summary>
        /// ����I�ɍ��W�𓯊����邩
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
        /// �I�u�W�F�N�g�폜�C�x���g
        /// </summary>
        public event EventHandler OnDestroyObject;

        [SerializeField, Tooltip("���W�𓯊����邩")]
        private bool _syncPosition = false;

        [SerializeField, Range(0.1f, 10f), Tooltip("���W�̓����Ԋu�i�b�j")]
        private float _syncInterval = 1f;

        [SerializeField, Tooltip("���W�����̃g���K�[�ƂȂ���W�Y����")]
        private float _syncPositionDistance = 20f;

        private CancellationTokenSource _cancel = new CancellationTokenSource();

        /// <summary>
        /// �N���X��
        /// </summary>
        private string _className = string.Empty;

        /// <summary>
        /// ���\�b�h�ꗗ
        /// </summary>
        private MethodInfo[] _methods = null;

        public virtual string GetAddressKey() { return string.Empty; }

        public virtual object CreateSpawnData() { return null; }

        public virtual void ImportSpawnData(object data) { }

        /// <summary>
        /// �I�u�W�F�N�g�����p�P�b�g�ɂ��X�|�[�����̏�����
        /// </summary>
        public virtual void InitializeSpawn() { }

        protected virtual void Awake()
        {
            IsSyncPosition = _syncPosition;

            // �N���X���擾
            _className = GetType().Name;

            // ���\�b�h�ꗗ�擾
            _methods = GetType().GetMethods(BindingFlags.Instance |
                                            BindingFlags.Public |
                                            BindingFlags.NonPublic |
                                            BindingFlags.Static |
                                            BindingFlags.DeclaredOnly);
            
            // ��M�C�x���g�ݒ�
            MyNetworkManager.Singleton.OnUdpReceiveOnMainThread += OnUdpReceiveOfSendMethod;
            MyNetworkManager.Singleton.OnUdpReceiveOnMainThread += OnUdpReceiveOfPosition;
        }

        protected virtual void OnDestroy()
        {
            // ��M�C�x���g�폜
            MyNetworkManager.Singleton.OnUdpReceiveOnMainThread -= OnUdpReceiveOfSendMethod;
            MyNetworkManager.Singleton.OnUdpReceiveOnMainThread -= OnUdpReceiveOfPosition;

            // �L�����Z�����s
            _cancel.Cancel();

            OnDestroyObject?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// �����ɓn���ꂽ���\�b�h�����s���A�S�Ă̒ʐM����ɂ����s������
        /// </summary>
        /// <param name="action">���s���郁�\�b�h</param>
        protected void SendMethod(Expression<Action> action)
        {
            // ���\�b�h���
            MethodCallExpression methodCall = action.Body as MethodCallExpression;

            // ���\�b�h���擾
            string name = methodCall.Method.Name;

            // �����擾
            List<object> args = new List<object>();
            foreach (var arg in methodCall.Arguments)
            {
                object value = Expression.Lambda(arg).Compile().DynamicInvoke();
                args.Add(value);
            }

            // �p�P�b�g���M
            UdpPacket packet = new SendMethodPacket(ObjectId, _className, name, args.ToArray());
            MyNetworkManager.Singleton.SendToAll(packet);

            // ���\�b�h���s
            // ��DateTime.Now.Millisecond�̂悤�Ȃ��̏u�Ԃɂ���Ēl���ς��ꍇ�Ƀv���C���[���m�ō��ق��o�邽�ߕύX
            //action.Compile().Invoke();
            InvokeMethod(name, args.ToArray());
        }

        /// <summary>
        /// ���\�b�h���s�p�P�b�g��M�C�x���g
        /// </summary>
        /// <param name="name">�v���C���[��</param>
        /// <param name="header">��M����UDP�p�P�b�g�̃w�b�_</param>
        /// <param name="packet">��M����UDP�p�P�b�g</param>
        private void OnUdpReceiveOfSendMethod(string name, UdpHeader header, UdpPacket packet)
        {
            // ���\�b�h���s�p�P�b�g�ȊO�͖���
            if (header != UdpHeader.SendMethod) return;

            // ���s�N���X���قȂ�ꍇ�͖���
            SendMethodPacket methodPacket = packet as SendMethodPacket;
            if (methodPacket.ClassName != _className) return;

            // ID���قȂ�ꍇ�͖���
            if (methodPacket.ObjectId != ObjectId) return;

            // ���\�b�h���s
            InvokeMethod(methodPacket.MethodName, methodPacket.Arguments);
        }

        /// <summary>
        /// ���W�����p�P�b�g��M�C�x���g
        /// </summary>
        /// <param name="name">�v���C���[��</param>
        /// <param name="header">��M����UDP�p�P�b�g�̃w�b�_</param>
        /// <param name="packet">��M����UDP�p�P�b�g</param>
        private void OnUdpReceiveOfPosition(string name, UdpHeader header, UdpPacket packet)
        {
            // ���W�����p�P�b�g�ȊO����
            if (header != UdpHeader.Position) return;

            // �p�P�b�g�擾
            PositionPacket posPacket = packet as PositionPacket;

            // ����I�u�W�F�N�gID�ȊO����
            if (posPacket.ObjectId != ObjectId) return;

            // ���W�K�p
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
        /// ���\�b�h���ƈ�������Ƀ��\�b�h���s
        /// </summary>
        /// <param name="name">���s���郁�\�b�h��</param>
        /// <param name="arguments">���s���郁�\�b�h�ɓn������</param>
        private void InvokeMethod(string name, object[] arguments)
        {
            // ���s���\�b�h�擾
            var method = _methods.Where(x => x.Name == name).First();
            var parameters = method.GetParameters();

            // �^����v���Ă��Ȃ��������L���X�g
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

            // ���\�b�h���s
            method.Invoke(this, args);
        }
    }
}