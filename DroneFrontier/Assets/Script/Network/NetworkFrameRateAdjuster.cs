using Network.Udp;
using System.Collections.Generic;
using UnityEngine;

namespace Network
{
    public class NetworkFrameRateAdjuster : MonoBehaviour
    {
        [SerializeField, Tooltip("�����t���[�����[�g")]
        private int _initFrameRate = 60;

        [SerializeField, Tooltip("�Œ�t���[�����[�g")]
        private int _minFrameRate = 10;

        [SerializeField, Tooltip("�ő�t���[�����[�g")]
        private int _maxFrameRate = 120;

        [SerializeField, Tooltip("�t���[�����[�g�̍ő呝���X�e�b�v��")]
        private int _frameRateMaxStep = 10;

        [SerializeField, Tooltip("�t���[�����[�g�`�F�b�N�Ԋu�i�b�j")]
        private float _checkInterval = 1f;

        private Dictionary<string, int> _playersFps = new Dictionary<string, int>();

        private string _myPlayerName;
        private int _playerCount;
        private bool _isHost;

        /// <summary>
        /// ���݂̃t���[�����[�g
        /// </summary>
        private int _currentFps = 0;

        /// <summary>
        /// �O��`�F�b�N���_����̌o�߃t���[����
        /// </summary>
        private int _frameCount = 0;

        /// <summary>
        /// �O��`�F�b�N����
        /// </summary>
        private float _prevCheckTime = 0;

        private void Awake()
        {
            MyNetworkManager.Singleton.OnUdpReceive += OnUdpReceive;

            _myPlayerName = MyNetworkManager.Singleton.MyPlayerName;
            _playerCount = MyNetworkManager.Singleton.PlayerCount;
            _isHost = MyNetworkManager.Singleton.IsHost;

            Application.targetFrameRate = _initFrameRate;
            _currentFps = _initFrameRate;
        }

        private void Update()
        {
            _frameCount++;
            float time = Time.realtimeSinceStartup - _prevCheckTime;
            if (time < _checkInterval) return;

            int fps = Mathf.CeilToInt(_frameCount / time);

            if (_isHost)
            {
                lock (_playersFps)
                {
                    AddOrSet(_playersFps, _myPlayerName, fps);
                    if (_playersFps.Count == _playerCount)
                    {
                        AdjustFps();
                    }
                }
            }
            else
            {
                MyNetworkManager.Singleton.SendToHost(new FrameRatePacket(fps));
            }

            _frameCount = 0;
            _prevCheckTime = Time.realtimeSinceStartup;
        }

        private void OnDestroy()
        {
            MyNetworkManager.Singleton.OnUdpReceive -= OnUdpReceive;
        }

        /// <summary>
        /// UDP�p�P�b�g��M�C�x���g
        /// </summary>
        /// <param name="name">�v���C���[��</param>
        /// <param name="header">��M����UDP�p�P�b�g�̃w�b�_</param>
        /// <param name="packet">��M����UDP�p�P�b�g</param>
        private void OnUdpReceive(string name, UdpHeader header, UdpPacket packet)
        {
            if (header != UdpHeader.FrameRate) return;

            int fps = (packet as FrameRatePacket).FrameRate;

            if (_isHost)
            {
                lock (_playersFps)
                {
                    AddOrSet(_playersFps, name, fps);
                    if (_playersFps.Count == _playerCount)
                    {
                        AdjustFps();
                    }
                }
            }
            else
            {
                Application.targetFrameRate = fps;
            }
        }

        private void AddOrSet<K, V>(Dictionary<K, V> dic, K key, V value)
        {
            if (dic.ContainsKey(key))
                dic[key] = value;
            else
                dic.Add(key, value);
        }

        private void AdjustFps()
        {
            int newFps = int.MaxValue;
            foreach (int fps in _playersFps.Values)
            {
                if (newFps > fps)
                    newFps = fps;
            }

            int step = 1;
            if (_currentFps > newFps)
            {
                step = newFps - _currentFps;
                if (Mathf.Abs(step) > _frameRateMaxStep)
                {
                    step = _frameRateMaxStep * -1;
                }
            }

            _currentFps += step;
            _currentFps = _currentFps < _minFrameRate ? _minFrameRate : _currentFps;
            _currentFps = _currentFps > _maxFrameRate ? _maxFrameRate : _currentFps;
            MyNetworkManager.Singleton.SendToAll(new FrameRatePacket(_currentFps));
            Application.targetFrameRate = _currentFps;

            _playersFps.Clear();
        }
    }
}