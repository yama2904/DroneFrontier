using Network.Udp;
using System.Collections.Generic;
using UnityEngine;

namespace Network
{
    public class NetworkFrameRateAdjuster : MonoBehaviour
    {
        [SerializeField, Tooltip("初期フレームレート")]
        private int _initFrameRate = 60;

        [SerializeField, Tooltip("最低フレームレート")]
        private int _minFrameRate = 10;

        [SerializeField, Tooltip("最大フレームレート")]
        private int _maxFrameRate = 120;

        [SerializeField, Tooltip("フレームレートの最大増減ステップ数")]
        private int _frameRateMaxStep = 10;

        [SerializeField, Tooltip("フレームレートチェック間隔（秒）")]
        private float _checkInterval = 1f;

        private Dictionary<string, int> _playersFps = new Dictionary<string, int>();

        private string _myPlayerName;
        private int _playerCount;
        private bool _isHost;

        /// <summary>
        /// 現在のフレームレート
        /// </summary>
        private int _currentFps = 0;

        /// <summary>
        /// 前回チェック時点からの経過フレーム数
        /// </summary>
        private int _frameCount = 0;

        /// <summary>
        /// 前回チェック時間
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
        /// UDPパケット受信イベント
        /// </summary>
        /// <param name="name">プレイヤー名</param>
        /// <param name="header">受信したUDPパケットのヘッダ</param>
        /// <param name="packet">受信したUDPパケット</param>
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