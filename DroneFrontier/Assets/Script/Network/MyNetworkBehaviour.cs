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
        /// �ʐM����Ƃ̃I�u�W�F�N�g���L�pID
        /// </summary>
        public long ObjectId { get; set; } = -1;

        [SerializeField, Tooltip("�I�u�W�F�N�g��ID�ŊǗ����đS�Ă̒ʐM����Ƌ��L���邩")]
        private bool _generateId = false;

        /// <summary>
        /// �N���X��
        /// </summary>
        private string _className = string.Empty;

        /// <summary>
        /// ���\�b�h�ꗗ
        /// </summary>
        private MethodInfo[] _methods = null;

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
            IPacket packet = new SendMethodPacket(_className, name, args.ToArray(), ObjectId);
            MyNetworkManager.Singleton.SendAsync(packet);

            // ���\�b�h���s
            action.Compile().Invoke();
        }

        private void Awake()
        {
            // �N���X���擾
            _className = GetType().Name;

            // ���\�b�h�ꗗ�擾
            _methods = GetType().GetMethods(BindingFlags.Instance |
                                            BindingFlags.Public |
                                            BindingFlags.NonPublic |
                                            BindingFlags.Static |
                                            BindingFlags.DeclaredOnly);
            
            // ��M�C�x���g�ݒ�
            MyNetworkManager.Singleton.OnUdpReceive += OnUdpReceiveOfSendMethod;

            // ToDo:ID�ŒʐM����Ƌ��L
        }

        private void OnDestroy()
        {
            // ��M�C�x���g�폜
            MyNetworkManager.Singleton.OnUdpReceive -= OnUdpReceiveOfSendMethod;
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

            // ���s���\�b�h�擾
            var method = _methods.Where(x => x.Name == methodPacket.MethodName).First();
            var parameters = method.GetParameters();

            // �^����v���Ă��Ȃ��������L���X�g
            object[] args = new object[methodPacket.Arguments.Length];
            Array.Copy(methodPacket.Arguments, args, args.Length);
            for (int i = 0; i < args.Length; i++)
            {
                object receiveArg = methodPacket.Arguments[i];
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